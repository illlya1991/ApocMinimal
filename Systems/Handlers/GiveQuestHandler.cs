using System;
using System.Collections.Generic;
using System.Linq;
using ApocMinimal.Database;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Обробник видачі квесту NPC
/// </summary>
public class GiveQuestHandler : BaseActionHandler
{
    public GiveQuestHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
        : base(db, rnd, logAction) { }

    public override string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не вказано цільового NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Помилка: невірний тип цільового NPC";
        if (!target.IsAlive) return $"{target.Name} мертвий";

        // Пошук доступних квестів
        var availableQuests = quests.Where(q => q.Status == QuestStatus.Available && q.AssignedNpcId == 0).ToList();
        if (availableQuests.Count == 0)
            return "Немає доступних квестів";

        // Вибираємо випадковий квест
        var quest = availableQuests[_rnd.Next(availableQuests.Count)];

        // Перевірка чи NPC може взяти квест
        if (target.Trait == NpcTrait.Coward && _rnd.NextDouble() < 0.5)
            return $"{target.Name} (Трус) відмовився від квесту \"{quest.Title}\"";

        // Призначаємо квест
        QuestSystem.AssignQuest(quest, target);
        _db.SaveNpc(target);
        _db.SaveQuest(quest);

        return $"{target.Name} взяв квест: \"{quest.Title}\" (залишилось {quest.DaysRemaining} дн.)";
    }
}