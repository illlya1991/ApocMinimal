// Models/LocationData/Location.cs

using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ApocMinimal.Models.LocationData;

public enum LocationType
{
    Apartment,
    Floor,
    Building,
    Street,
    District,
    City,
    Commercial
}

public enum LocationStatus
{
    Dangerous,
    Cleared,
}

public enum MapState
{
    Template,
    ApocStart,
    Current,
}

public enum CommercialType
{
    None,
    Shop,
    Supermarket,
    Mall,
    Market,
    Hairdresser,
    BeautySalon,
    Pharmacy,
    Hospital,
    Factory,
    Hotel
}

public class Location
{
    private Dictionary<string, double> _resourceNodes = new();
    private bool _isExplored;
    private LocationStatus _status = LocationStatus.Dangerous;
    private double _dangerLevel;

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public LocationType Type { get; set; }
    public int ParentId { get; set; }

    public Dictionary<string, double> ResourceNodes
    {
        get => _resourceNodes;
        set
        {
            _resourceNodes = value;
            IsDirty = true;
        }
    }

    public double DangerLevel
    {
        get => _dangerLevel;
        set
        {
            if (Math.Abs(_dangerLevel - value) > 0.01)
                IsDirty = true;
            _dangerLevel = value;
        }
    }

    public bool IsExplored
    {
        get => _isExplored;
        set
        {
            if (_isExplored != value)
                IsDirty = true;
            _isExplored = value;
        }
    }

    public LocationStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
                IsDirty = true;
            _status = value;
        }
    }

    public string MonsterTypeName { get; set; } = "";
    public MapState MapState { get; set; } = MapState.Current;
    public CommercialType CommercialType { get; set; } = CommercialType.None;
    public int SubCount { get; set; } = 0;

    [JsonIgnore]
    public bool IsDirty { get; set; } = false;

    public string TypeLabel => Type switch
    {
        LocationType.Apartment => "Квартира",
        LocationType.Floor => "Этаж",
        LocationType.Building => "Здание",
        LocationType.Street => "Улица",
        LocationType.District => "Район",
        LocationType.City => "Город",
        LocationType.Commercial => CommercialType switch
        {
            CommercialType.Shop => "Магазин",
            CommercialType.Supermarket => "Супермаркет",
            CommercialType.Mall => "ТЦ",
            CommercialType.Market => "Рынок",
            CommercialType.Hairdresser => "Парикмахерская",
            CommercialType.BeautySalon => "Салон красоты",
            CommercialType.Pharmacy => "Аптека",
            CommercialType.Hospital => "Больница",
            CommercialType.Factory => "Завод",
            CommercialType.Hotel => "Гостиница",
            _ => "Коммерческое"
        },
        _ => Type.ToString(),
    };

    // Вспомогательные методы для работы с ResourceNodes
    public void AddResource(string resourceName, double amount)
    {
        if (_resourceNodes.ContainsKey(resourceName))
            _resourceNodes[resourceName] += amount;
        else
            _resourceNodes[resourceName] = amount;
        IsDirty = true;
    }

    public bool RemoveResource(string resourceName, double amount)
    {
        if (!_resourceNodes.TryGetValue(resourceName, out double current))
            return false;

        if (current <= amount)
        {
            _resourceNodes.Remove(resourceName);
        }
        else
        {
            _resourceNodes[resourceName] = Math.Round((current - amount) * 10) / 10.0;
        }
        IsDirty = true;
        return true;
    }

    public void ClearDirty()
    {
        IsDirty = false;
    }
}