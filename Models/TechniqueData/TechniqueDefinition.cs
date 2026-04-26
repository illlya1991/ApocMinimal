namespace ApocMinimal.Models.TechniqueData;

public enum TechKind { Passive, Active }

public class TechniqueDefinition
{
    public string Id       { get; init; } = "";
    public string Name     { get; init; } = "";
    public string Description { get; init; } = "";
    public TechKind Kind   { get; init; }
    public int  AltarLevel { get; init; } = 1;
    public double BuyCost  { get; init; }
    // Passive — stat bonuses applied as permanent modifiers (statId → bonus)
    public Dictionary<int, double> StatBonus { get; init; } = new();
    // Active — two-mode description
    public string CombatEffect { get; init; } = "";
    public string LifeEffect   { get; init; } = "";
}

public class AbilityDefinition
{
    public string Id       { get; init; } = "";
    public string Name     { get; init; } = "";
    public string Description { get; init; } = "";
    public int  AltarLevel { get; init; } = 1;
    public double BuyCost  { get; init; }
    public List<string> TechniqueIds { get; init; } = new();
}

public static class TechAbilityCatalog
{
    // ── Passive Techniques ──────────────────────────────────────────────────
    // Stat IDs: 1=Сила, 2=Ловкость, 3=Выносливость, 5=Регенерация,
    //           7=Рефлексы, 9=Стойкость, 12=Логика, 16=Фокус,
    //           17=Интуиция, 19=Дедукция, 21=Воля,
    //           23=Запас энергии, 25=Концентрация, 26=Контроль, 28=Тонкость
    //
    // ── Active  Techniques ──────────────────────────────────────────────────
    // Stored in NPC — CombatEffect / LifeEffect describe usage

    public static readonly TechniqueDefinition[] Techniques =
    {
        // altar 1 — passives
        new() { Id="tdef_str1",   Name="Укрепление тела",
            Description="Постоянно усиливает физическую силу.",
            Kind=TechKind.Passive, AltarLevel=1, BuyCost=60,
            StatBonus=new(){ [1]=5 } },

        new() { Id="tdef_end1",   Name="Закалка тела",
            Description="Повышает выносливость и сопротивляемость.",
            Kind=TechKind.Passive, AltarLevel=1, BuyCost=60,
            StatBonus=new(){ [3]=5 } },

        new() { Id="tdef_agi1",   Name="Гибкость",
            Description="Развивает ловкость и рефлексы.",
            Kind=TechKind.Passive, AltarLevel=1, BuyCost=60,
            StatBonus=new(){ [2]=4, [7]=3 } },

        // altar 1 — active
        new() { Id="tdef_strike", Name="Удар силы",
            Description="Базовая ударная техника. Два режима применения.",
            Kind=TechKind.Active, AltarLevel=1, BuyCost=80,
            CombatEffect="Наносит усиленный удар — +20% урон в текущем ходу.",
            LifeEffect="Ломает преграды, ускоряет физическую работу." },

        // altar 2 — passives
        new() { Id="tdef_focus2", Name="Острый фокус",
            Description="Развивает концентрацию и силу воли.",
            Kind=TechKind.Passive, AltarLevel=2, BuyCost=100,
            StatBonus=new(){ [16]=8, [21]=4 } },

        new() { Id="tdef_ce2",    Name="Накопление энергии",
            Description="Расширяет запас энергии и улучшает контроль.",
            Kind=TechKind.Passive, AltarLevel=2, BuyCost=100,
            StatBonus=new(){ [23]=8, [26]=4 } },

        new() { Id="tdef_int2",   Name="Логическое мышление",
            Description="Усиливает интеллект и дедуктивные способности.",
            Kind=TechKind.Passive, AltarLevel=2, BuyCost=100,
            StatBonus=new(){ [12]=8, [19]=4 } },

        // altar 2 — active
        new() { Id="tdef_heal",   Name="Исцеление энергией",
            Description="Целительная техника. Применима в бою и в жизни.",
            Kind=TechKind.Active, AltarLevel=2, BuyCost=140,
            CombatEffect="Восстанавливает 20 HP союзнику в текущем ходу.",
            LifeEffect="Снимает одно ранение или ускоряет восстановление." },

        new() { Id="tdef_sense",  Name="Сенсорика",
            Description="Обострённое восприятие окружения.",
            Kind=TechKind.Active, AltarLevel=2, BuyCost=110,
            CombatEffect="Обнаруживает скрытых врагов до начала хода.",
            LifeEffect="Предчувствует опасность, повышает эффективность разведки." },

        new() { Id="tdef_dodge",  Name="Уклонение",
            Description="Техника быстрого уклонения.",
            Kind=TechKind.Active, AltarLevel=2, BuyCost=120,
            CombatEffect="Избегает следующего входящего удара.",
            LifeEffect="Мгновенно реагирует на внезапную угрозу." },

        // altar 3 — passives
        new() { Id="tdef_regen3", Name="Быстрое восстановление",
            Description="Ускоряет регенерацию и повышает выносливость.",
            Kind=TechKind.Passive, AltarLevel=3, BuyCost=180,
            StatBonus=new(){ [5]=6, [3]=4 } },

        new() { Id="tdef_ctrl3",  Name="Точный контроль",
            Description="Идеальный контроль энергии и тонкость исполнения.",
            Kind=TechKind.Passive, AltarLevel=3, BuyCost=180,
            StatBonus=new(){ [26]=8, [28]=5 } },

        // altar 3 — active
        new() { Id="tdef_shield", Name="Щит энергии",
            Description="Создаёт защитный барьер из энергии.",
            Kind=TechKind.Active, AltarLevel=3, BuyCost=220,
            CombatEffect="Блокирует весь урон в текущем ходу.",
            LifeEffect="Защищает от угроз окружающей среды и стихийных воздействий." },

        // altar 4 — active
        new() { Id="tdef_blast",  Name="Взрыв энергии",
            Description="Мощный выброс накопленной энергии.",
            Kind=TechKind.Active, AltarLevel=4, BuyCost=300,
            CombatEffect="Мощная AoE-атака — наносит урон всем врагам в зоне.",
            LifeEffect="Расчищает крупные завалы и укреплённые преграды." },
    };

    // ── Abilities ───────────────────────────────────────────────────────────

    public static readonly AbilityDefinition[] Abilities =
    {
        new() { Id="abil_warrior",  Name="Путь воина",
            Description="Боевой путь, развивающий физическую мощь и ударную силу.",
            AltarLevel=1, BuyCost=130,
            TechniqueIds=new(){"tdef_str1","tdef_strike"} },

        new() { Id="abil_survivor", Name="Путь выживальщика",
            Description="Путь тех, кто умеет выжить в любых условиях.",
            AltarLevel=1, BuyCost=110,
            TechniqueIds=new(){"tdef_end1","tdef_agi1"} },

        new() { Id="abil_scout",    Name="Путь разведчика",
            Description="Скрытность, реакция, восприятие.",
            AltarLevel=2, BuyCost=200,
            TechniqueIds=new(){"tdef_agi1","tdef_sense","tdef_dodge"} },

        new() { Id="abil_healer",   Name="Путь целителя",
            Description="Исцеление союзников и восстановление сил.",
            AltarLevel=2, BuyCost=230,
            TechniqueIds=new(){"tdef_end1","tdef_heal"} },

        new() { Id="abil_mage",     Name="Путь мага",
            Description="Глубокое понимание энергии и разума.",
            AltarLevel=2, BuyCost=250,
            TechniqueIds=new(){"tdef_ce2","tdef_focus2","tdef_int2"} },

        new() { Id="abil_guardian", Name="Путь стража",
            Description="Несокрушимая защита и точный контроль.",
            AltarLevel=3, BuyCost=380,
            TechniqueIds=new(){"tdef_regen3","tdef_ctrl3","tdef_shield"} },
    };

    public static TechniqueDefinition? FindTech(string id) =>
        Array.Find(Techniques, t => t.Id == id);

    public static AbilityDefinition? FindAbility(string id) =>
        Array.Find(Abilities, a => a.Id == id);
}
