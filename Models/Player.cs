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

    // ── Derived ────────────────────────────────────────────────────────
    /// <summary>200 × 5^(level-1). Level 1=200, 2=1000, 3=5000 … 10=390 625 000.</summary>
    public long   UpgradeCost => (long)(200 * Math.Pow(5, AltarLevel - 1));
    public bool   CanUpgrade  => AltarLevel < 10 && FaithPoints >= UpgradeCost;

    /// <summary>Max ОВ any single NPC can generate per day (reached at follower level 5).</summary>
    public const double MaxFaithPerNpcPerDay = 10.0;

    // ── Follower limits per altar level ───────────────────────────────
    // [altarLevel 1..10, followerLevel 0..5]; -1 = unlimited
    private static readonly int[,] _followerLimits =
    {
        //           lvl0   lvl1   lvl2   lvl3   lvl4   lvl5
        {              0,     0,     0,     0,     0,     0 }, // altar 0 (unused)
        {             10,     5,     3,     1,     0,     0 }, // altar 1
        {             30,    10,     5,     3,     1,     0 }, // altar 2
        {            100,    30,    15,    10,     3,     1 }, // altar 3
        {            500,   100,    50,    25,    10,     2 }, // altar 4
        {             -1,   300,   150,    50,    25,     3 }, // altar 5
        {             -1,    -1,  1000,   300,   100,    10 }, // altar 6
        {             -1,    -1,    -1,  1000,   300,    30 }, // altar 7
        {             -1,    -1,    -1,    -1,  1000,   100 }, // altar 8
        {             -1,    -1,    -1,    -1,    -1,   300 }, // altar 9
        {             -1,    -1,    -1,    -1,    -1,    -1 }, // altar 10
    };

    /// <summary>
    /// How many followers of <paramref name="followerLevel"/> are allowed at current altar level.
    /// Returns -1 for unlimited, 0 if the level is not unlocked yet.
    /// </summary>
    public int GetFollowerLimit(int followerLevel)
    {
        int al = Math.Clamp(AltarLevel, 0, 10);
        int fl = Math.Clamp(followerLevel, 0, 5);
        return _followerLimits[al, fl];
    }

    /// <summary>Total non-zero-level followers allowed (sum of lvl1–5 limits).</summary>
    public int MaxActiveFollowers
    {
        get
        {
            int sum = 0;
            for (int fl = 1; fl <= 5; fl++)
            {
                int lim = GetFollowerLimit(fl);
                if (lim == -1) return int.MaxValue;
                sum += lim;
            }
            return sum;
        }
    }

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
