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

    // ── Player-level technique effects ────────────────────────────────
    /// <summary>If true, costs Player.Energy (not NPC energy or ОР).</summary>
    public bool IsPlayerTechnique { get; set; }
    /// <summary>Trust bonus applied to all alive followers.</summary>
    public double TrustBoost { get; set; }
    /// <summary>Fear reduction applied to all alive followers.</summary>
    public double FearClear { get; set; }
    /// <summary>Stamina bonus applied to all alive followers.</summary>
    public double StaminaBoost { get; set; }
    /// <summary>Threat damage dealt to the highest-threat monster faction.</summary>
    public double ThreatDamage { get; set; }
    /// <summary>ОР gained (scaled by followers' Logic/Creativity avg).</summary>
    public double DevPointsBoost { get; set; }
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

    // ── Divine energy ─────────────────────────────────────────────────
    public double Energy { get; set; } = 100.0;
    public int MaxEnergy => 50 + TerminalLevel * 10;
    public int EnergyRegenPerDay => 10 + TerminalLevel * 3;

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

        // ── Социальные (Mental, NPC Energy) ──────────────────────────
        new() { Name="Проповедь",          TerminalLevel=1,
                TechLevel=TechniqueLevel.Initiate, TechType=TechniqueType.Mental,
                EnergyCost=20, OPCost=0, TrustBoost=6,
                Description="Слово Терминала укрепляет веру. +6 Доверия цели (НПС с наименьшим Доверием)." },
        new() { Name="Внушение",           TerminalLevel=3,
                TechLevel=TechniqueLevel.Adept,    TechType=TechniqueType.Mental,
                EnergyCost=30, OPCost=0, TrustBoost=18,
                Description="Целенаправленное внушение. +18 Доверия цели (НПС с наименьшим Доверием)." },
        new() { Name="Очищение разума",    TerminalLevel=5,
                TechLevel=TechniqueLevel.Warrior,  TechType=TechniqueType.Mental,
                EnergyCost=45, OPCost=0, TrustBoost=10, FearClear=100,
                Description="Страх рассеивается. −100 Страха и +10 Доверия НПС с наибольшим Страхом." },

        // ── Крафтинг (Energy, NPC Energy) ────────────────────────────
        new() { Name="Ресурсный анализ",   TerminalLevel=2,
                TechLevel=TechniqueLevel.Initiate, TechType=TechniqueType.Energy,
                EnergyCost=15, OPCost=0,
                Description="Настройка энергетических каналов НПС. Усиливает все энергетические характеристики." },
        new() { Name="Силовое укрепление", TerminalLevel=4,
                TechLevel=TechniqueLevel.Adept,    TechType=TechniqueType.Energy,
                EnergyCost=25, OPCost=0, StaminaBoost=25,
                Description="+25 Выносливости НПС с наименьшей Выносливостью." },
        new() { Name="Синтез силы",        TerminalLevel=6,
                TechLevel=TechniqueLevel.Warrior,  TechType=TechniqueType.Energy,
                EnergyCost=50, OPCost=0,
                Description="Мощный синтез энергий. Значительно усиливает все энергетические характеристики НПС." },

        // ── Боевые (Physical, IsPlayerTechnique) ─────────────────────
        new() { Name="Боевой дух",      TerminalLevel=2,  IsPlayerTechnique=true,
                TechLevel=TechniqueLevel.Initiate, TechType=TechniqueType.Physical,
                EnergyCost=20, StaminaBoost=20, TrustBoost=5,
                Description="Боевой призыв. +20 Выносливости и +5 Доверия всем последователям." },
        new() { Name="Удар возмездия",  TerminalLevel=7,  IsPlayerTechnique=true,
                TechLevel=TechniqueLevel.Master,   TechType=TechniqueType.Physical,
                EnergyCost=60, ThreatDamage=25,
                Description="Мощный удар по угрозе. Снижает уровень угрозы опаснейшей фракции монстров на 25 (масштабируется от Силы последователей)." },
    };

    public IEnumerable<Technique> UnlockedTechniques =>
        AllTechniques.Where(t => t.TerminalLevel <= TerminalLevel);
}
