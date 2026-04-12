using System;
using System.Collections.Generic;
using System.Linq;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Обробник передачі ресурсу
/// </summary>
public class TransferResourceHandler : BaseActionHandler
{
    public TransferResourceHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
        : base(db, rnd, logAction) { }

    public override string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        // Перевірка параметрів
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не вказано цільового NPC";

        if (!parameters.ContainsKey("resourceName") || parameters["resourceName"] == null)
            return "Не вказано ресурс";

        if (!parameters.ContainsKey("amount") || parameters["amount"] == null)
            return "Не вказано кількість";

        var target = parameters["targetNpc"] as Npc;
        var resourceName = parameters["resourceName"]?.ToString();
        var amount = Convert.ToDouble(parameters["amount"]);

        if (target == null) return "Помилка: невірний тип цільового NPC";
        if (string.IsNullOrEmpty(resourceName)) return "Помилка: невірна назва ресурсу";
        if (amount <= 0) return "Кількість має бути більше 0";

        // Перевірка живості NPC
        if (!target.IsAlive) return $"{target.Name} мертвий";

        // Пошук ресурсу
        var resource = resources.FirstOrDefault(r => r.Name == resourceName);
        if (resource == null) return $"Ресурс '{resourceName}' не знайдено";

        // Перевірка достатності ресурсу
        if (resource.Amount < amount)
            return $"Недостатньо '{resource.Name}'. Є: {resource.Amount:F0}, потрібно: {amount:F0}";

        // Передача ресурсу
        resource.Amount -= amount;
        _db.SaveResource(resource);

        // Задоволення потреб NPC (їжа, вода тощо)
        NeedSystem.SatisfyNeed(target, resourceName, amount * 0.5);
        _db.SaveNpc(target);

        return $"Передано {amount:F0} од. '{resource.Name}' → {target.Name}. Залишилось: {resource.Amount:F0}";
    }
}