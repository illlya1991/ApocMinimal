namespace ApocMinimal.Models;

/// <summary>Stage F technique (unlocked every 2 altar levels).</summary>
public class Technique
{
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
    public int    AltarLevel  { get; set; }   // unlocked at this altar level
    public double FaithCost   { get; set; }
}

public class Player
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = "";
    public double FaithPoints { get; set; }
    public int    AltarLevel  { get; set; } = 1;   // 1–10
    public int    CurrentDay  { get; set; }

    // ── Stage F fields ────────────────────────────────────────────────
    /// <summary>Altar can project a protective barrier of this radius (0 = none).</summary>
    public double BarrierSize      { get; set; }
    /// <summary>Number of controlled location IDs.</summary>
    public int    TerritoryControl { get; set; }
    /// <summary>Max simultaneous followers (grows with altar level).</summary>
    public int    MaxFollowers     => 2 + AltarLevel * 2;

    // ── Derived ────────────────────────────────────────────────────────
    public double DailyFaithLimit => 10 + AltarLevel * 8;
    public int    UpgradeCost     => AltarLevel * 75;
    public bool   CanUpgrade      => AltarLevel < 10 && FaithPoints >= UpgradeCost;

    // ── Techniques (unlocked every 2 levels) ──────────────────────────
    public static readonly Technique[] AllTechniques =
    {
        new() { Name="Благословение",       AltarLevel=2, FaithCost=10,
                Description="Даёт +20 здоровья одному НПС." },
        new() { Name="Исцеление",           AltarLevel=4, FaithCost=20,
                Description="Полностью восстанавливает здоровье одного НПС." },
        new() { Name="Щит веры",            AltarLevel=6, FaithCost=30,
                Description="Создаёт барьер вокруг алтаря на 3 дня." },
        new() { Name="Откровение",          AltarLevel=8, FaithCost=40,
                Description="Вскрывает все соседние локации на карте." },
        new() { Name="Апокалиптический удар",AltarLevel=10,FaithCost=100,
                Description="Мгновенно уничтожает одну враждебную группу." },
    };

    public IEnumerable<Technique> UnlockedTechniques =>
        AllTechniques.Where(t => t.AltarLevel <= AltarLevel);
}
