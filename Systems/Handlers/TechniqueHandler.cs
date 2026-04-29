using System;
using System.Collections.Generic;
using System.Linq;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.Models.UIData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Обработчик обучения NPC техникам
/// </summary>
public class TechniqueHandler : BaseActionHandler
{
    public TechniqueHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
        : base(db, rnd, logAction) { }

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

        if (actionKey != "TeachTechnique")
            return $"Неизвестное действие: {actionKey}";

        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не указан целевой NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Ошибка: неверный тип целевого NPC";
        if (!target.IsAlive) return $"{target.Name} мертв";

        if (!parameters.ContainsKey("technique") || parameters["technique"] == null)
            return "Не указана техника";

        var technique = parameters["technique"] as Technique;
        if (technique == null) return "Ошибка: неверный тип техники";

        // Проверка уровня алтаря
        if (technique.TerminalLevel > player.TerminalLevel)
            return $"Техника «{technique.Name}» требует уровень Терминала {technique.TerminalLevel} (у вас {player.TerminalLevel})";

        // Проверка веры
        if (player.DevPoints < technique.OPCost)
            return $"Недостаточно веры для обучения (нужно {technique.OPCost:F0}, есть {player.DevPoints:F0})";

        // Проверка способностей NPC
        if (!CheckNPCAbility(target, technique))
            return $"{target.Name} не способен изучить эту технику";

        // Применяем технику
        bool success = TechniqueSystem.Apply(technique, target, out string techLog);
        if (!success)
            return techLog;

        // Списываем веру
        player.DevPoints -= technique.OPCost;
        _db.SavePlayer(player);
        _db.SaveNpc(target);

        // Бонус к доверию
        int trustGain = (int)Math.Min(10, technique.OPCost / 10);
        target.Trust = Math.Min(100, target.Trust + trustGain);
        _db.SaveNpc(target);

        // Логирование
        Log($"  {target.Name} изучил технику:", LogEntry.ColorSuccess);
        Log($"    «{technique.Name}»", LogEntry.ColorSpeech);
        Log($"    Стоимость: {technique.OPCost:F0} веры", LogEntry.ColorNormal);
        Log($"    Доверие +{trustGain}", LogEntry.ColorSuccess);

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social,
            $"Координатор обучил меня технике «{technique.Name}»"));

        return $"Техника «{technique.Name}» передана {target.Name}";
    }

    private bool CheckNPCAbility(Npc target, Technique technique)
    {
        // Проверка требований к характеристикам
        foreach (var (statId, minVal) in technique.RequiredStats)
        {
            int statValue = target.Stats.GetStatValue(statId);
            if (statValue < minVal)
                return false;
        }

        // Проверка энергии и выносливости
        if (target.Energy < technique.EnergyCost)
            return false;
        if (target.Stamina < technique.StaminaCost)
            return false;

        return true;
    }
}