using System.Collections.Generic;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Обробник перегляду інформації про NPC
/// </summary>
public class ViewInfoHandler : BaseActionHandler
{
    public ViewInfoHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        // Очікуємо параметр 'targetNpc'
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не вказано цільового NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Помилка: невірний тип цільового NPC";

        // Інформація буде показана через лог, повертаємо коротке повідомлення
        return $"Переглянуто інформацію про {target.Name}";
    }
}