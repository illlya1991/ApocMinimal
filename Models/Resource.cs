namespace ApocMinimal.Models;

/// <summary>Canonical names for seeded resources — eliminates magic string literals.</summary>
public static class ResourceNames
{
    public const string Food      = "Еда";
    public const string Water     = "Вода";
    public const string Medicine  = "Медикаменты";
    public const string Wood      = "Дерево";
    public const string Tools     = "Инструменты";
}

public class Resource
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = "";
    public double Amount   { get; set; }
    public string Category { get; set; } = "";
}
