using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.Models.PersonData;

/// <summary>A technique grantable to NPCs or usable by the player via the terminal.</summary>
public class Technique
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int TerminalLevel { get; set; }   // min terminal level to grant/use
    public double OPCost { get; set; }

    // ── Technique system ──────────────────────────────────────────────
    public TechniqueLevel TechLevel { get; set; } = TechniqueLevel.Initiate;
    public TechniqueType TechType { get; set; } = TechniqueType.Energy;
    public double EnergyCost { get; set; }
    public double StaminaCost { get; set; }
    /// <summary>Required minimum stat values (StatId → minValue).</summary>
    public Dictionary<int, double> RequiredStats { get; set; } = new();
    /// <summary>
    /// If > 0, Apply() heals the target NPC by this amount instead of boosting stats.
    /// </summary>
    public double HealAmount { get; set; }
}

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double DevPoints { get; set; }
    public int TerminalLevel { get; set; } = 1;   // 1–10
    public int CurrentDay { get; set; }
    public PlayerFaction Faction { get; set; } = PlayerFaction.ElementMages;

    // ── Barrier & territory ───────────────────────────────────────────
    public int BarrierLevel { get; set; } = 1;
    public int TerritoryControl { get; set; }
    public List<int> ControlledZoneIds { get; set; } = new();

    public int BaseUnits =>
        (int)(TerminalLevel * (BarrierLevel * (BarrierLevel + 1)) / 2.0 * FactionCoeffs.CoeffBarrierUnits);

    // ── Faction coefficients ──────────────────────────────────────────
    public FactionCoefficients FactionCoeffs { get; set; } = new();

    // ── Player action hour ────────────────────────────────────────────
    /// <summary>How many direct player actions were taken today.</summary>
    public int PlayerActionsToday { get; set; }
    /// <summary>Maximum player direct actions per day (1 "player hour").</summary>
    public const int MaxPlayerActionsPerDay = 10;

    // ── Derived ────────────────────────────────────────────────────────
    public long UpgradeCost => (long)(200 * Math.Pow(5, TerminalLevel - 1) * FactionCoeffs.CoeffTerminalUpgradeCost);
    public bool CanUpgrade => TerminalLevel < 10 && DevPoints >= UpgradeCost;

    /// <summary>Max ОР any single NPC can generate per day (reached at follower level 5).</summary>
    public const double MaxDevPointsPerNpcPerDay = 10.0;

    // ── Follower limits per terminal level ────────────────────────────
    // [terminalLevel 1..10, followerLevel 0..5]; -1 = unlimited
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
        int al = Math.Clamp(TerminalLevel, 0, 10);
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
        new() { Name="Ученик базовый",        TerminalLevel=1,  OPCost=5,
                TechLevel=TechniqueLevel.Initiate,    TechType=TechniqueType.Energy,
                EnergyCost=5,   StaminaCost=2,  HealAmount=10,
                Description="Базовая техника ученика. Даёт +10 здоровья одному НПС." },
        new() { Name="Благословение",        TerminalLevel=2,  OPCost=10,
                TechLevel=TechniqueLevel.Initiate,    TechType=TechniqueType.Energy,
                EnergyCost=10,  StaminaCost=5,  HealAmount=20,
                Description="Даёт +20 здоровья одному НПС." },
        new() { Name="Исцеление",            TerminalLevel=4,  OPCost=20,
                TechLevel=TechniqueLevel.Adept,       TechType=TechniqueType.Energy,
                EnergyCost=20,  StaminaCost=10, HealAmount=100,
                Description="Полностью восстанавливает здоровье одного НПС." },
        new() { Name="Щит веры",             TerminalLevel=6,  OPCost=30,
                TechLevel=TechniqueLevel.Warrior,     TechType=TechniqueType.Energy,
                EnergyCost=30,  StaminaCost=15,
                Description="Создаёт барьер вокруг алтаря на 3 дня." },
        new() { Name="Откровение",           TerminalLevel=8,  OPCost=40,
                TechLevel=TechniqueLevel.Master,      TechType=TechniqueType.Mental,
                EnergyCost=40,  StaminaCost=20,
                Description="Вскрывает все соседние локации на карте." },
        new() { Name="Апокалиптический удар",TerminalLevel=10, OPCost=100,
                TechLevel=TechniqueLevel.Apex,        TechType=TechniqueType.Physical,
                EnergyCost=80,  StaminaCost=50,
                Description="Мгновенно уничтожает одну враждебную группу." },
    };

    public IEnumerable<Technique> UnlockedTechniques =>
        AllTechniques.Where(t => t.TerminalLevel <= TerminalLevel);
}
