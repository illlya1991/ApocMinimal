namespace ApocMinimal.Models.PersonData;

public enum MonsterFactionId
{
    GoblinSwarms,
    IceArachnids,
    Ogres,
    EarthElementals,
    Succubi,
}

public class MonsterFaction
{
    public MonsterFactionId Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public double ThreatLevel { get; set; }
    public double ThreatGrowthPerDay { get; set; }

    public bool IsActive => ThreatLevel > 5;

    public string ThreatLabel => ThreatLevel switch
    {
        < 20 => "Низкая",
        < 40 => "Умеренная",
        < 60 => "Высокая",
        < 80 => "Критическая",
        _    => "ОПАСНОСТЬ",
    };

    public string ThreatColor => ThreatLevel switch
    {
        < 20 => "#4ade80",
        < 40 => "#facc15",
        < 60 => "#f97316",
        < 80 => "#ef4444",
        _    => "#dc2626",
    };
}

public static class MonsterFactionFactory
{
    public static List<MonsterFaction> CreateDefault() => new()
    {
        new MonsterFaction
        {
            Id = MonsterFactionId.GoblinSwarms,
            Name = "Гоблины-Стаи",
            Description = "Быстрые и многочисленные. Атакуют роями, воруют ресурсы и терроризируют периметр.",
            AttackPower = 3, Defense = 2,
            ThreatLevel = 15, ThreatGrowthPerDay = 2.0,
        },
        new MonsterFaction
        {
            Id = MonsterFactionId.IceArachnids,
            Name = "Ледяные Арахниды",
            Description = "Опутывают добычу паутиной льда. Снижают скорость передвижения и вызывают паралич страхом.",
            AttackPower = 5, Defense = 4,
            ThreatLevel = 10, ThreatGrowthPerDay = 1.5,
        },
        new MonsterFaction
        {
            Id = MonsterFactionId.Ogres,
            Name = "Огры",
            Description = "Медленные, но разрушительные. Уничтожают укрепления и наносят массовый урон.",
            AttackPower = 8, Defense = 7,
            ThreatLevel = 5, ThreatGrowthPerDay = 0.8,
        },
        new MonsterFaction
        {
            Id = MonsterFactionId.EarthElementals,
            Name = "Земляные Элементали",
            Description = "Поглощают ресурсы из земли. Появляются в локациях с залежами и блокируют добычу.",
            AttackPower = 6, Defense = 8,
            ThreatLevel = 8, ThreatGrowthPerDay = 1.2,
        },
        new MonsterFaction
        {
            Id = MonsterFactionId.Succubi,
            Name = "Суккубы",
            Description = "Соблазняют выживших, снижая преданность и доверие. Могут переманить последователей.",
            AttackPower = 4, Defense = 3,
            ThreatLevel = 12, ThreatGrowthPerDay = 1.8,
        },
    };

    public static List<string> SimulateDay(List<MonsterFaction> factions, Random rnd)
    {
        var logs = new List<string>();
        foreach (var f in factions)
        {
            double growth = f.ThreatGrowthPerDay * (0.8 + rnd.NextDouble() * 0.4);
            f.ThreatLevel = Math.Min(100, f.ThreatLevel + growth);

            if (f.ThreatLevel >= 80 && rnd.NextDouble() < 0.1)
                logs.Add($"⚠ {f.Name}: угроза критическая — возможное нашествие!");
        }
        return logs;
    }

    public static void ApplyDefeat(MonsterFaction faction, double damage)
    {
        faction.ThreatLevel = Math.Max(0, faction.ThreatLevel - damage);
    }
}
