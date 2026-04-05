namespace ApocMinimal.Models;

public enum LocationType
{
    Apartment,  // квартира
    Floor,      // этаж
    Building,   // здание
    Street,     // улица
    District,   // район
    City,       // город
}

public class Location
{
    public int          Id       { get; set; }
    public string       Name     { get; set; } = "";
    public LocationType Type     { get; set; }
    public int          ParentId { get; set; }  // 0 = top level (City)

    /// <summary>Resource nodes available in this location (ResourceName → maxAmount).</summary>
    public Dictionary<string, double> ResourceNodes { get; set; } = new();

    /// <summary>Danger level 0–100: affects combat chance and action costs.</summary>
    public double DangerLevel { get; set; }

    /// <summary>Is this location explored/visible to the player?</summary>
    public bool IsExplored { get; set; }

    public string TypeLabel => Type switch
    {
        LocationType.Apartment => "Квартира",
        LocationType.Floor     => "Этаж",
        LocationType.Building  => "Здание",
        LocationType.Street    => "Улица",
        LocationType.District  => "Район",
        LocationType.City      => "Город",
        _                      => Type.ToString(),
    };
}

/// <summary>100 resource types that can appear in location nodes.</summary>
public static class ResourceTypes
{
    public static readonly string[] All =
    {
        // Еда и вода (1–15)
        "Консервы", "Крупа", "Мука", "Сахар", "Соль",
        "Питьевая вода", "Фильтры для воды", "Алкоголь", "Рыба", "Мясо",
        "Овощи", "Фрукты", "Хлеб", "Сухари", "Специи",
        // Медицина (16–25)
        "Бинты", "Антисептик", "Антибиотики", "Обезболивающее", "Витамины",
        "Шприцы", "Хирургические инструменты", "Костыли", "Маски", "Перчатки",
        // Строительство (26–40)
        "Доски", "Брёвна", "Кирпич", "Цемент", "Гвозди",
        "Проволока", "Металлолом", "Стекло", "Утеплитель", "Рубероид",
        "Краска", "Верёвка", "Труба", "Арматура", "Пластик",
        // Инструменты и техника (41–55)
        "Молоток", "Пила", "Дрель", "Отвёртка", "Плоскогубцы",
        "Сварочный аппарат", "Генератор", "Аккумулятор", "Провода", "Фонарик",
        "Рация", "Ноутбук", "Телефон", "Солнечная панель", "Двигатель",
        // Топливо и энергия (56–65)
        "Бензин", "Дизель", "Уголь", "Дрова", "Газовый баллон",
        "Свечи", "Спички", "Зажигалки", "Масло лампадное", "Торф",
        // Оружие и защита (66–75)
        "Нож", "Топор", "Арбалет", "Дробовик", "Пистолет",
        "Патроны", "Арбалетные болты", "Кастет", "Щит", "Броня",
        // Одежда и быт (76–85)
        "Тёплая одежда", "Дождевик", "Обувь", "Одеяло", "Спальный мешок",
        "Посуда", "Мыло", "Зубная паста", "Туалетная бумага", "Стиральный порошок",
        // Редкие и особые (86–100)
        "Семена", "Книги", "Карты местности", "Аптечка", "Противогаз",
        "Ночной прицел", "Взрывчатка", "Дымовые шашки", "Наркотики", "Золото",
        "Антиквариат", "Ключи от машины", "Документы", "Рация дальнего действия", "ЭМИ-устройство",
    };
}
