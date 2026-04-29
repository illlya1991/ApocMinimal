using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.Systems;

/// <summary>
/// Generates 1050 catalog techniques for the template DB:
/// 5 general × 10 levels = 50, plus 20 faction × 10 levels × 5 factions = 1000.
/// </summary>
public static class TechniqueSeeder
{
    // TechLevel by terminal level (0-indexed, lvl 1 = Initiate)
    private static readonly TechniqueLevel[] LevelMap =
    {
        TechniqueLevel.Initiate,   // lvl 1
        TechniqueLevel.Adept,      // lvl 2
        TechniqueLevel.Warrior,    // lvl 3
        TechniqueLevel.Veteran,    // lvl 4
        TechniqueLevel.Master,     // lvl 5
        TechniqueLevel.GrandMaster,// lvl 6
        TechniqueLevel.Phantom,    // lvl 7
        TechniqueLevel.Legend,     // lvl 8
        TechniqueLevel.Vessel,     // lvl 9
        TechniqueLevel.Apex,       // lvl 10
    };

    // Required stat IDs per TechType (checked at level >= 3)
    private static readonly int[] ReqStatPhys   = { 1, 1, 3, 9, 2, 6, 7 };
    private static readonly int[] ReqStatMental  = { 16, 21, 12, 19, 13, 17, 11 };
    private static readonly int[] ReqStatEnergy  = { 23, 26, 25, 27, 24, 28 };

    // ── Faction technique name pools [Physical×7, Mental×7, Energy×6] ───────

    private static readonly (string Faction, string[] Phys, string[] Ment, string[] Ener)[] FactionPools =
    {
        ("ElementMages",
            new[] { "Огненный удар",        "Ветряной выпад",         "Земляной бросок",
                    "Водяной хват",          "Световая атака",         "Стихийная форма",
                    "Молниеносный рывок" },
            new[] { "Стихийный разум",       "Природная интуиция",     "Энергетическое видение",
                    "Элементальный анализ",  "Ментальный резонанс",    "Восприятие потоков",
                    "Аналитика стихий" },
            new[] { "Огненный поток",        "Водяная волна",          "Земляной барьер",
                    "Ветряной вихрь",        "Световая аура",          "Тёмный импульс" }),

        ("PathBlades",
            new[] { "Удар клинка",           "Стальной выпад",         "Боевой приём",
                    "Воинская атака",        "Мощный удар",            "Стремительная серия",
                    "Точный выпад" },
            new[] { "Боевой расчёт",         "Тактическая интуиция",   "Воля воина",
                    "Стальной разум",        "Концентрация бойца",     "Боевое предвидение",
                    "Воинский фокус" },
            new[] { "Боевая аура",           "Энергия воина",          "Внутренняя сила",
                    "Стальной поток",        "Воинская волна",         "Дух пути" }),

        ("MirrorHealers",
            new[] { "Зеркальный щит",        "Целительный удар",       "Восстановительная стойка",
                    "Исцеляющий жест",       "Защитная форма",         "Бодрящий приём",
                    "Регенерирующий хват" },
            new[] { "Зеркальный разум",      "Ментальное исцеление",   "Эмпатия целителя",
                    "Психическое восстановление", "Исцеляющая интуиция", "Зеркальное сознание",
                    "Воля врачевателя" },
            new[] { "Исцеляющий поток",      "Зеркальная аура",        "Целительная волна",
                    "Световое исцеление",    "Восстановительный импульс", "Отражение энергии" }),

        ("DeepSmiths",
            new[] { "Кузнечный удар",        "Глубинная сила",         "Каменная хватка",
                    "Стальной кулак",        "Гномья стойка",          "Рудный щит",
                    "Подземный бросок" },
            new[] { "Кузнечная логика",      "Инженерный расчёт",      "Глубинная мудрость",
                    "Мастерство ремесла",    "Технический анализ",     "Гномья память",
                    "Архитектурная интуиция" },
            new[] { "Кузнечная аура",        "Подземный резерв",       "Рудная волна",
                    "Стальная сила духа",    "Горный поток",           "Земная энергия" }),

        ("GuardHeralds",
            new[] { "Стражевый щит",         "Вестнический маневр",    "Защитный удар",
                    "Патрульная стойка",     "Барьерная форма",        "Охранный приём",
                    "Дозорный жест" },
            new[] { "Стражевая воля",        "Дипломатическое предвидение", "Вестнический интеллект",
                    "Тактика защиты",        "Социальная интуиция",    "Ментальный барьер",
                    "Охранный фокус" },
            new[] { "Барьерная аура",        "Стражевый поток",        "Вестническая волна",
                    "Защитный барьер",       "Световая охрана",        "Договорная сила" }),
    };

    // General pool [Physical×2, Mental×2, Energy×1]
    private static readonly string[] GenPhys = { "Базовый удар",      "Физическая подготовка" };
    private static readonly string[] GenMent = { "Укрепление воли",   "Базовая концентрация" };
    private static readonly string[] GenEner = { "Накопление энергии" };

    // ── Public API ────────────────────────────────────────────────────────────

    public static IEnumerable<Technique> GetAll()
    {
        // 50 general techniques
        for (int lvl = 1; lvl <= 10; lvl++)
        {
            for (int i = 0; i < GenPhys.Length; i++)
                yield return Make("", $"gen_p{i + 1}_l{lvl}", GenPhys[i], TechniqueType.Physical, lvl, i);
            for (int i = 0; i < GenMent.Length; i++)
                yield return Make("", $"gen_m{i + 1}_l{lvl}", GenMent[i], TechniqueType.Mental,   lvl, i);
            for (int i = 0; i < GenEner.Length; i++)
                yield return Make("", $"gen_e{i + 1}_l{lvl}", GenEner[i], TechniqueType.Energy,   lvl, i);
        }

        // 1000 faction techniques
        foreach (var (faction, phys, ment, ener) in FactionPools)
        {
            for (int lvl = 1; lvl <= 10; lvl++)
            {
                string lvlSuffix = Roman(lvl);
                for (int i = 0; i < phys.Length; i++)
                    yield return Make(faction, $"{faction}_p{i + 1}_l{lvl}",
                        $"{phys[i]} {lvlSuffix}", TechniqueType.Physical, lvl, i, faction);
                for (int i = 0; i < ment.Length; i++)
                    yield return Make(faction, $"{faction}_m{i + 1}_l{lvl}",
                        $"{ment[i]} {lvlSuffix}", TechniqueType.Mental,   lvl, i, faction);
                for (int i = 0; i < ener.Length; i++)
                    yield return Make(faction, $"{faction}_e{i + 1}_l{lvl}",
                        $"{ener[i]} {lvlSuffix}", TechniqueType.Energy,   lvl, i, faction, healIfHealer: true);
            }
        }
    }

    // ── Internal builder ──────────────────────────────────────────────────────

    private static Technique Make(
        string faction,
        string key,
        string name,
        TechniqueType techType,
        int termLvl,
        int idx,
        string factionCtx = "",
        bool healIfHealer = false)
    {
        double healAmount = 0;
        if (healIfHealer && factionCtx == "MirrorHealers")
            healAmount = termLvl * 5.0;

        var t = new Technique
        {
            Name          = name,
            Description   = BuildDesc(name, techType, termLvl, healAmount),
            TerminalLevel = termLvl,
            TechLevel     = LevelMap[termLvl - 1],
            TechType      = techType,
            OPCost        = 50.0 * termLvl,
            EnergyCost    = 5.0  + termLvl * 4,
            StaminaCost   = 3.0  + termLvl * 2,
            HealAmount    = healAmount,
            Faction       = faction,
            CatalogKey    = key,
        };

        // Stat prerequisites starting from level 3
        if (termLvl >= 3)
        {
            int[] pool = techType switch
            {
                TechniqueType.Physical => ReqStatPhys,
                TechniqueType.Mental   => ReqStatMental,
                _                      => ReqStatEnergy,
            };
            int statId = pool[idx % pool.Length];
            t.RequiredStats[statId] = (termLvl - 2) * 10.0;
        }

        return t;
    }

    private static string BuildDesc(string name, TechniqueType type, int lvl, double heal)
    {
        string typeLabel = type switch
        {
            TechniqueType.Physical => "физические",
            TechniqueType.Mental   => "ментальные",
            _                      => "энергетические",
        };
        string suffix = heal > 0
            ? $" Восстанавливает {heal:F0} ОЗ цели."
            : $" Усиливает {typeLabel} характеристики НПС.";
        return $"{name}.{suffix}";
    }

    private static string Roman(int n) => n switch
    {
        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
        6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", _ => "X",
    };
}
