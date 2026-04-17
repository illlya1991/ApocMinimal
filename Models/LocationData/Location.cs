namespace ApocMinimal.Models.LocationData;

public enum LocationType
{
    Apartment,  // квартира
    Floor,      // этаж
    Building,   // здание
    Street,     // улица
    District,   // район
    City,       // город
}

public enum LocationStatus
{
    Dangerous, // есть монстры
    Cleared,   // зачищено
}

public enum MapState
{
    Template,  // шаблон мира (не активен)
    ApocStart, // состояние на начало апокалипсиса
    Current,   // текущее состояние мира
}

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public LocationType Type { get; set; }
    public int ParentId { get; set; }  // 0 = top level (City)

    /// <summary>Resource nodes available in this location (ResourceName → maxAmount).</summary>
    public Dictionary<string, double> ResourceNodes { get; set; } = new();

    /// <summary>Danger level 0–100: affects combat chance and action costs.</summary>
    public double DangerLevel { get; set; }

    /// <summary>Is this location explored/visible to the player?</summary>
    public bool IsExplored { get; set; }

    /// <summary>Whether the location has been cleared of monsters.</summary>
    public LocationStatus Status { get; set; } = LocationStatus.Dangerous;
    /// <summary>Type of monsters present (empty when Cleared).</summary>
    public string MonsterTypeName { get; set; } = "";

    /// <summary>Which map snapshot this location belongs to.</summary>
    public MapState MapState { get; set; } = MapState.Current;

    public string TypeLabel => Type switch
    {
        LocationType.Apartment => "Квартира",
        LocationType.Floor => "Этаж",
        LocationType.Building => "Здание",
        LocationType.Street => "Улица",
        LocationType.District => "Район",
        LocationType.City => "Город",
        _ => Type.ToString(),
    };
}
