namespace ApocMinimal.Models.PersonData;

public enum MonsterType { Zombie, Raider, Mutant, Beast, Boss }

public class Monster
{
    public string Name { get; set; } = "";
    public MonsterType Type { get; set; }
    public double Health { get; set; } = 100;
    public double DangerLevel { get; set; }

    /// <summary>Combat stats (same StatDefs IDs 1–10).</summary>
    public Dictionary<int, double> Stats { get; set; } = new();

    /// <summary>ОВ awarded on kill.</summary>
    public double OvReward { get; set; }
    /// <summary>Probability (0–1) of heart drop (+10 ОР).</summary>
    public double HeartChance { get; set; }

    public string TypeLabel => Type switch
    {
        MonsterType.Zombie => "Зомби",
        MonsterType.Raider => "Мародёр",
        MonsterType.Mutant => "Мутант",
        MonsterType.Beast => "Зверь",
        MonsterType.Boss => "Босс",
        _ => Type.ToString(),
    };
}

public static class MonsterFactory
{
    private static readonly string[][] _names =
    {
        new[] { "Бродячий мертвец", "Гнилой зомби", "Голодный мертвяк", "Медленный зомби" },
        new[] { "Вооружённый мародёр", "Бандит", "Головорез", "Дикий рейдер" },
        new[] { "Мутировавший человек", "Уродец", "Двуглавый мутант", "Кислотный мутант" },
        new[] { "Дикий пёс", "Огромная крыса", "Бешеный волк", "Заражённый медведь" },
        new[] { "Главарь банды", "Альфа-мутант", "Повелитель зомби", "Безумный учёный" },
    };

    public static Monster Generate(double dangerLevel, Random rnd)
    {
        var type = dangerLevel switch
        {
            < 20 => MonsterType.Zombie,
            < 40 => (MonsterType)rnd.Next(0, 2),
            < 60 => (MonsterType)rnd.Next(1, 4),
            < 80 => (MonsterType)rnd.Next(2, 5),
            _ => MonsterType.Boss,
        };

        double power = Math.Clamp(dangerLevel * 0.8 + rnd.Next(-10, 11), 10, 100);
        var nameArr = _names[(int)type];

        var stats = new Dictionary<int, double>();
        for (int i = 1; i <= 10; i++)
            stats[i] = Math.Clamp(power + rnd.Next(-15, 16), 10, 100);

        return new Monster
        {
            Name = nameArr[rnd.Next(nameArr.Length)],
            Type = type,
            Health = Math.Clamp(power * 1.5 + rnd.Next(-20, 21), 20, 300),
            DangerLevel = dangerLevel,
            Stats = stats,
            OvReward = type == MonsterType.Boss
                            ? Math.Round(dangerLevel * 0.5)
                            : Math.Round(dangerLevel * 0.2),
            HeartChance = type == MonsterType.Boss ? 0.5 : 0.15,
        };
    }
}
