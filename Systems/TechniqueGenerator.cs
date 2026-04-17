using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.Systems;

/// <summary>
/// Procedural technique generation: Element × Form × Effect.
/// Generates a unique Technique from the combination of these three axes.
/// </summary>
public static class TechniqueGenerator
{
    // ── Element axis ─────────────────────────────────────────────────────────

    private enum Element { Fire, Water, Earth, Wind, Lightning, Dark, Light }

    private static readonly (string RuName, TechniqueType Type, int[] PrimaryStats)[] _elements =
    {
        ("Огненная",     TechniqueType.Energy,   new[]{27, 23, 24}),  // Выход, Запас, Восст
        ("Водяная",      TechniqueType.Energy,   new[]{24, 28, 23}),  // Восст, Тонкость, Запас
        ("Земляная",     TechniqueType.Physical, new[]{2, 1, 8}),     // Стойкость, Выносл, Регенер
        ("Ветряная",     TechniqueType.Physical, new[]{6, 5, 9}),     // Ловкость, Рефлексы, Сенсор
        ("Молниеносная", TechniqueType.Physical, new[]{5, 3, 6}),     // Рефлексы, Сила, Ловкость
        ("Тёмная",       TechniqueType.Mental,   new[]{16, 19, 21}),  // Воля, Интуиция, Творчество
        ("Световая",     TechniqueType.Mental,   new[]{17, 26, 12}),  // Обучение, Концентрация, Память
    };

    // ── Form axis ────────────────────────────────────────────────────────────

    private static readonly (string RuName, double CostMult, double HealMult, string Suffix)[] _forms =
    {
        ("Волна",      1.0, 0.0, "волны"),
        ("Удар",       1.2, 0.0, "удара"),
        ("Взрыв",      1.5, 0.0, "взрыва"),
        ("Печать",     0.8, 0.0, "печати"),
        ("Домен",      2.0, 0.0, "домена"),
        ("Барьер",     1.1, 0.0, "барьера"),
        ("Исцеление",  0.9, 1.0, "исцеления"),  // Heal form
        ("Поток",      1.0, 0.0, "потока"),
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generate a random technique at the given level.
    /// Result can be used directly as a Technique.
    /// </summary>
    public static Technique Generate(TechniqueLevel level, Random rnd)
    {
        int elemIdx = rnd.Next(_elements.Length);
        int formIdx = rnd.Next(_forms.Length);

        var (elemName, techType, primaryStats) = _elements[elemIdx];
        var (formName, costMult, healMult, formSuffix) = _forms[formIdx];

        double multiplier = level.GetMultiplier();
        double chakraCost  = Math.Round(10 + multiplier * 5 * costMult);
        double staminaCost = Math.Round(5  + multiplier * 2 * costMult);
        double faithCost   = Math.Round(multiplier * 10 * costMult);

        string name = $"{elemName} техника {formSuffix}";
        string description;
        double healAmount = 0;
        var requiredStats = new Dictionary<int, double>();

        if (healMult > 0)
        {
            healAmount = multiplier * 15;
            description = $"Техника {elemName.ToLower()} {formSuffix}. Восстанавливает {healAmount:F0} ОЗ цели.";
        }
        else
        {
            int bonusPerStat = (int)(multiplier * 2);
            description = $"Техника {elemName.ToLower()} {formSuffix}. ";
            var statNames = primaryStats.Select(id => StatName(id)).ToArray();
            description += $"+{bonusPerStat} {string.Join(", ", statNames)}.";
        }

        // Require one primary stat ≥ level * 10
        if (primaryStats.Length > 0)
            requiredStats[primaryStats[0]] = Math.Min(90, (int)level * 10);

        return new Technique
        {
            Name          = name,
            Description   = description,
            AltarLevel    = level.MinAltarLevel(),
            FaithCost     = faithCost,
            TechLevel     = level,
            TechType      = techType,
            ChakraCost    = chakraCost,
            StaminaCost   = staminaCost,
            HealAmount    = healAmount,
            RequiredStats = requiredStats,
        };
    }

    /// <summary>Generate a batch of techniques across all levels.</summary>
    public static List<Technique> GenerateBatch(int countPerLevel, Random rnd)
    {
        var result = new List<Technique>();
        foreach (TechniqueLevel level in Enum.GetValues<TechniqueLevel>())
            for (int i = 0; i < countPerLevel; i++)
                result.Add(Generate(level, rnd));
        return result;
    }

    /// <summary>Generate a technique matching an NPC's strongest stat type.</summary>
    public static Technique GenerateForNpc(Npc npc, TechniqueLevel level, Random rnd)
    {
        // Pick element biased toward the NPC's dominant stat type
        int physTotal   = npc.Stats.Strength + npc.Stats.Endurance + npc.Stats.Agility;
        int mentalTotal = npc.Stats.Intelligence + npc.Stats.Will + npc.Stats.Focus;
        int energyTotal = npc.Stats.EnergyReserve + npc.Stats.Concentration + npc.Stats.Control;

        int[] elementPool;
        if (physTotal >= mentalTotal && physTotal >= energyTotal)
            elementPool = new[]{2, 3, 4};   // Earth, Wind, Lightning (Physical)
        else if (mentalTotal >= energyTotal)
            elementPool = new[]{5, 6};       // Dark, Light (Mental)
        else
            elementPool = new[]{0, 1};       // Fire, Water (Energy)

        int origElem = rnd.Next(_elements.Length);
        // 70% chance to pick from pool, 30% chance random
        int elemIdx = rnd.NextDouble() < 0.7 ? elementPool[rnd.Next(elementPool.Length)] : origElem;
        elemIdx = Math.Clamp(elemIdx, 0, _elements.Length - 1);

        var (elemName, techType, primaryStats) = _elements[elemIdx];
        int formIdx = rnd.Next(_forms.Length);
        var (formName, costMult, healMult, formSuffix) = _forms[formIdx];

        double multiplier = level.GetMultiplier();
        string name = $"{elemName} техника {formSuffix}";
        double healAmount = healMult > 0 ? multiplier * 15 : 0;
        int bonusPerStat = (int)(multiplier * 2);

        string description = healAmount > 0
            ? $"Техника {elemName.ToLower()} {formSuffix}. Восстанавливает {healAmount:F0} ОЗ."
            : $"Техника {elemName.ToLower()} {formSuffix}. +{bonusPerStat} {string.Join(", ", primaryStats.Select(StatName))}.";

        var requiredStats = new Dictionary<int, double>();
        if (primaryStats.Length > 0)
            requiredStats[primaryStats[0]] = Math.Min(90, (int)level * 10);

        return new Technique
        {
            Name          = name,
            Description   = description,
            AltarLevel    = level.MinAltarLevel(),
            FaithCost     = Math.Round(multiplier * 10 * costMult),
            TechLevel     = level,
            TechType      = techType,
            ChakraCost    = Math.Round(10 + multiplier * 5 * costMult),
            StaminaCost   = Math.Round(5  + multiplier * 2 * costMult),
            HealAmount    = healAmount,
            RequiredStats = requiredStats,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string StatName(int statId) => statId switch
    {
        1  => "Выносливость", 2  => "Стойкость",    3  => "Сила",
        4  => "Восст.(физ)",  5  => "Рефлексы",     6  => "Ловкость",
        7  => "Адаптация",    8  => "Регенерация",   9  => "Сенсорика",
        10 => "Долголетие",   11 => "Фокус",        12 => "Память",
        13 => "Логика",       14 => "Дедукция",     15 => "Интеллект",
        16 => "Воля",         17 => "Обучение",     18 => "Гибкость",
        19 => "Интуиция",     20 => "Соц.интеллект",21 => "Творчество",
        22 => "Математика",   23 => "Запас энергии",24 => "Восст.энерг.",
        25 => "Концентрация", 26 => "Контроль",     27 => "Выход",
        28 => "Тонкость",     29 => "Устойчивость", 30 => "Восприятие",
        _ => $"Ст.{statId}",
    };
}
