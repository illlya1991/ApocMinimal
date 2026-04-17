namespace ApocMinimal.Models.PersonData.NpcData;

/// <summary>
/// An injury affecting an NPC's stat performance.
/// Heals over time (HealDaysLeft decreases each day).
/// </summary>
public class Injury
{
    public int Id { get; set; }
    public int NpcId { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Stat number (1–30) affected by this injury.</summary>
    public int AffectedStatId { get; set; }

    /// <summary>Negative penalty applied to the affected stat (stored as positive value).</summary>
    public int Penalty { get; set; }

    /// <summary>Days until this injury heals completely.</summary>
    public int HealDaysLeft { get; set; }

    public bool IsHealed => HealDaysLeft <= 0;

    public string Label => $"{Name} (-{Penalty} ст.{AffectedStatId}, {HealDaysLeft}д.)";
}

/// <summary>Pre-defined injury types by category.</summary>
public static class InjuryTypes
{
    public static readonly (string Name, int StatId, int Penalty, int Days)[] All =
    {
        // Physical injuries
        ("Растяжение лодыжки",  6,  20, 3),   // Ловкость -20, 3 days
        ("Ушиб руки",           3,  15, 2),   // Сила -15
        ("Перелом ребра",       1,  25, 7),   // Выносливость -25
        ("Порез",               4,  10, 2),   // Восстановление -10
        ("Ожог",                7,  20, 5),   // Адаптация -20
        ("Сотрясение мозга",   11,  30, 5),   // Фокус -30
        ("Глубокая рана",       8,  20, 6),   // Регенерация -20
        ("Контузия",            5,  15, 3),   // Рефлексы -15
        // Illness
        ("Отравление",          1,  20, 4),   // Выносливость -20
        ("Лихорадка",          15,  25, 3),   // Интеллект -25
        ("Истощение",           1,  30, 2),   // Выносливость -30
        // Energy injuries
        ("Чакровый сбой",      23,  20, 4),   // Запас энергии -20
        ("Перерасход ци",      24,  25, 3),   // Восстановление энергии -25
    };

    public static (string Name, int StatId, int Penalty, int Days) GetRandom(Random rnd)
        => All[rnd.Next(All.Length)];

    public static (string Name, int StatId, int Penalty, int Days) FromCombatDamage(double damage, Random rnd)
    {
        // Heavy damage → more severe injury
        var candidates = damage > 30 ? All.Where(i => i.Penalty >= 20).ToArray()
                       : damage > 15 ? All.Where(i => i.Penalty >= 10).ToArray()
                       : All.Where(i => i.Penalty < 20).ToArray();
        var pool = candidates.Length > 0 ? candidates : All;
        return pool[rnd.Next(pool.Length)];
    }
}
