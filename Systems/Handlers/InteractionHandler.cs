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
/// Универсальный обработчик для группы "Взаимодействие"
/// Обрабатывает: Chat (поговорить), Donate (пожертвование), и другие действия
/// </summary>
public class InteractionHandler : BaseActionHandler
{
    private readonly Random _random;

    public InteractionHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction,
        Dictionary<string, double> config)
        : base(db, rnd, logAction, config)
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
        // Получаем действие из параметров (нужно передавать ActionKey)
        if (!parameters.ContainsKey("_actionKey") || parameters["_actionKey"] == null)
            return "Не указано действие";

        var actionKey = parameters["_actionKey"].ToString();

        // Проверка наличия целевого NPC
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не указан целевой NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Ошибка: неверный тип целевого NPC";
        if (!target.IsAlive) return $"{target.Name} мертв";

        // Выбираем действие по ключу
        return actionKey switch
        {
            "Chat" => ExecuteChat(target, player),
            "Donate" => ExecuteDonate(target, player),
            "Calm" => ExecuteCalm(target, player),
            "Inspire" => ExecuteInspire(target, player),
            "Intimidate" => ExecuteIntimidate(target, player),
            _ => $"Неизвестное действие: {actionKey}"
        };
    }

    // ============================================================
    // 1. Поговорить (Chat)
    // ============================================================
    private string ExecuteChat(Npc target, Player player)
    {
        // Генерация ответа
        string response = GenerateChatResponse(target);

        // Проверка критических потребностей
        var urgentNeed = NeedSystem.GetMostUrgentNeed(target);
        if (urgentNeed != null && urgentNeed.IsCritical)
            response += $" Мне срочно нужно: {urgentNeed.Name}!";

        // Расчет изменения доверия
        int trustChange = CalculateTrustChange(target, "Chat");
        target.Trust = Math.Clamp(target.Trust + trustChange, 0, 100);

        // Сохранение
        _db.SaveNpc(target);
        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social, $"Поговорил с Божеством (доверие {(trustChange >= 0 ? "+" : "")}{trustChange})"));
        _db.SaveNpc(target);

        // Логирование
        Log($"Ты обращаешься к {target.Name}:", LogEntry.ColorNormal);
        Log($"  {target.Name}: «{response}»", LogEntry.ColorSpeech);
        Log($"  Доверие {(trustChange >= 0 ? "+" : "")}{trustChange}",
            trustChange >= 0 ? LogEntry.ColorSuccess : LogEntry.ColorDanger);

        return $"Разговор с {target.Name} завершён";
    }

    private string GenerateChatResponse(Npc target)
    {
        // Черты характера
        if (target.CharTraits.Contains(CharacterTrait.Loyal))
            return "Я всегда верю в тебя, Божество!";
        if (target.CharTraits.Contains(CharacterTrait.Cowardly))
            return target.Health < 40 ? "Пожалуйста, не бросай нас! Я боюсь..." : "Я постараюсь. Только это не опасно?";
        if (target.CharTraits.Contains(CharacterTrait.Greedy))
            return "Что ты можешь мне дать за разговор?";
        if (target.CharTraits.Contains(CharacterTrait.Paranoid))
            return "...(подозрительно оглядывается и ничего не говорит)";
        if (target.CharTraits.Contains(CharacterTrait.Lazy))
            return "*зевает* ... Может, позже?";
        if (target.CharTraits.Contains(CharacterTrait.Curious))
            return "Расскажи мне что-нибудь интересное!";

        // Тип NPC
        switch (target.Trait)
        {
            case NpcTrait.Loner when target.Trust < 50:
                return "...(пожимает плечами и отводит взгляд)";
            case NpcTrait.Loner:
                return "Я справлюсь сам. Спасибо.";
            case NpcTrait.Coward when target.Health < 40:
                return "Пожалуйста, не бросай нас! Я боюсь...";
            case NpcTrait.Coward:
                return "Я постараюсь. Только это не опасно?";
            case NpcTrait.Leader when target.Trust > 60:
                return "Я верю в тебя, Божество! Веду остальных вперёд.";
        }

        // Уровень доверия
        if (target.Trust >= 70) return "Я верю в тебя! Мы выживем вместе.";
        if (target.Trust >= 40) return "Спасибо за заботу. Стараюсь держаться.";
        return "Тяжело. Не знаю, есть ли смысл продолжать...";
    }

    // ============================================================
    // 2. Получить пожертву (Donate)
    // ============================================================
    private string ExecuteDonate(Npc target, Player player)
    {
        double donationAmount = CalculateDonationAmount(target, player.FactionCoeffs.CoeffDonation);

        if (donationAmount <= 0)
        {
            Log($"{target.Name} не может сделать пожертвование сейчас.", LogEntry.ColorWarning);
            return $"Пожертвование не получено";
        }

        // Применяем пожертвование
        target.Devotion = Math.Max(0, target.Devotion - donationAmount);
        player.DevPoints += donationAmount;

        _db.SaveNpc(target);
        _db.SavePlayer(player);

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Divine, $"Пожертвовал {donationAmount:F0} ОР"));
        _db.SaveNpc(target);

        // Логирование
        Log($"{target.Name} делает пожертвование:", LogEntry.ColorSpeech);
        Log($"  +{donationAmount:F0} ОР получено", LogEntry.ColorAltarColor);
        Log($"  Преданность NPC: {target.Devotion + donationAmount:F0} → {target.Devotion:F0}", LogEntry.ColorNormal);

        return $"Пожертвование от {target.Name} получено";
    }

    private double CalculateDonationAmount(Npc target, double donationCoeff = 1.0)
    {
        double baseAmount = target.Devotion * 0.3 * donationCoeff;

        double followerMod = GetConfig($"donate_mod_level_{target.FollowerLevel}", target.FollowerLevel switch
        {
            0 => 0.5, 1 => 0.8, 2 => 1.0, 3 => 1.2, 4 => 1.5, 5 => 2.0, _ => 1.0
        });

        double trustMod = 0.5 + target.Trust / 100.0;

        double traitMod = 1.0;
        if (target.CharTraits.Contains(CharacterTrait.Loyal)) traitMod *= 1.5;
        if (target.CharTraits.Contains(CharacterTrait.Greedy)) traitMod *= 0.3;
        if (target.CharTraits.Contains(CharacterTrait.Generous)) traitMod *= 1.3;

        double healthMod = target.Health / 100.0;
        double randomMod = 0.7 + _random.NextDouble() * 0.6;

        double donation = baseAmount * followerMod * trustMod * traitMod * healthMod * randomMod;
        return Math.Clamp(Math.Floor(donation), 1, target.Devotion);
    }

    // ============================================================
    // 3. Успокоить (Calm) - снижает страх
    // ============================================================
    private string ExecuteCalm(Npc target, Player player)
    {
        int fearReduction = CalculateFearReduction(target);

        target.Fear = Math.Max(0, target.Fear - fearReduction);
        _db.SaveNpc(target);

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social, $"Божество успокоило (страх -{fearReduction})"));
        _db.SaveNpc(target);

        Log($"Ты успокаиваешь {target.Name}:", LogEntry.ColorNormal);
        Log($"  Страх -{fearReduction} (теперь {target.Fear:F0})", LogEntry.ColorSuccess);

        return $"{target.Name} успокоен";
    }

    private int CalculateFearReduction(Npc target)
    {
        int reduction = 10;

        if (target.CharTraits.Contains(CharacterTrait.Empathetic)) reduction += 5;
        if (target.CharTraits.Contains(CharacterTrait.Cowardly)) reduction += 3;
        if (target.CharTraits.Contains(CharacterTrait.Brave)) reduction -= 5;

        return Math.Clamp(reduction, 5, 25);
    }

    // ============================================================
    // 4. Вдохновить (Inspire) - повышает веру и инициативу
    // ============================================================
    private string ExecuteInspire(Npc target, Player player)
    {
        int faithGain = CalculateInspirationGain(target);

        target.Devotion = Math.Min(100, target.Devotion + faithGain);
        target.Initiative = Math.Min(100, target.Initiative + 5);
        _db.SaveNpc(target);

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Divine, $"Мотивация координатора (преданность +{faithGain})"));
        _db.SaveNpc(target);

        Log($"Ты вдохновляешь {target.Name}:", LogEntry.ColorNormal);
        Log($"  Преданность +{faithGain} (теперь {target.Devotion:F0})", LogEntry.ColorAltarColor);
        Log($"  Инициатива +5 (теперь {target.Initiative:F0})", LogEntry.ColorSuccess);

        return $"{target.Name} вдохновлён";
    }

    private int CalculateInspirationGain(Npc target)
    {
        int gain = 5;

        if (target.CharTraits.Contains(CharacterTrait.Loyal)) gain += 5;
        if (target.CharTraits.Contains(CharacterTrait.Brave)) gain += 3;
        if (target.FollowerLevel >= 3) gain += 5;

        return Math.Min(gain, 20);
    }

    // ============================================================
    // 5. Запугать (Intimidate) - повышает страх, но может дать ресурсы
    // ============================================================
    private string ExecuteIntimidate(Npc target, Player player)
    {
        int fearGain = 15;
        double resourceChance = 0.3;

        target.Fear = Math.Min(100, target.Fear + fearGain);

        string result = $"Ты запугиваешь {target.Name}!\n  Страх +{fearGain} (теперь {target.Fear:F0})";

        // Шанс получить ресурсы
        if (_random.NextDouble() < resourceChance)
        {
            var randomResource = _db.GetAllResources().FirstOrDefault();
            if (randomResource != null)
            {
                double amount = _random.Next(1, 10);
                randomResource.Amount += amount;
                _db.SaveResource(randomResource);
                result += $"\n  Получено {amount} ед. {randomResource.Name}";
            }
        }

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social, $"Божество запугало (страх +{fearGain})"));
        _db.SaveNpc(target);

        Log(result, LogEntry.ColorWarning);

        return $"{target.Name} запуган";
    }

    // ============================================================
    // Общий метод расчета изменения доверия
    // ============================================================
    private int CalculateTrustChange(Npc target, string actionType)
    {
        int change = actionType == "Chat" ? 2 : 0;

        // Бонусы
        if (target.CharTraits.Contains(CharacterTrait.Empathetic)) change += 3;
        if (target.CharTraits.Contains(CharacterTrait.Loyal)) change += 2;

        // Штрафы
        if (target.CharTraits.Contains(CharacterTrait.Paranoid)) change -= 1;
        if (target.CharTraits.Contains(CharacterTrait.Greedy)) change -= 1;
        if (target.Health < 30) change -= 2;

        return Math.Max(0, change);
    }
}