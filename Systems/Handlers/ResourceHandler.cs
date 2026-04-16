using System;
using System.Collections.Generic;
using System.Linq;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.UIData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Универсальный обработчик для группы "Ресурсы"
/// </summary>
public class ResourceHandler : BaseActionHandler
{
    private readonly Random _random;

    public ResourceHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
        : base(db, rnd, logAction)
    {
        _random = rnd;
    }

    public override string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        if (!parameters.ContainsKey("_actionKey") || parameters["_actionKey"] == null)
            return "Не указано действие";

        var actionKey = parameters["_actionKey"].ToString();

        // Проверка общих параметров для всех действий
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не указан целевой NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Ошибка: неверный тип целевого NPC";
        if (!target.IsAlive) return $"{target.Name} мертв";

        if (!parameters.ContainsKey("resourceName") || parameters["resourceName"] == null)
            return "Не указан ресурс";

        Resource? resource = parameters["resourceName"] as Resource;
        if (resource == null) return "Не указан ресурс";

        if (!parameters.ContainsKey("amount") || parameters["amount"] == null)
            return "Не указано количество";

        var amount = Convert.ToDouble(parameters["amount"]);
        if (amount <= 0) return "Количество должно быть больше 0";

        return actionKey switch
        {
            "TransferResource" => ExecuteTransfer(target, resource, amount, player, resources),
            "DemandResource" => ExecuteDemand(target, resource, amount, player, resources),
            _ => $"Неизвестное действие: {actionKey}"
        };
    }

    // ============================================================
    // 1. Передача ресурса NPC (от игрока к NPC)
    // ============================================================
    private string ExecuteTransfer(Npc target, Resource resource, double amount, Player player, List<Resource> resources)
    {
        if (resource.Amount < amount)
            return $"Недостаточно '{resource.Name}'. Есть: {resource.Amount:F0}, нужно: {amount:F0}";

        resource.Amount -= amount;
        _db.SaveResource(resource);

        // Удовлетворение потребностей NPC
        var satisfaction = NeedSystem.SatisfyNeed(target, resource.Name, amount * 0.5);
        if (satisfaction)
            Log($"  Потребность '{resource.Name}' удовлетворена на {amount * 0.5:F0}%", LogEntry.ColorSuccess);

        // Расчет изменения доверия
        int trustGain = CalculateTransferTrustGain(target, resource, amount);
        target.Trust = Math.Min(100, target.Trust + trustGain);
        _db.SaveNpc(target);

        // Добавление в память
        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social,
            $"Получил {amount:F0} ед. {resource.Name} от Божества"));

        // Логирование
        Log($"Передано {amount:F0} ед. '{resource.Name}' → {target.Name}", LogEntry.ColorSuccess);
        Log($"  Осталось: {resource.Amount:F0}", LogEntry.ColorNormal);
        Log($"  Доверие {(trustGain >= 0 ? "+" : "")}{trustGain} (теперь {target.Trust:F0})",
            trustGain >= 0 ? LogEntry.ColorSuccess : LogEntry.ColorDanger);

        // Шанс повышения уровня последователя
        if (trustGain > 5 && target.FollowerLevel < 5 && _random.NextDouble() < 0.1)
        {
            target.FollowerLevel++;
            _db.SaveNpc(target);
            Log($"  {target.Name} повышен до {target.FollowerLabel}!", LogEntry.ColorAltarColor);
        }

        return $"Ресурс передан {target.Name}";
    }

    // ============================================================
    // 2. Требование ресурса от NPC (игрок забирает)
    // ============================================================
    private string ExecuteDemand(Npc target, Resource resource, double amount, Player player, List<Resource> resources)
    {
        // Проверка доверия
        if (target.Trust < 30)
            return $"{target.Name} не доверяет тебе достаточно, чтобы отдать ресурсы (Доверие: {target.Trust:F0} < 30)";

        // Расчет доступного количества
        double available = CalculateAvailableResources(target, resource);
        if (available <= 0)
            return $"{target.Name} не имеет ресурсов для передачи";

        double actualAmount = Math.Min(amount, available);

        // Шанс отказа в зависимости от доверия
        double refuseChance = Math.Max(0.1, 0.5 - (target.Trust / 100.0) * 0.4);
        if (_random.NextDouble() < refuseChance)
        {
            // Отказ - снижение доверия
            int trustLoss = (int)Math.Min(15, 5 + actualAmount / 10);
            target.Trust = Math.Max(0, target.Trust - trustLoss);
            _db.SaveNpc(target);

            Log($"{target.Name} отказался отдавать ресурсы!", LogEntry.ColorDanger);
            Log($"  Доверие -{trustLoss} (теперь {target.Trust:F0})", LogEntry.ColorDanger);

            target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social,
                $"Отказал Божеству в ресурсах (доверие -{trustLoss})"));

            return $"{target.Name} отказал";
        }

        // Получаем ресурсы
        resource.Amount += actualAmount;
        _db.SaveResource(resource);

        // Снижение веры NPC
        double faithLoss = actualAmount * 0.5;
        target.Faith = Math.Max(0, target.Faith - faithLoss);

        // Снижение доверия
        int trustLossFinal = (int)Math.Min(20, 5 + actualAmount / 5);
        target.Trust = Math.Max(0, target.Trust - trustLossFinal);

        _db.SaveNpc(target);

        // Логирование
        Log($"{target.Name} отдал {actualAmount:F0} ед. '{resource.Name}'", LogEntry.ColorWarning);
        Log($"  Вера NPC: -{faithLoss:F0} (теперь {target.Faith:F0})", LogEntry.ColorDanger);
        Log($"  Доверие -{trustLossFinal} (теперь {target.Trust:F0})", LogEntry.ColorDanger);

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social,
            $"Божество потребовало {actualAmount:F0} ед. {resource.Name} (доверие -{trustLossFinal})"));

        return $"Требование ресурсов от {target.Name} выполнено";
    }

    // ============================================================
    // Вспомогательные методы
    // ============================================================

    private int CalculateTransferTrustGain(Npc target, Resource resource, double amount)
    {
        int gain = (int)Math.Min(15, amount / 10 + 2);

        // Бонус от черт
        if (target.CharTraits.Contains(CharacterTrait.Generous)) gain += 3;
        if (target.CharTraits.Contains(CharacterTrait.Empathetic)) gain += 2;

        // Бонус, если ресурс удовлетворяет критическую потребность
        var urgentNeed = NeedSystem.GetMostUrgentNeed(target);
        if (urgentNeed != null && urgentNeed.IsCritical && urgentNeed.Name == resource.Name)
            gain += 5;

        return Math.Max(1, gain);
    }

    private double CalculateAvailableResources(Npc target, Resource resource)
    {
        // NPC отдаёт верой, а не прямыми ресурсами
        double baseAmount = target.Faith * 0.3;

        // Модификатор уровня последователя
        double followerMod = target.FollowerLevel switch
        {
            0 => 0.3,
            1 => 0.5,
            2 => 0.7,
            3 => 1.0,
            4 => 1.3,
            5 => 1.6,
            _ => 1.0
        };

        // Модификатор доверия
        double trustMod = target.Trust / 100.0;

        // Модификатор черт
        double traitMod = 1.0;
        if (target.CharTraits.Contains(CharacterTrait.Generous)) traitMod *= 1.5;
        if (target.CharTraits.Contains(CharacterTrait.Greedy)) traitMod *= 0.3;

        // Штраф за критические потребности
        double needsMod = 1.0;
        if (target.Needs.Any(n => n.IsCritical)) needsMod *= 0.5;

        return baseAmount * followerMod * trustMod * traitMod * needsMod;
    }
}