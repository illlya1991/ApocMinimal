using System;
using System.Collections.Generic;
using System.Linq;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Обробник розмови з NPC
/// </summary>
public class ChatHandler : BaseActionHandler
{
    public ChatHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

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

        // Генерація відповіді на основі рис NPC та рівня довіри
        string response = (target.Trait, target.Trust) switch
        {
            (NpcTrait.Loner, < 50) => "...(пожимає плечами і відводить погляд)",
            (NpcTrait.Loner, _) => "Я справлюсь сам. Дякую.",
            (NpcTrait.Coward, _) when target.Health < 40 => "Будь ласка, не кидай нас! Я боюся...",
            (NpcTrait.Coward, _) => "Я постараюся. Тільки це не небезпечно?",
            (NpcTrait.Leader, > 60) => "Я вірю в тебе, Божество! Веду інших вперед.",
            (_, >= 70) => "Я вірю в тебе! Ми виживемо разом.",
            (_, >= 40) => "Дякую за турботу. Намагаюся триматися.",
            _ => "Важко. Не знаю, чи є сенс продовжувати..."
        };

        // Перевірка критичних потреб
        var urgentNeed = NeedSystem.GetMostUrgentNeed(target);
        if (urgentNeed != null && urgentNeed.IsCritical)
            response += $" Мені терміново потрібно: {urgentNeed.Name}!";

        // Збільшення довіри
        target.Trust = Math.Min(100, target.Trust + 2);
        _db.SaveNpc(target);

        // Додавання спогаду
        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Social, "Поговорив з Божеством"));
        _db.SaveNpc(target);

        return $"{target.Name}: «{response}»";
    }
}