namespace ApocMinimal.Models.PersonData.NpcData;

public enum NeedCategory { Basic, Special }

public class Need
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public NeedCategory Category { get; set; }
    public int Level { get; set; } = 3;     // 1–5: интенсивность потребности
    public double Value { get; set; }        // 0–100: текущая неудовлетворённость (выше = острее)
    public double Satisfaction { get; set; } = 100;

    public bool IsCritical  => Value >= 80;
    public bool IsUrgent    => Value >= 60;
    public bool IsSatisfied => Value <= 20;

    public void Decay(double amount)
    {
        Value        = Math.Clamp(Value + amount * Level, 0, 100);
        Satisfaction = 100 - Value;
    }

    public void Satisfy(double amount)
    {
        Value        = Math.Clamp(Value - amount, 0, 100);
        Satisfaction = 100 - Value;
    }
}

// ── Базовые 10 потребностей ───────────────────────────────────────────────────

/// <summary>
/// Stable IDs 1–10 for basic needs. Match 1-based index in BasicNeeds.Names.
/// </summary>
public enum BasicNeedId
{
    Food            = 1,  // Еда              — ресурс (FoodRestore)
    Water           = 2,  // Вода             — ресурс (WaterRestore)
    Sleep           = 3,  // Сон              — действие Sleep
    Heat            = 4,  // Тепло            — жильё + одежда
    Hygiene         = 5,  // Гигиена          — место + вода + действие + предмет
    Safety          = 6,  // Безопасность     — авто: барьер + жильё + часовые
    RestHealth      = 7,  // Отдых и здоровье — действия + предметы
    Social          = 8,  // Общение          — другие НПС + действия
    SexFamily       = 9,  // Секс / Семья     — другие НПС + действия
    SelfImprovement = 10, // Самосовершенств. — предметы + действия
}

public static class BasicNeeds
{
    public static readonly string[] Names =
    {
        "Еда", "Вода", "Сон", "Тепло", "Гигиена",
        "Безопасность", "Отдых и здоровье", "Общение", "Секс/Семья", "Самосовершенствование"
    };

    public static string NameOf(BasicNeedId id) => Names[(int)id - 1];
}

// ── Специальные 10 потребностей (качественное усиление базовых) ──────────────

/// <summary>
/// IDs 11–20 for special (quality-upgrade) needs, one per basic need.
/// Not every NPC has all of them.
/// </summary>
public enum SpecialNeedId
{
    Gourmet         = 11, // Гурман         ← Еда        (горячая многокомп. еда)
    Sommelier       = 12, // Сомелье        ← Вода       (алкоголь / напитки)
    LightSleeper    = 13, // Чуткий сон     ← Сон        (кровать, тихая комната)
    Softie          = 14, // Неженка        ← Тепло      (комфортная t°, обогрев)
    Aesthete        = 15, // Эстет          ← Гигиена    (косметика, парфюм)
    Paranoid        = 16, // Параноик       ← Безопасность (личная охрана)
    Hedonist        = 17, // Гедонист       ← Отдых/Здор. (спа, массаж, премиум)
    SocialLion      = 18, // Светский лев   ← Общение    (статусные НПС, события)
    Romantic        = 19, // Романтик       ← Секс/Семья (романтика, подарки)
    Perfectionist   = 20, // Перфекционист  ← Самосов.   (лучшие инструменты)
}

public static class SpecialNeedsData
{
    public static readonly (int Id, string Name, int LinkedBasicId)[] All =
    {
        ((int)SpecialNeedId.Gourmet,       "Гурман",        (int)BasicNeedId.Food),
        ((int)SpecialNeedId.Sommelier,     "Сомелье",       (int)BasicNeedId.Water),
        ((int)SpecialNeedId.LightSleeper,  "Чуткий сон",    (int)BasicNeedId.Sleep),
        ((int)SpecialNeedId.Softie,        "Неженка",       (int)BasicNeedId.Heat),
        ((int)SpecialNeedId.Aesthete,      "Эстет",         (int)BasicNeedId.Hygiene),
        ((int)SpecialNeedId.Paranoid,      "Параноик",      (int)BasicNeedId.Safety),
        ((int)SpecialNeedId.Hedonist,      "Гедонист",      (int)BasicNeedId.RestHealth),
        ((int)SpecialNeedId.SocialLion,    "Светский лев",  (int)BasicNeedId.Social),
        ((int)SpecialNeedId.Romantic,      "Романтик",      (int)BasicNeedId.SexFamily),
        ((int)SpecialNeedId.Perfectionist, "Перфекционист", (int)BasicNeedId.SelfImprovement),
    };
}

// Совместимость: старый класс SpecialNeeds используется в ряде мест
public static class SpecialNeeds
{
    public static readonly string[] All = SpecialNeedsData.All.Select(x => x.Name).ToArray();
}
