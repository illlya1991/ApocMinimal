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
/// Обробник розмови з NPC
/// </summary>
public class ChatHandler : BaseActionHandler
{
    private readonly Random _random;

    public ChatHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
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

        // Генерация ответа на основе черт NPC и уровня доверия
        string response = GenerateResponse(target);

        // Проверка критических потреб
        var urgentNeed = NeedSystem.GetMostUrgentNeed(target);
        if (urgentNeed != null && urgentNeed.IsCritical)
            response += $" Мне срочно нужно: {urgentNeed.Name}!";

        // Увеличение доверия (при успешном разговоре)
        int trustGain = CalculateTrustGain(target);
        target.Trust = Math.Min(100, target.Trust + trustGain);
        _db.SaveNpc(target);

        // Добавление воспоминания
        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social, "Поговорил с Божеством"));
        _db.SaveNpc(target);

        // Выводим диалог в лог
        Log($"Ты обращаешься к {target.Name}:", LogEntry.ColorNormal);
        Log($"  {target.Name}: «{response}»", LogEntry.ColorSpeech);

        // Дополнительная информация о изменении доверия
        if (trustGain > 0)
            Log($"  Доверие +{trustGain}", LogEntry.ColorSuccess);
        else if (trustGain < 0)
            Log($"  Доверие {trustGain}", LogEntry.ColorDanger);

        return $"Разговор с {target.Name} завершён";
    }

    /// <summary>
    /// Генерация ответа NPC на основе его черт и уровня доверия
    /// </summary>
    private string GenerateResponse(Npc target)
    {
        // Базовые ответы в зависимости от черт характера
        if (target.CharTraits.Contains(CharacterTrait.Loyal))
            return "Я всегда верю в тебя, Божество!";

        if (target.CharTraits.Contains(CharacterTrait.Cowardly))
            return target.Health < 40
                ? "Пожалуйста, не бросай нас! Я боюсь..."
                : "Я постараюсь. Только это не опасно?";

        if (target.CharTraits.Contains(CharacterTrait.Greedy))
            return "Что ты можешь мне дать за разговор?";

        if (target.CharTraits.Contains(CharacterTrait.Paranoid))
            return "...(подозрительно оглядывается и ничего не говорит)";

        if (target.CharTraits.Contains(CharacterTrait.Lazy))
            return "*зевает* ... Может, позже?";

        if (target.CharTraits.Contains(CharacterTrait.Curious))
            return "Расскажи мне что-нибудь интересное!";

        // Ответы в зависимости от типа NPC
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

        // Ответы в зависимости от уровня доверия
        if (target.Trust >= 70)
            return "Я верю в тебя! Мы выживем вместе.";
        if (target.Trust >= 40)
            return "Спасибо за заботу. Стараюсь держаться.";

        return "Тяжело. Не знаю, есть ли смысл продолжать...";
    }

    /// <summary>
    /// Расчет изменения доверия после разговора
    /// </summary>
    private int CalculateTrustGain(Npc target)
    {
        int baseGain = 2;

        // Бонусы за черты характера
        if (target.CharTraits.Contains(CharacterTrait.Empathetic))
            baseGain += 3;
        if (target.CharTraits.Contains(CharacterTrait.Loyal))
            baseGain += 2;

        // Штрафы за черты
        if (target.CharTraits.Contains(CharacterTrait.Paranoid))
            baseGain -= 1;
        if (target.CharTraits.Contains(CharacterTrait.Greedy))
            baseGain -= 1;

        // Штраф за низкое здоровье
        if (target.Health < 30)
            baseGain -= 2;

        // Бонус за критическую потребность (если помочь)
        var urgentNeed = NeedSystem.GetMostUrgentNeed(target);
        if (urgentNeed != null && urgentNeed.IsCritical)
            baseGain -= 1; // Не помогаем, а просто говорим

        return Math.Max(0, baseGain);
    }
}