namespace ApocMinimal.Models.ResourceData;

public class ResourceCatalogEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Rarity { get; set; } = "Common";
    public string Unit { get; set; } = "шт";
    public double Weight { get; set; } = 0.5;
    public int SpoilageDays { get; set; } = 0;
    public double FoodRestore { get; set; } = 0;
    public double WaterRestore { get; set; } = 0;
    public bool IsLocationNode { get; set; } = true;
    public int LocationWeight { get; set; } = 1;
}
