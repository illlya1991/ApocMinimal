namespace ApocMinimal.Models;

public enum NeedCategory { Basic, Special }

public class Need
{
    public int          Id           { get; set; }
    public string       Name         { get; set; } = "";
    public NeedCategory Category     { get; set; }
    public int          Level        { get; set; } = 1;   // 1–5: сила потребности
    public double       Value        { get; set; }        // 0–100: текущий уровень (выше = острее)
    public double       Satisfaction { get; set; } = 100; // 0–100

    public bool IsCritical    => Value >= 80;
    public bool IsUrgent      => Value >= 60;
    public bool IsSatisfied   => Value <= 20;

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

public static class BasicNeeds
{
    public static readonly string[] Names =
    {
        "Еда", "Вода", "Сон", "Тепло", "Гигиена",
        "Туалет", "Безопасность", "Отдых", "Здоровье", "Социальность"
    };
}

public static class SpecialNeeds
{
    public static readonly string[] All =
    {
        "Алкоголь", "Комфорт", "Развлечения", "Секс", "Парикмахер",
        "Техника", "Книги", "Медитация", "Спорт", "Музыка",
        "Рисование", "Кулинария", "Садоводство", "Рыбалка", "Охота",
        "Коллекционирование", "Путешествия", "Адреналин", "Признание", "Одиночество",
        "Азартные игры", "Шопинг", "Ритуалы", "Наставничество", "Исследования",
        "Танцы", "Пение", "Ремесло", "Химия", "Астрология"
    };
}
