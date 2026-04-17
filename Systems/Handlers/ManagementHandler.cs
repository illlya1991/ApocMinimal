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
/// Обработчик управления NPC: награждение и наказание
/// </summary>
public class ManagementHandler : BaseActionHandler
{
    public ManagementHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction,
        Dictionary<string, double> config)
        : base(db, rnd, logAction, config) { }

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

        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не указан целевой NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Ошибка: неверный тип целевого NPC";
        if (!target.IsAlive) return $"{target.Name} мертв";

        return actionKey switch
        {
            "RewardNpc" => ExecuteRewardNpc(target, player),
            "PunishNpc" => ExecutePunishNpc(target, player),
            _ => $"Неизвестное действие: {actionKey}"
        };
    }

    // ============================================================
    // 1. Награждение NPC (повышение уровня последователя)
    // ============================================================
    private string ExecuteRewardNpc(Npc target, Player player)
    {
        if (target.FollowerLevel >= 5)
            return $"{target.Name} уже достиг максимального уровня последователя ({target.FollowerLabel})";

        // Проверка лимита алтаря
        int currentCount = _db.GetFollowerCountAtLevel(target.FollowerLevel + 1);
        int limit = player.GetFollowerLimit(target.FollowerLevel + 1);
        if (limit != -1 && currentCount >= limit)
            return $"Невозможно повысить: достигнут лимит алтаря для уровня {target.FollowerLevel + 1} (лимит: {limit})";

        // Стоимость повышения в вере
        double cost = CalculateUpgradeCost(target);
        if (player.FaithPoints < cost)
            return $"Недостаточно веры для повышения {target.Name} (нужно {cost:F0}, есть {player.FaithPoints:F0})";

        // Повышаем уровень
        player.FaithPoints -= cost;
        target.FollowerLevel++;

        // Бонусы при повышении
        ApplyRewardBonuses(target, player.CurrentDay);

        _db.SavePlayer(player);
        _db.SaveNpc(target);

        // Логирование
        Log($"  {target.Name} повышен до {target.FollowerLabel}!", LogEntry.ColorAltarColor);
        Log($"    Стоимость: {cost:F0} веры", LogEntry.ColorNormal);
        Log($"    Доверие: +10 (теперь {target.Trust:F0})", LogEntry.ColorSuccess);

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Divine,
            $"Божество повысило меня до {target.FollowerLabel}!"));

        return $"{target.Name} повышен до {target.FollowerLabel}";
    }

    // ============================================================
    // 2. Наказание NPC (понижение уровня последователя)
    // ============================================================
    private string ExecutePunishNpc(Npc target, Player player)
    {
        if (target.FollowerLevel <= 0)
            return $"{target.Name} уже на минимальном уровне последователя";

        // Понижаем уровень
        int oldLevel = target.FollowerLevel;
        target.FollowerLevel = Math.Max(0, target.FollowerLevel - 1);

        // Штрафы
        ApplyPunishmentPenalties(target, player.CurrentDay);

        _db.SaveNpc(target);

        // Логирование
        Log($"  {target.Name} понижен до {target.FollowerLabel}!", LogEntry.ColorDanger);
        Log($"    Доверие: -15 (теперь {target.Trust:F0})", LogEntry.ColorDanger);
        Log($"    Вера NPC: -{(oldLevel - target.FollowerLevel) * 10:F0}", LogEntry.ColorDanger);

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Divine,
            $"Божество наказало меня, понизив до {target.FollowerLabel}"));

        return $"{target.Name} понижен до {target.FollowerLabel}";
    }

    // ============================================================
    // Вспомогательные методы
    // ============================================================

    private double CalculateUpgradeCost(Npc target)
    {
        double baseCost = GetConfig($"follower_upgrade_cost_{target.FollowerLevel}", target.FollowerLevel switch
        {
            0 => 50, 1 => 100, 2 => 200, 3 => 400, 4 => 800, _ => 1000
        });

        double trustDiscount = 1.0 - (target.Trust / 200.0);
        return Math.Max(10, baseCost * trustDiscount);
    }

    private void ApplyRewardBonuses(Npc target, int currentDay)
    {
        // Повышение доверия
        target.Trust = Math.Min(100, target.Trust + 10);

        // Восстановление статов
        target.Stamina = Math.Min(100, target.Stamina + 20);
        target.Fear = Math.Max(0, target.Fear - 15);

        // Бонус к вере NPC
        target.Faith = Math.Min(100, target.Faith + 20);

        // Небольшое повышение инициативы
        target.Initiative = Math.Min(100, target.Initiative + 5);
    }

    private void ApplyPunishmentPenalties(Npc target, int currentDay)
    {
        // Снижение доверия
        target.Trust = Math.Max(0, target.Trust - 15);

        // Штраф к статам
        target.Stamina = Math.Max(0, target.Stamina - 15);
        target.Fear = Math.Min(100, target.Fear + 20);

        // Потеря веры NPC
        target.Faith = Math.Max(0, target.Faith - 10);

        // Снижение инициативы
        target.Initiative = Math.Max(0, target.Initiative - 5);
    }

}