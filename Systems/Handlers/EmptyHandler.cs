using System.Collections.Generic;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Пустий обробник для дій, які ще не реалізовані
/// </summary>
public class EmptyHandler : BaseActionHandler
{
    public EmptyHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        return "Ця дія ще не реалізована";
    }
}

public class PunishNpcHandler : BaseActionHandler
{
    public PunishNpcHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(Dictionary<string, object> parameters, Player player, List<Npc> npcs, List<Resource> resources, List<Quest> quests)
    {
        return "Покарання NPC (ще не реалізовано)";
    }
}

public class RewardNpcHandler : BaseActionHandler
{
    public RewardNpcHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(Dictionary<string, object> parameters, Player player, List<Npc> npcs, List<Resource> resources, List<Quest> quests)
    {
        return "Нагородження NPC (ще не реалізовано)";
    }
}

public class DonateHandler : BaseActionHandler
{
    public DonateHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(Dictionary<string, object> parameters, Player player, List<Npc> npcs, List<Resource> resources, List<Quest> quests)
    {
        return "Отримання пожертви (ще не реалізовано)";
    }
}

public class TeachTechniqueHandler : BaseActionHandler
{
    public TeachTechniqueHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(Dictionary<string, object> parameters, Player player, List<Npc> npcs, List<Resource> resources, List<Quest> quests)
    {
        return "Навчання техніці (ще не реалізовано)";
    }
}

public class DemandResourceHandler : BaseActionHandler
{
    public DemandResourceHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(Dictionary<string, object> parameters, Player player, List<Npc> npcs, List<Resource> resources, List<Quest> quests)
    {
        return "Вимога ресурсу (ще не реалізовано)";
    }
}

public class AssignPublicQuestHandler : BaseActionHandler
{
    public AssignPublicQuestHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(Dictionary<string, object> parameters, Player player, List<Npc> npcs, List<Resource> resources, List<Quest> quests)
    {
        return "Публічний квест (ще не реалізовано)";
    }
}

public class CompleteQuestHandler : BaseActionHandler
{
    public CompleteQuestHandler(Database.DatabaseManager db, Random rnd) : base(db, rnd) { }

    public override string Execute(Dictionary<string, object> parameters, Player player, List<Npc> npcs, List<Resource> resources, List<Quest> quests)
    {
        return "Завершення квесту (ще не реалізовано)";
    }
}