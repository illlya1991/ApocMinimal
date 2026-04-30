namespace ApocMinimal.Models.PersonData;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double DevPoints { get; set; }
    public int BaseUnits { get; set; }
    public int TerminalLevel { get; set; } = 1;   // 1–10
    public int CurrentDay { get; set; }
    public PlayerFaction Faction { get; set; } = PlayerFaction.ElementMages;

    // ── Barrier & territory ───────────────────────────────────────────
    public int BarrierLevel { get; set; } = 1;
    public int TerritoryControl { get; set; }
    public List<int> ControlledZoneIds { get; set; } = new();

    public int MaxBaseUnits =>
        (int)(TerminalLevel * (BarrierLevel * (BarrierLevel + 1)) / 2.0 * FactionCoeffs.CoeffBarrierUnits);

    public int FreeBaseUnits
    {
        get
        {
            return MaxBaseUnits - BaseUnits;
        }
    }

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
        {              0,     0,     0,     0,     0,     0 }, // Terminal 0 (unused)
        {             10,     5,     3,     1,     0,     0 }, // Terminal 1
        {             30,    10,     5,     3,     1,     0 }, // Terminal 2
        {            100,    30,    15,    10,     3,     1 }, // Terminal 3
        {            500,   100,    50,    25,    10,     2 }, // Terminal 4
        {             -1,   300,   150,    50,    25,     3 }, // Terminal 5
        {             -1,    -1,  1000,   300,   100,    10 }, // Terminal 6
        {             -1,    -1,    -1,  1000,   300,    30 }, // Terminal 7
        {             -1,    -1,    -1,    -1,  1000,   100 }, // Terminal 8
        {             -1,    -1,    -1,    -1,    -1,   300 }, // Terminal 9
        {             -1,    -1,    -1,    -1,    -1,    -1 }, // Terminal 10
    };

    /// <summary>
    /// How many followers of <paramref name="followerLevel"/> are allowed at current Terminal level.
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
