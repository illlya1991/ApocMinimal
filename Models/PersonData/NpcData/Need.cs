namespace ApocMinimal.Models.PersonData.NpcData;

public enum NeedCategory { Basic, Special }

public class Need
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public NeedCategory Category { get; set; }
    public int Level { get; set; } = 1;   // 1–5: сила потребности
    public double Value { get; set; }        // 0–100: текущий уровень (выше = острее)
    public double Satisfaction { get; set; } = 100; // 0–100

    public bool IsCritical => Value >= 80;
    public bool IsUrgent => Value >= 60;
    public bool IsSatisfied => Value <= 20;

    public void Decay(double amount)
    {
        Value = Math.Clamp(Value + amount * Level, 0, 100);
        Satisfaction = 100 - Value;
    }

    public void Satisfy(double amount)
    {
        Value = Math.Clamp(Value - amount, 0, 100);
        Satisfaction = 100 - Value;
    }
}

/// <summary>
/// Stable IDs for the 10 basic needs.
/// Value matches the 1-based index in BasicNeeds.Names and the Need.Id assigned in InitialiseNeeds.
/// </summary>
public enum BasicNeedId
{
    Food = 1,
    Water = 2,
    Sleep = 3,
    Heat = 4,
    Hygiene = 5,
    Toilet = 6,
    Safety = 7,
    Rest = 8,
    Health = 9,
    Social = 10,
}

public static class BasicNeeds
{
    public static readonly string[] Names =
    {
        "Еда", "Вода", "Сон", "Тепло", "Гигиена",
        "Туалет", "Безопасность", "Отдых", "Здоровье", "Социальность"
    };

    /// <summary>Display name for a known basic need ID.</summary>
    public static string NameOf(BasicNeedId id) => Names[(int)id - 1];
}

public static class SpecialNeeds
{
    public static readonly string[] All =
    {
        // 1–30 (оригинальные)
        "Алкоголь", "Комфорт", "Развлечения", "Секс", "Парикмахер",
        "Техника", "Книги", "Медитация", "Спорт", "Музыка",
        "Рисование", "Кулинария", "Садоводство", "Рыбалка", "Охота",
        "Коллекционирование", "Путешествия", "Адреналин", "Признание", "Одиночество",
        "Азартные игры", "Шопинг", "Ритуалы", "Наставничество", "Исследования",
        "Танцы", "Пение", "Ремесло", "Химия", "Астрология",
        // 31–50 (вредные привычки и зависимости)
        "Курение", "Наркотики", "Кофе", "Сладкое", "Острая еда",
        "Азарт", "Драки", "Власть", "Контроль", "Доминирование",
        "Подчинение", "Провокации", "Скандалы", "Месть", "Зависть",
        "Паразитизм", "Накопительство", "Тайны", "Манипуляции", "Ложь",
        // 51–70 (социальные и психологические)
        "Дружба", "Любовь", "Семья", "Забота о других", "Защита слабых",
        "Справедливость", "Честность", "Уважение", "Слава", "Лидерство",
        "Самопожертвование", "Исповедь", "Прощение", "Ностальгия", "Традиции",
        "Порядок", "Чистота", "Эстетика", "Письмо-дневник", "Самоанализ",
        // 71–85 (духовные и интеллектуальные)
        "Молитва", "Созерцание", "Природа", "Тишина", "Философия",
        "Изобретательство", "Торговля", "Богатство", "Знания", "Свобода",
        "Независимость", "Справедливая борьба", "Победа", "Соревнование", "Риск",
        // 86–100 (физические и прочие)
        "Физический труд", "Рукоделие", "Плавание", "Бег", "Боевые искусства",
        "Сон на природе", "Животные", "Дети", "Строительство", "Разрушение",
        "Поджоги", "Коллекции оружия", "Tatуировки", "Экстаз", "Безумие",
    };
}
