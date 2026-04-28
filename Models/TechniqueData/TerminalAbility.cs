namespace ApocMinimal.Models.TechniqueData;

public enum TerminalAbilityType
{
    Base,    // 🔵 Базовая — общая механика
    Earth,   // 🟡 Земляная — выживание и ресурсы
    Unique,  // 🔴 Уникальная — особая сила Терминала
}

/// <summary>
/// Пассивная способность Терминала, разблокируемая при достижении уровня.
/// 15 способностей: 3 на каждый уровень 2/4/6/8/10.
/// </summary>
public class TerminalAbility
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int UnlockLevel { get; set; }      // 2, 4, 6, 8 или 10
    public TerminalAbilityType AbilityType { get; set; }
    public double OPCostPerUse { get; set; }  // 0 = пассивная

    public string TypeIcon => AbilityType switch
    {
        TerminalAbilityType.Base   => "🔵",
        TerminalAbilityType.Earth  => "🟡",
        TerminalAbilityType.Unique => "🔴",
        _ => "●"
    };

    public string TypeLabel => AbilityType switch
    {
        TerminalAbilityType.Base   => "Базовая",
        TerminalAbilityType.Earth  => "Земляная",
        TerminalAbilityType.Unique => "Уникальная",
        _ => ""
    };
}

/// <summary>
/// Каталог из 15 способностей Терминала (3 × 5 уровней).
/// </summary>
public static class TerminalAbilityCatalog
{
    public static readonly TerminalAbility[] All =
    {
        // ── Уровень 2 ──────────────────────────────────────────────────────
        new()
        {
            Id = "ta_l2_base", Name = "Эхо Ченджери",
            Description = "Терминал пассивно усиливает преданность всех выживших: +2 Преданности каждый день.",
            UnlockLevel = 2, AbilityType = TerminalAbilityType.Base, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l2_earth", Name = "Корни Земли",
            Description = "Выжившие при добыче ресурсов получают +20% к найденному количеству.",
            UnlockLevel = 2, AbilityType = TerminalAbilityType.Earth, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l2_unique", Name = "Метка Терминала",
            Description = "Раз в 3 дня можно отметить одного НПС — он получает +5 к инициативе и +5 к здоровью на 7 дней.",
            UnlockLevel = 2, AbilityType = TerminalAbilityType.Unique, OPCostPerUse = 5,
        },

        // ── Уровень 4 ──────────────────────────────────────────────────────
        new()
        {
            Id = "ta_l4_base", Name = "Щит Сохранения",
            Description = "Каждый день Терминал поглощает часть урона, наносимого выжившим: -1 урон каждому НПС от нужд.",
            UnlockLevel = 4, AbilityType = TerminalAbilityType.Base, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l4_earth", Name = "Зов Пустоши",
            Description = "Разведанные локации дают +10% к ресурсам. Новые локации открываются быстрее.",
            UnlockLevel = 4, AbilityType = TerminalAbilityType.Earth, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l4_unique", Name = "Пульс Ченджери",
            Description = "За 15 ОР можно провести пульс: все НПС получают +10 здоровья и -10 страха.",
            UnlockLevel = 4, AbilityType = TerminalAbilityType.Unique, OPCostPerUse = 15,
        },

        // ── Уровень 6 ──────────────────────────────────────────────────────
        new()
        {
            Id = "ta_l6_base", Name = "Поле Доверия",
            Description = "Разговоры с НПС дают на 50% больше доверия. Страх снижается на 1 в день автоматически.",
            UnlockLevel = 6, AbilityType = TerminalAbilityType.Base, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l6_earth", Name = "Земляной Барьер",
            Description = "Защищённые локации получают -30% к урону от монстров. Барьер стоит на 20% дешевле.",
            UnlockLevel = 6, AbilityType = TerminalAbilityType.Earth, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l6_unique", Name = "Волна Перерождения",
            Description = "За 30 ОР: воскресить одного мёртвого НПС с 30% здоровья и нулевым страхом.",
            UnlockLevel = 6, AbilityType = TerminalAbilityType.Unique, OPCostPerUse = 30,
        },

        // ── Уровень 8 ──────────────────────────────────────────────────────
        new()
        {
            Id = "ta_l8_base", Name = "Резонанс Преданности",
            Description = "Пожертвования НПС дают +30% ОР. Преданность тратится на 20% медленнее.",
            UnlockLevel = 8, AbilityType = TerminalAbilityType.Base, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l8_earth", Name = "Ченджери-Питание",
            Description = "Еда и вода расходуются на 25% медленнее благодаря энергии Терминала.",
            UnlockLevel = 8, AbilityType = TerminalAbilityType.Earth, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l8_unique", Name = "Разрыв Пространства",
            Description = "За 50 ОР: мгновенно перенести любого НПС в любую разведанную локацию без затрат времени.",
            UnlockLevel = 8, AbilityType = TerminalAbilityType.Unique, OPCostPerUse = 50,
        },

        // ── Уровень 10 ─────────────────────────────────────────────────────
        new()
        {
            Id = "ta_l10_base", Name = "Абсолютный Ченджери",
            Description = "Все пассивные эффекты Терминала удваиваются. ОР от НПС +50%.",
            UnlockLevel = 10, AbilityType = TerminalAbilityType.Base, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l10_earth", Name = "Господство над Пустошью",
            Description = "Все монстры в защищённых зонах получают -50% здоровья. Ресурсы в зонах удвоены.",
            UnlockLevel = 10, AbilityType = TerminalAbilityType.Earth, OPCostPerUse = 0,
        },
        new()
        {
            Id = "ta_l10_unique", Name = "Истинный Терминал",
            Description = "Финальная способность. Открывает путь к созданию Истинного ЦС. Стоимость: 200 ОР.",
            UnlockLevel = 10, AbilityType = TerminalAbilityType.Unique, OPCostPerUse = 200,
        },
    };

    public static TerminalAbility? Find(string id)
        => All.FirstOrDefault(a => a.Id == id);

    public static IEnumerable<TerminalAbility> GetUnlocked(int terminalLevel)
        => All.Where(a => a.UnlockLevel <= terminalLevel);

    public static IEnumerable<TerminalAbility> GetForLevel(int terminalLevel)
        => All.Where(a => a.UnlockLevel == terminalLevel);
}
