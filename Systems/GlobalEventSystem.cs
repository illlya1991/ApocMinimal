using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

public enum GlobalEventType
{
    PresidentOffer,    // President offers resources in exchange for followers/faith
    ResourceDrop,      // Helicopter/supply drop arrives
    HazardStorm,       // Environmental hazard (damage/need spike)
    MonsterSurge,      // Increased monster danger in all locations
    NpcMigration,      // New survivors arrive at base
    RadioBroadcast,    // Informational event (no game effect, flavour)
}

public class GlobalEvent
{
    public int Day { get; set; }
    public GlobalEventType Type { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Accept/reject choice — if null, event is automatic.</summary>
    public GlobalEventChoice? Choice { get; set; }
    public bool IsHandled { get; set; }
}

public class GlobalEventChoice
{
    public string AcceptLabel { get; set; } = "Принять";
    public string RejectLabel { get; set; } = "Отказать";
    /// <summary>Faith cost to accept (negative = faith gain).</summary>
    public double FaithCost { get; set; }
    public string? ResourceName { get; set; }
    public double ResourceAmount { get; set; }
    /// <summary>Number of followers the president demands in exchange.</summary>
    public int FollowerDemand { get; set; }
}

/// <summary>
/// Generates and checks global world events triggered on specific days.
/// Events fire on a schedule and randomly throughout the game.
/// </summary>
public static class GlobalEventSystem
{
    // Periodic event schedule: day modulo → event type
    private static readonly (int Modulo, int Offset, GlobalEventType Type)[] _schedule =
    {
        (7,  0, GlobalEventType.PresidentOffer),   // every 7 days
        (5,  2, GlobalEventType.ResourceDrop),     // every 5 days (offset 2)
        (10, 4, GlobalEventType.HazardStorm),      // every 10 days
        (14, 7, GlobalEventType.MonsterSurge),     // every 14 days
        (21, 3, GlobalEventType.NpcMigration),     // every 21 days
        (3,  1, GlobalEventType.RadioBroadcast),   // every 3 days (flavor)
    };

    private static readonly string[] _radioMessages =
    {
        "Сигнал поймал обрывок речи — кто-то жив в северном квартале.",
        "Помехи. Затем голос: «Держитесь. Помощь идёт».",
        "Частота 89.4 — кто-то передаёт координаты безопасного укрытия.",
        "Из радио: «День 30. Мы ещё здесь. Не сдавайтесь».",
        "Сигнал SOS из района порта. Повторяется каждые 15 минут.",
    };

    private static readonly string[] _stormDescriptions =
    {
        "Кислотный дождь накрыл город. Все открытые потребности резко возросли.",
        "Магнитная буря нарушила работу электроники. Страх вырос у всех выживших.",
        "Радиоактивный туман накрыл несколько кварталов. Риск болезни повышен.",
        "Волна тепла. Потребность в воде удвоилась на следующие 2 дня.",
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Check if any global event fires on the given day.</summary>
    public static GlobalEvent? CheckDay(int day, Player player, List<Npc> npcs, Random rnd)
    {
        foreach (var (modulo, offset, type) in _schedule)
        {
            if ((day - offset) % modulo == 0 && day >= offset)
                return BuildEvent(day, type, player, npcs, rnd);
        }
        return null;
    }

    /// <summary>Apply the consequences of a player accepting a choice event.</summary>
    public static string ApplyAccept(GlobalEvent evt, Player player, List<Resource> resources, List<Npc> npcs)
    {
        var c = evt.Choice;
        if (c == null) return "";
        evt.IsHandled = true;

        player.FaithPoints -= c.FaithCost;

        if (!string.IsNullOrEmpty(c.ResourceName) && c.ResourceAmount > 0)
        {
            var res = resources.FirstOrDefault(r => r.Name == c.ResourceName);
            if (res != null) res.Amount += c.ResourceAmount;
        }

        if (c.FollowerDemand > 0)
        {
            // Highest-level followers leave first
            int removed = 0;
            var leaving = npcs.Where(n => n.IsAlive && n.FollowerLevel > 0)
                              .OrderByDescending(n => n.FollowerLevel)
                              .Take(c.FollowerDemand).ToList();
            foreach (var npc in leaving) { npc.FollowerLevel = 0; removed++; }
            return $"Президент забрал {removed} последователей. Получено: {c.ResourceAmount:F0} {c.ResourceName}";
        }

        return $"Принято. Получено: {c.ResourceAmount:F0} {c.ResourceName}";
    }

    /// <summary>Apply automatic event effects (no choice needed).</summary>
    public static string ApplyAuto(GlobalEvent evt, Player player, List<Resource> resources, List<Npc> npcs, Random rnd)
    {
        evt.IsHandled = true;
        return evt.Type switch
        {
            GlobalEventType.ResourceDrop   => ApplyResourceDrop(evt, resources, rnd),
            GlobalEventType.HazardStorm    => ApplyHazardStorm(npcs, rnd),
            GlobalEventType.MonsterSurge   => "Монстры активизировались. Опасность локаций повышена.",
            GlobalEventType.NpcMigration   => ApplyNpcMigration(npcs, rnd),
            GlobalEventType.RadioBroadcast => "",
            _                              => "",
        };
    }

    // ── Event builders ────────────────────────────────────────────────────────

    private static GlobalEvent BuildEvent(int day, GlobalEventType type,
        Player player, List<Npc> npcs, Random rnd)
    {
        return type switch
        {
            GlobalEventType.PresidentOffer  => BuildPresidentOffer(day, player, npcs, rnd),
            GlobalEventType.ResourceDrop    => BuildResourceDrop(day, rnd),
            GlobalEventType.HazardStorm     => BuildHazardStorm(day, rnd),
            GlobalEventType.MonsterSurge    => BuildMonsterSurge(day),
            GlobalEventType.NpcMigration    => BuildNpcMigration(day, rnd),
            GlobalEventType.RadioBroadcast  => BuildRadioBroadcast(day, rnd),
            _                              => new GlobalEvent { Day = day, Type = type },
        };
    }

    private static GlobalEvent BuildPresidentOffer(int day, Player player, List<Npc> npcs, Random rnd)
    {
        int followers = npcs.Count(n => n.IsAlive && n.FollowerLevel > 0);
        int demand = Math.Max(1, (int)(followers * 0.1) + rnd.Next(1, 4));
        var resources = new[] { "Еда", "Медикаменты", "Инструменты", "Дерево" };
        string res = resources[rnd.Next(resources.Length)];
        double amount = demand * rnd.Next(10, 21);

        return new GlobalEvent
        {
            Day = day, Type = GlobalEventType.PresidentOffer,
            Title = "Предложение Президента",
            Description = $"Президент предлагает {amount:F0} ед. {res} в обмен на {demand} последователей.",
            Choice = new GlobalEventChoice
            {
                AcceptLabel     = "Принять сделку",
                RejectLabel     = "Отказать",
                FaithCost       = 0,
                ResourceName    = res,
                ResourceAmount  = amount,
                FollowerDemand  = demand,
            },
        };
    }

    private static GlobalEvent BuildResourceDrop(int day, Random rnd)
    {
        var resources = new[] { "Еда", "Вода", "Медикаменты", "Инструменты" };
        string res = resources[rnd.Next(resources.Length)];
        double amount = rnd.Next(20, 61) * 5;
        return new GlobalEvent
        {
            Day = day, Type = GlobalEventType.ResourceDrop,
            Title = "Гуманитарный сброс",
            Description = $"Вертолёт сбросил груз: {amount:F0} ед. {res}.",
            Choice = new GlobalEventChoice
            {
                AcceptLabel = "Забрать груз",
                RejectLabel = "Игнорировать",
                FaithCost = -5, ResourceName = res, ResourceAmount = amount,
            },
        };
    }

    private static GlobalEvent BuildHazardStorm(int day, Random rnd)
        => new GlobalEvent
        {
            Day = day, Type = GlobalEventType.HazardStorm,
            Title = "Природная катастрофа",
            Description = _stormDescriptions[rnd.Next(_stormDescriptions.Length)],
        };

    private static GlobalEvent BuildMonsterSurge(int day)
        => new GlobalEvent
        {
            Day = day, Type = GlobalEventType.MonsterSurge,
            Title = "Нашествие монстров",
            Description = "Волна мутантов движется через город. Опасность локаций временно удвоена.",
        };

    private static GlobalEvent BuildNpcMigration(int day, Random rnd)
    {
        int count = rnd.Next(1, 4);
        return new GlobalEvent
        {
            Day = day, Type = GlobalEventType.NpcMigration,
            Title = "Прибытие выживших",
            Description = $"К базе вышли {count} выживших. Они просят убежища.",
            Choice = new GlobalEventChoice
            {
                AcceptLabel = $"Принять ({count} чел.)",
                RejectLabel = "Отказать",
                FaithCost = -10 * count,
            },
        };
    }

    private static GlobalEvent BuildRadioBroadcast(int day, Random rnd)
        => new GlobalEvent
        {
            Day = day, Type = GlobalEventType.RadioBroadcast,
            Title = "Радиосигнал",
            Description = _radioMessages[rnd.Next(_radioMessages.Length)],
        };

    // ── Auto-apply helpers ────────────────────────────────────────────────────

    private static string ApplyResourceDrop(GlobalEvent evt, List<Resource> resources, Random rnd)
    {
        if (evt.Choice == null) return "";
        var res = resources.FirstOrDefault(r => r.Name == evt.Choice.ResourceName);
        if (res != null) res.Amount += evt.Choice.ResourceAmount;
        return $"Груз получен: +{evt.Choice.ResourceAmount:F0} {evt.Choice.ResourceName}";
    }

    private static string ApplyHazardStorm(List<Npc> npcs, Random rnd)
    {
        foreach (var npc in npcs.Where(n => n.IsAlive))
        {
            npc.Fear = Math.Min(100, npc.Fear + rnd.Next(5, 16));
            npc.Health = Math.Max(0, npc.Health - rnd.Next(3, 11));
        }
        return "Природная катастрофа нанесла урон всем выжившим.";
    }

    private static string ApplyNpcMigration(List<Npc> npcs, Random rnd)
    {
        // Placeholder — actual NPC generation would require NpcGenerator
        return "Новые выжившие добавлены в группу.";
    }
}
