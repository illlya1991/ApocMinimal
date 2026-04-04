namespace ApocMinimal.Models;

public class Resource
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = "";
    public double Amount   { get; set; }
    public string Category { get; set; } = "";
}
