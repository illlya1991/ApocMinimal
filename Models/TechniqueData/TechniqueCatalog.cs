namespace ApocMinimal.Models.TechniqueData;

public record TechniqueCatalogEntry(
    string AbilityKey,
    string Name,
    TechniqueLevel TechLevel,
    TechniqueType TechType,
    double OvCost,
    string Description,
    Dictionary<int, double> StatBonus
);

public record PassiveUpgrade(
    string AbilityKey,
    string Name,
    TechniqueLevel Rank,
    int StatId,
    double StatAmount,
    double OvCost,
    string Description
);

public static class TechniqueCatalog
{
    public static readonly TechniqueCatalogEntry[] Techniques =
    {
        // ── Genin (altar ≥ 1) ────────────────────────────────────────────
        new("tech_genin_strike",    "Удар силы",
            TechniqueLevel.Genin,  TechniqueType.Physical, 150,
            "Базовый физический удар. +3 Сила, +2 Ловкость.",
            new() { [1]=3, [2]=2 }),

        new("tech_genin_wave",      "Волна чакры",
            TechniqueLevel.Genin,  TechniqueType.Energy,   150,
            "Слабый выброс энергии. +3 Запас энергии, +2 Концентрация.",
            new() { [23]=3, [25]=2 }),

        new("tech_genin_focus",     "Острый ум",
            TechniqueLevel.Genin,  TechniqueType.Mental,   150,
            "Краткое усиление концентрации. +3 Фокус, +2 Логика.",
            new() { [16]=3, [12]=2 }),

        // ── Elite Genin (altar ≥ 2) ───────────────────────────────────────
        new("tech_elite_genin_fireball", "Огненный шар",
            TechniqueLevel.EliteGenin, TechniqueType.Energy, 200,
            "Техника огненной чакры. +5 Запас энергии, +3 Макс. выход.",
            new() { [23]=5, [27]=3 }),

        new("tech_elite_genin_fist", "Железный кулак",
            TechniqueLevel.EliteGenin, TechniqueType.Physical, 200,
            "Усиленный физический удар. +5 Сила, +3 Выносливость.",
            new() { [1]=5, [3]=3 }),

        new("tech_elite_genin_scan", "Сенсорная волна",
            TechniqueLevel.EliteGenin, TechniqueType.Mental, 200,
            "Расширенное восприятие окружения. +5 Сенсорика, +3 Интуиция.",
            new() { [4]=5, [17]=3 }),

        // ── Chunin (altar ≥ 3) ────────────────────────────────────────────
        new("tech_chunin_illusion",  "Иллюзия страха",
            TechniqueLevel.Chunin,  TechniqueType.Mental, 300,
            "Ментальная атака, вызывает панику. +7 Воля, +4 Соц. интеллект.",
            new() { [21]=7, [14]=4 }),

        new("tech_chunin_water",     "Водяной хлыст",
            TechniqueLevel.Chunin,  TechniqueType.Energy, 300,
            "Техника воды — дальняя атака. +7 Запас энергии, +4 Тонкость.",
            new() { [23]=7, [28]=4 }),

        new("tech_chunin_reflex",    "Мгновенная реакция",
            TechniqueLevel.Chunin,  TechniqueType.Physical, 300,
            "Тренировка рефлексов. +7 Рефлексы, +4 Ловкость.",
            new() { [7]=7, [2]=4 }),

        // ── Elite Chunin (altar ≥ 4) ──────────────────────────────────────
        new("tech_elite_chunin_stone", "Каменная кожа",
            TechniqueLevel.EliteChunin, TechniqueType.Physical, 500,
            "Физическое укрепление тела. +10 Стойкость, +6 Выносливость.",
            new() { [9]=10, [3]=6 }),

        new("tech_elite_chunin_lightning", "Молниеносный удар",
            TechniqueLevel.EliteChunin, TechniqueType.Physical, 500,
            "Сверхбыстрая атака. +10 Ловкость, +6 Рефлексы.",
            new() { [2]=10, [7]=6 }),

        new("tech_elite_chunin_mind", "Аналитический разум",
            TechniqueLevel.EliteChunin, TechniqueType.Mental, 500,
            "Усиление аналитических способностей. +10 Логика, +6 Дедукция.",
            new() { [12]=10, [19]=6 }),

        // ── Jonin (altar ≥ 5) ─────────────────────────────────────────────
        new("tech_jonin_blast",  "Взрыв чакры",
            TechniqueLevel.Jonin, TechniqueType.Energy, 800,
            "Мощный выброс чакры. +15 Запас энергии, +8 Макс. выход.",
            new() { [23]=15, [27]=8 }),

        new("tech_jonin_mind",   "Контроль разума",
            TechniqueLevel.Jonin, TechniqueType.Mental, 800,
            "Высшая ментальная техника. +15 Воля, +8 Когнитивная гибкость.",
            new() { [21]=15, [15]=8 }),

        new("tech_jonin_body",   "Физический пик",
            TechniqueLevel.Jonin, TechniqueType.Physical, 800,
            "Тело достигает высокой формы. +15 Сила, +8 Восстановление сил.",
            new() { [1]=15, [8]=8 }),

        // ── Elite Jonin (altar ≥ 6) ───────────────────────────────────────
        new("tech_elite_jonin_perfect", "Совершенная форма",
            TechniqueLevel.EliteJonin, TechniqueType.Physical, 1000,
            "Тело достигает пика. +18 Сила, +10 Выносливость, +8 Стойкость.",
            new() { [1]=18, [3]=10, [9]=8 }),

        new("tech_elite_jonin_chakra", "Чистый поток чакры",
            TechniqueLevel.EliteJonin, TechniqueType.Energy, 1000,
            "Идеальный контроль энергии. +18 Контроль, +10 Концентрация.",
            new() { [26]=18, [25]=10 }),

        new("tech_elite_jonin_vision", "Третий глаз",
            TechniqueLevel.EliteJonin, TechniqueType.Mental, 1000,
            "Расширенное восприятие. +18 Интуиция, +10 Сенсорика.",
            new() { [17]=18, [4]=10 }),

        // ── Anbu (altar ≥ 7) ──────────────────────────────────────────────
        new("tech_anbu_clone",   "Клон чакры",
            TechniqueLevel.Anbu,  TechniqueType.Energy, 1200,
            "Создание энергетических копий. +20 Запас энергии, +12 Макс. выход, +10 Контроль.",
            new() { [23]=20, [27]=12, [26]=10 }),

        new("tech_anbu_shadow",  "Теневая форма",
            TechniqueLevel.Anbu,  TechniqueType.Physical, 1200,
            "Ниндзя-техника скрытности. +20 Ловкость, +12 Рефлексы, +10 Адаптация.",
            new() { [2]=20, [7]=12, [6]=10 }),

        new("tech_anbu_seal",    "Печать памяти",
            TechniqueLevel.Anbu,  TechniqueType.Mental, 1200,
            "Усиление памяти и концентрации. +20 Память, +12 Фокус.",
            new() { [13]=20, [16]=12 }),

        // ── Sannin (altar ≥ 8) ────────────────────────────────────────────
        new("tech_sannin_nature", "Природная сила",
            TechniqueLevel.Sannin, TechniqueType.Energy, 1500,
            "Слияние с природной энергией. +25 Запас энергии, +15 Восст. энергии, +12 Энерг. восприятие.",
            new() { [23]=25, [24]=15, [29]=12 }),

        new("tech_sannin_body",  "Абсолютная плоть",
            TechniqueLevel.Sannin, TechniqueType.Physical, 1500,
            "Предельное физическое развитие. +25 Выносливость, +15 Регенерация, +12 Долголетие.",
            new() { [3]=25, [5]=15, [10]=12 }),

        new("tech_sannin_mind",  "Высший разум",
            TechniqueLevel.Sannin, TechniqueType.Mental, 1500,
            "Пробуждение умственных способностей. +25 Логика, +15 Математика, +12 Скорость обучения.",
            new() { [12]=25, [20]=15, [22]=12 }),

        // ── Jinchuriki (altar ≥ 9) ────────────────────────────────────────
        new("tech_jinchuriki_beast", "Сила зверя",
            TechniqueLevel.Jinchuriki, TechniqueType.Energy, 2000,
            "Управление огромной внутренней силой. +30 Запас энергии, +20 Макс. выход, +15 Энерг. стойкость.",
            new() { [23]=30, [27]=20, [30]=15 }),

        new("tech_jinchuriki_armor", "Броня демона",
            TechniqueLevel.Jinchuriki, TechniqueType.Physical, 2000,
            "Абсолютная физическая защита. +30 Стойкость, +20 Выносливость, +15 Регенерация.",
            new() { [9]=30, [3]=20, [5]=15 }),

        // ── Kage (altar ≥ 10) ─────────────────────────────────────────────
        new("tech_kage_void",    "Бездна",
            TechniqueLevel.Kage,   TechniqueType.Mental, 3000,
            "Абсолютная ментальная пустота. +40 Воля, +25 Фокус, +20 Дедукция.",
            new() { [21]=40, [16]=25, [19]=20 }),

        new("tech_kage_creation", "Создание реальности",
            TechniqueLevel.Kage,   TechniqueType.Energy, 3000,
            "Управление сутью энергии. +40 Контроль, +25 Тонкость, +20 Концентрация.",
            new() { [26]=40, [28]=25, [25]=20 }),

        new("tech_kage_transcend","Трансцендентность",
            TechniqueLevel.Kage,   TechniqueType.Physical, 3000,
            "Тело превосходит человеческие пределы. +40 Сила, +25 Ловкость, +20 Рефлексы.",
            new() { [1]=40, [2]=25, [7]=20 }),
    };

    public static readonly PassiveUpgrade[] Passives =
    {
        // ── Genin (+5 = 150 OV, altar ≥ 1) ──────────────────────────────
        new("pass_genin_str",  "Усиление: Сила",          TechniqueLevel.Genin, 1,  5, 150, "+5 Сила"),
        new("pass_genin_agi",  "Усиление: Ловкость",      TechniqueLevel.Genin, 2,  5, 150, "+5 Ловкость"),
        new("pass_genin_end",  "Усиление: Выносливость",  TechniqueLevel.Genin, 3,  5, 150, "+5 Выносливость"),
        new("pass_genin_ref",  "Усиление: Рефлексы",      TechniqueLevel.Genin, 7,  5, 150, "+5 Рефлексы"),
        new("pass_genin_foc",  "Усиление: Фокус",         TechniqueLevel.Genin, 16, 5, 150, "+5 Фокус"),
        new("pass_genin_will", "Усиление: Воля",           TechniqueLevel.Genin, 21, 5, 150, "+5 Воля"),
        new("pass_genin_ce",   "Усиление: Запас энергии", TechniqueLevel.Genin, 23, 5, 150, "+5 Запас энергии"),
        new("pass_genin_ctrl", "Усиление: Контроль",      TechniqueLevel.Genin, 26, 5, 150, "+5 Контроль"),

        // ── Elite Genin (+10 = 200 OV, altar ≥ 2) ────────────────────────
        new("pass_egenin_str", "Усиление II: Сила",          TechniqueLevel.EliteGenin, 1,  10, 200, "+10 Сила"),
        new("pass_egenin_agi", "Усиление II: Ловкость",      TechniqueLevel.EliteGenin, 2,  10, 200, "+10 Ловкость"),
        new("pass_egenin_end", "Усиление II: Выносливость",  TechniqueLevel.EliteGenin, 3,  10, 200, "+10 Выносливость"),
        new("pass_egenin_ref", "Усиление II: Рефлексы",      TechniqueLevel.EliteGenin, 7,  10, 200, "+10 Рефлексы"),
        new("pass_egenin_foc", "Усиление II: Фокус",         TechniqueLevel.EliteGenin, 16, 10, 200, "+10 Фокус"),
        new("pass_egenin_will","Усиление II: Воля",           TechniqueLevel.EliteGenin, 21, 10, 200, "+10 Воля"),
        new("pass_egenin_ce",  "Усиление II: Запас энергии", TechniqueLevel.EliteGenin, 23, 10, 200, "+10 Запас энергии"),
        new("pass_egenin_ctrl","Усиление II: Контроль",      TechniqueLevel.EliteGenin, 26, 10, 200, "+10 Контроль"),

        // ── Chunin (+15 = 300 OV, altar ≥ 3) ─────────────────────────────
        new("pass_chunin_str", "Усиление III: Сила",          TechniqueLevel.Chunin, 1,  15, 300, "+15 Сила"),
        new("pass_chunin_agi", "Усиление III: Ловкость",      TechniqueLevel.Chunin, 2,  15, 300, "+15 Ловкость"),
        new("pass_chunin_end", "Усиление III: Выносливость",  TechniqueLevel.Chunin, 3,  15, 300, "+15 Выносливость"),
        new("pass_chunin_ref", "Усиление III: Рефлексы",      TechniqueLevel.Chunin, 7,  15, 300, "+15 Рефлексы"),
        new("pass_chunin_foc", "Усиление III: Фокус",         TechniqueLevel.Chunin, 16, 15, 300, "+15 Фокус"),
        new("pass_chunin_will","Усиление III: Воля",           TechniqueLevel.Chunin, 21, 15, 300, "+15 Воля"),
        new("pass_chunin_ce",  "Усиление III: Запас энергии", TechniqueLevel.Chunin, 23, 15, 300, "+15 Запас энергии"),
        new("pass_chunin_ctrl","Усиление III: Контроль",      TechniqueLevel.Chunin, 26, 15, 300, "+15 Контроль"),

        // ── Elite Chunin (+20 = 500 OV, altar ≥ 4) ───────────────────────
        new("pass_echunin_str","Усиление IV: Сила",            TechniqueLevel.EliteChunin, 1,  20, 500, "+20 Сила"),
        new("pass_echunin_end","Усиление IV: Выносливость",    TechniqueLevel.EliteChunin, 3,  20, 500, "+20 Выносливость"),
        new("pass_echunin_foc","Усиление IV: Фокус",           TechniqueLevel.EliteChunin, 16, 20, 500, "+20 Фокус"),
        new("pass_echunin_ce", "Усиление IV: Запас энергии",   TechniqueLevel.EliteChunin, 23, 20, 500, "+20 Запас энергии"),

        // ── Jonin (+25 = 800 OV, altar ≥ 5) ──────────────────────────────
        new("pass_jonin_str",  "Усиление V: Сила",             TechniqueLevel.Jonin, 1,  25, 800, "+25 Сила"),
        new("pass_jonin_end",  "Усиление V: Выносливость",     TechniqueLevel.Jonin, 3,  25, 800, "+25 Выносливость"),
        new("pass_jonin_will", "Усиление V: Воля",              TechniqueLevel.Jonin, 21, 25, 800, "+25 Воля"),
        new("pass_jonin_ce",   "Усиление V: Запас энергии",    TechniqueLevel.Jonin, 23, 25, 800, "+25 Запас энергии"),
    };

    public static int MinAltarLevel(TechniqueLevel rank) => (int)rank + 1;
}
