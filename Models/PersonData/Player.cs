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
    /// <summary>Faction restriction. Empty = available to all factions.</summary>
    public string Faction { get; set; } = "";
    /// <summary>Unique catalog key for INSERT OR IGNORE deduplication.</summary>
    public string CatalogKey { get; set; } = "";

    public double TrustBoost { get; set; }
    public double FearClear { get; set; }
    public double StaminaBoost { get; set; }

    /// <summary>Activation modes stored as JSON. 1–9 entries; count scales with TerminalLevel.</summary>
    public List<ActivationMode> ActivationModes { get; set; } = new();

    /// <summary>Human-readable label for the number of activation modes.</summary>
    public string ModesLabel => ActivationModes.Count == 0 ? "—" : $"{ActivationModes.Count} реж.";
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
    public int PlayerActionsToday { get; set; }
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

}
