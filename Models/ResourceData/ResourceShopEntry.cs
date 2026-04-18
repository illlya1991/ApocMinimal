namespace ApocMinimal.Models.ResourceData;

public class ResourceShopEntry
{
    public int Id { get; set; }
    public string ResourceName { get; set; } = "";
    public int Quality { get; set; } = 1;  // 1-5
    public double FaithCostPer10 { get; set; }  // OV per 10 units
    public bool IsUnlocked { get; set; } = false;
}
