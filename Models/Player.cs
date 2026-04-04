namespace ApocMinimal.Models;

public class Player
{
    public int    Id           { get; set; }
    public string Name         { get; set; } = "";
    public double FaithPoints  { get; set; }
    public int    AltarLevel   { get; set; }
    public int    CurrentDay   { get; set; }

    public double DailyFaithLimit => 10 + AltarLevel * 5;
    public int    UpgradeCost     => AltarLevel * 50;
    public bool   CanUpgrade      => AltarLevel < 5 && FaithPoints >= UpgradeCost;
}
