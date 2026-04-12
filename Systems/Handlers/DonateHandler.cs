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
/// Обробник отримання пожертви від NPC
/// </summary>
public class DonateHandler : BaseActionHandler
{
    private readonly Random _random;

    public DonateHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
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
        // Проверка параметров
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не вказано цільового NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Помилка: невірний тип цільового NPC";
        if (!target.IsAlive) return $"{target.Name} мертвий";

        // Расчет суммы пожертвования
        double donationAmount = CalculateDonation(target);

        if (donationAmount <= 0)
        {
            Log($"{target.Name} не может сделать пожертвование сейчас.", LogEntry.ColorWarning);
            return $"Пожертвование не получено";
        }

        // Уменьшаем веру NPC
        target.Faith = Math.Max(0, target.Faith - donationAmount);

        // Добавляем веру игроку
        player.FaithPoints += donationAmount;

        // Сохраняем изменения
        _db.SaveNpc(target);
        _db.SavePlayer(player);

        // Добавляем воспоминание
        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Divine, $"Пожертвовал {donationAmount:F0} веры Божеству"));
        _db.SaveNpc(target);

        // Выводим информацию в лог
        Log($"{target.Name} делает пожертвование:", LogEntry.ColorSpeech);
        Log($"  +{donationAmount:F0} веры получено", LogEntry.ColorAltarColor);

        // Дополнительная информация о факторах
        LogDonationFactors(target, donationAmount);

        return $"Пожертвование от {target.Name} получено";
    }

    /// <summary>
    /// Расчет суммы пожертвования
    /// </summary>
    private double CalculateDonation(Npc target)
    {
        double baseAmount = target.Faith * 0.3; // 30% от текущей веры

        // Модификаторы от уровня последователя
        double followerMod = target.FollowerLevel switch
        {
            0 => 0.5,      // Нейтральный - меньше жертвует
            1 => 0.8,      // Послушник
            2 => 1.0,      // Последователь
            3 => 1.2,      // Верный
            4 => 1.5,      // Преданный
            5 => 2.0,      // Фанатик
            _ => 1.0
        };

        // Модификатор от доверия
        double trustMod = 0.5 + target.Trust / 100.0;

        // Модификатор от черт характера
        double traitMod = 1.0;
        if (target.CharTraits.Contains(CharacterTrait.Loyal))
            traitMod *= 1.5;
        if (target.CharTraits.Contains(CharacterTrait.Greedy))
            traitMod *= 0.3;
        if (target.CharTraits.Contains(CharacterTrait.Generous))
            traitMod *= 1.3;
        if (target.CharTraits.Contains(CharacterTrait.Empathetic))
            traitMod *= 1.2;

        // Модификатор от здоровья
        double healthMod = target.Health / 100.0;

        // Модификатор от потребностей
        double needsMod = CalculateNeedsModifier(target);

        // Случайный фактор (0.7 - 1.3)
        double randomMod = 0.7 + _random.NextDouble() * 0.6;

        double donation = baseAmount * followerMod * trustMod * traitMod * healthMod * needsMod * randomMod;

        // Минимум 1, максимум не более текущей веры
        return Math.Clamp(Math.Floor(donation), 1, target.Faith);
    }

    /// <summary>
    /// Расчет модификатора от потребностей
    /// </summary>
    private double CalculateNeedsModifier(Npc target)
    {
        double modifier = 1.0;

        // Если есть критические потребности - меньше жертвует
        foreach (var need in target.Needs.Where(n => n.IsCritical))
        {
            modifier *= 0.5;
        }

        // Если есть срочные потребности - немного меньше
        foreach (var need in target.Needs.Where(n => n.IsUrgent && !n.IsCritical))
        {
            modifier *= 0.8;
        }

        return Math.Max(0.2, modifier);
    }

    /// <summary>
    /// Вывод информации о факторах пожертвования
    /// </summary>
    private void LogDonationFactors(Npc target, double donation)
    {
        Log("  Факторы пожертвования:", LogEntry.ColorNormal);
        Log($"    Вера NPC: {target.Faith + donation:F0} → {target.Faith:F0} (-{donation:F0})", LogEntry.ColorNormal);
        Log($"    Уровень последователя: {target.FollowerLabel}", LogEntry.ColorNormal);
        Log($"    Доверие: {target.Trust:F0}%", LogEntry.ColorNormal);

        if (target.CharTraits.Contains(CharacterTrait.Loyal))
            Log($"    Черта: Преданный (+50%)", LogEntry.ColorSuccess);
        if (target.CharTraits.Contains(CharacterTrait.Generous))
            Log($"    Черта: Щедрый (+30%)", LogEntry.ColorSuccess);
        if (target.CharTraits.Contains(CharacterTrait.Greedy))
            Log($"    Черта: Жадный (-70%)", LogEntry.ColorDanger);
    }
}