using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using ApocMinimal.Models;
using ApocMinimal.Systems;

namespace ApocMinimal.Database;

public class DatabaseManager
{
    private readonly string _connectionString;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public DatabaseManager()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apoc_minimal.db");
        _connectionString = $"Data Source={dbPath};Version=3;";
        InitializeDatabase();
    }

    public bool SaveExists
    {
        get
        {
            try
            {
                using var conn = OpenConnection();
                return (long)(ExecuteScalar(conn, "SELECT COUNT(*) FROM Player") ?? 0L) > 0;
            }
            catch { return false; }
        }
    }

    // =========================================================
    // Schema
    // =========================================================

    private void InitializeDatabase()
    {
        using var conn = OpenConnection();

        ExecuteNQ(conn, @"
            CREATE TABLE IF NOT EXISTS Player (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                Name             TEXT    NOT NULL,
                FaithPoints      REAL    DEFAULT 0,
                AltarLevel       INTEGER DEFAULT 1,
                CurrentDay       INTEGER DEFAULT 0,
                BarrierSize      REAL    DEFAULT 0,
                TerritoryControl INTEGER DEFAULT 0
            )");

        ExecuteNQ(conn, @"
            CREATE TABLE IF NOT EXISTS Npcs (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Name            TEXT    NOT NULL,
                Age             INTEGER DEFAULT 0,
                Gender          TEXT    DEFAULT 'Male',
                Profession      TEXT    DEFAULT '',
                Description     TEXT    DEFAULT '',
                Health          REAL    DEFAULT 100,
                Faith           REAL    DEFAULT 0,
                Stamina         REAL    DEFAULT 100,
                Chakra          REAL    DEFAULT 50,
                Fear            REAL    DEFAULT 10,
                Trust           REAL    DEFAULT 50,
                Initiative      REAL    DEFAULT 50,
                Trait           TEXT    DEFAULT 'None',
                FollowerLevel   INTEGER DEFAULT 0,
                CharTraits      TEXT    DEFAULT '[]',
                Specializations TEXT    DEFAULT '[]',
                Emotions        TEXT    DEFAULT '[]',
                Goal            TEXT    DEFAULT '',
                Dream           TEXT    DEFAULT '',
                Desire          TEXT    DEFAULT '',
                Needs           TEXT    DEFAULT '[]',
                Stats           TEXT    DEFAULT '{}',
                ActiveTask      TEXT    DEFAULT '',
                TaskDaysLeft    INTEGER DEFAULT 0,
                TaskRewardResId INTEGER DEFAULT 0,
                TaskRewardAmt   REAL    DEFAULT 0,
                Memory          TEXT    DEFAULT '[]'
            )");

        ExecuteNQ(conn, @"
            CREATE TABLE IF NOT EXISTS Resources (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Name     TEXT NOT NULL,
                Amount   REAL DEFAULT 0,
                Category TEXT DEFAULT ''
            )");

        ExecuteNQ(conn, @"
            CREATE TABLE IF NOT EXISTS Quests (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                Title            TEXT    NOT NULL,
                Description      TEXT    DEFAULT '',
                Source           TEXT    DEFAULT 'AI',
                Status           TEXT    DEFAULT 'Available',
                AssignedNpcId    INTEGER DEFAULT 0,
                DaysRequired     INTEGER DEFAULT 3,
                DaysRemaining    INTEGER DEFAULT 3,
                RewardResourceId INTEGER DEFAULT 0,
                RewardAmount     REAL    DEFAULT 0,
                FaithCost        REAL    DEFAULT 0
            )");

        ExecuteNQ(conn, @"
            CREATE TABLE IF NOT EXISTS Locations (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Name          TEXT    NOT NULL,
                Type          TEXT    DEFAULT 'Apartment',
                ParentId      INTEGER DEFAULT 0,
                ResourceNodes TEXT    DEFAULT '{}',
                DangerLevel   REAL    DEFAULT 0,
                IsExplored    INTEGER DEFAULT 0
            )");

        MigrateColumns(conn);
        SeedIfEmpty(conn);
    }

    private void MigrateColumns(SQLiteConnection conn)
    {
        var cols = new[]
        {
            ("Player", "BarrierSize",      "REAL DEFAULT 0"),
            ("Player", "TerritoryControl", "INTEGER DEFAULT 0"),
            ("Npcs",   "Gender",           "TEXT DEFAULT 'Male'"),
            ("Npcs",   "Description",      "TEXT DEFAULT ''"),
            ("Npcs",   "Stamina",          "REAL DEFAULT 100"),
            ("Npcs",   "Chakra",           "REAL DEFAULT 50"),
            ("Npcs",   "Fear",             "REAL DEFAULT 10"),
            ("Npcs",   "Trust",            "REAL DEFAULT 50"),
            ("Npcs",   "Initiative",       "REAL DEFAULT 50"),
            ("Npcs",   "FollowerLevel",    "INTEGER DEFAULT 0"),
            ("Npcs",   "CharTraits",       "TEXT DEFAULT '[]'"),
            ("Npcs",   "Specializations",  "TEXT DEFAULT '[]'"),
            ("Npcs",   "Emotions",         "TEXT DEFAULT '[]'"),
            ("Npcs",   "Goal",             "TEXT DEFAULT ''"),
            ("Npcs",   "Dream",            "TEXT DEFAULT ''"),
            ("Npcs",   "Desire",           "TEXT DEFAULT ''"),
            ("Npcs",   "Needs",            "TEXT DEFAULT '[]'"),
            ("Npcs",   "Memory",           "TEXT DEFAULT '[]'"),
            // legacy columns kept for smooth migration
            ("Npcs",   "Hunger",           "REAL DEFAULT 0"),
            ("Npcs",   "Thirst",           "REAL DEFAULT 0"),
            ("Npcs",   "Trait",            "TEXT DEFAULT 'None'"),
            ("Npcs",   "ActiveTask",       "TEXT DEFAULT ''"),
            ("Npcs",   "TaskDaysLeft",     "INTEGER DEFAULT 0"),
            ("Npcs",   "TaskRewardResId",  "INTEGER DEFAULT 0"),
            ("Npcs",   "TaskRewardAmt",    "REAL DEFAULT 0"),
            ("Npcs",   "Stats",            "TEXT DEFAULT '{}'"),
            ("Player", "CurrentDay",       "INTEGER DEFAULT 0"),
        };
        foreach (var (table, col, def) in cols)
        {
            try { ExecuteNQ(conn, $"ALTER TABLE {table} ADD COLUMN {col} {def}"); }
            catch { }
        }
    }

    private void SeedIfEmpty(SQLiteConnection conn)
    {
        var count = (long)(ExecuteScalar(conn, "SELECT COUNT(*) FROM Player") ?? 0L);
        if (count > 0) return;

        // Player
        using (var cmd = new SQLiteCommand(
            "INSERT INTO Player (Name,FaithPoints,AltarLevel,CurrentDay,BarrierSize,TerritoryControl) " +
            "VALUES (@n,@fp,@al,@cd,@bs,@tc)", conn))
        {
            cmd.Parameters.AddWithValue("@n",  "Божество");
            cmd.Parameters.AddWithValue("@fp", 0.0);
            cmd.Parameters.AddWithValue("@al", 1);
            cmd.Parameters.AddWithValue("@cd", 1);
            cmd.Parameters.AddWithValue("@bs", 0.0);
            cmd.Parameters.AddWithValue("@tc", 0);
            cmd.ExecuteNonQuery();
        }

        var rnd = new Random(42);

        // NPCs
        var npcs = new[]
        {
            ("Алексей", 32, Gender.Male,   "Механик",  90.0, 40.0, NpcTrait.Leader),
            ("Мария",   27, Gender.Female, "Медик",    85.0, 60.0, NpcTrait.None),
            ("Иван",    45, Gender.Male,   "Охотник",  95.0, 30.0, NpcTrait.Coward),
            ("Анна",    23, Gender.Female, "Инженер",  80.0, 55.0, NpcTrait.Loner),
        };

        foreach (var (name, age, gender, prof, hp, faith, trait) in npcs)
        {
            var npc = new Npc
            {
                Name        = name,
                Age         = age,
                Gender      = gender,
                Profession  = prof,
                Health      = hp,
                Faith       = faith,
                Stamina     = 100,
                Chakra      = rnd.Next(30, 70),
                Fear        = rnd.Next(5, 30),
                Trust       = rnd.Next(40, 70),
                Initiative  = rnd.Next(35, 75),
                Trait       = trait,
                FollowerLevel = 0,
                Description = NpcDescriptions.All[rnd.Next(NpcDescriptions.All.Length)],
                Goal        = NpcGoals.Goals[rnd.Next(NpcGoals.Goals.Length)],
                Dream       = NpcGoals.Dreams[rnd.Next(NpcGoals.Dreams.Length)],
                Desire      = NpcGoals.Desires[rnd.Next(NpcGoals.Desires.Length)],
                Stats       = GenerateStats(prof, rnd),
            };

            npc.CharTraits     = CharacterTraitExtensions.GeneratePair(rnd).ToList();
            npc.Emotions       = EmotionNames.GenerateRandom(rnd);
            npc.Needs          = NeedSystem.InitialiseNeeds(npc, rnd);
            npc.Specializations = GenerateSpecializations(prof, rnd);

            InsertNpc(conn, npc);
        }

        // Resources
        var resources = new[]
        {
            ("Еда",         50.0, "Базовый"),
            ("Вода",        40.0, "Базовый"),
            ("Медикаменты", 15.0, "Медицинский"),
            ("Дерево",      30.0, "Материал"),
            ("Инструменты", 10.0, "Материал"),
        };
        foreach (var (rname, amt, cat) in resources)
        {
            using var cmd = new SQLiteCommand(
                "INSERT INTO Resources (Name,Amount,Category) VALUES (@n,@a,@c)", conn);
            cmd.Parameters.AddWithValue("@n", rname);
            cmd.Parameters.AddWithValue("@a", amt);
            cmd.Parameters.AddWithValue("@c", cat);
            cmd.ExecuteNonQuery();
        }

        // Seed map (City → District → Building → Floor → Apartments)
        SeedLocations(conn, rnd);

        // Seed quests
        SeedQuests(conn, rnd);
    }

    private static void InsertNpc(SQLiteConnection conn, Npc npc)
    {
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Npcs
              (Name,Age,Gender,Profession,Description,Health,Faith,Stamina,Chakra,
               Fear,Trust,Initiative,Trait,FollowerLevel,CharTraits,Specializations,
               Emotions,Goal,Dream,Desire,Needs,Stats,
               ActiveTask,TaskDaysLeft,TaskRewardResId,TaskRewardAmt,Memory)
            VALUES
              (@nm,@ag,@gn,@pr,@ds,@hp,@fa,@st,@ck,
               @fr,@tr,@in,@tt,@fl,@ct,@sp,
               @em,@gl,@dr,@de,@nd,@ss,
               @at,@tdl,@trr,@tra,@me)", conn);

        cmd.Parameters.AddWithValue("@nm",  npc.Name);
        cmd.Parameters.AddWithValue("@ag",  npc.Age);
        cmd.Parameters.AddWithValue("@gn",  npc.Gender.ToString());
        cmd.Parameters.AddWithValue("@pr",  npc.Profession);
        cmd.Parameters.AddWithValue("@ds",  npc.Description);
        cmd.Parameters.AddWithValue("@hp",  npc.Health);
        cmd.Parameters.AddWithValue("@fa",  npc.Faith);
        cmd.Parameters.AddWithValue("@st",  npc.Stamina);
        cmd.Parameters.AddWithValue("@ck",  npc.Chakra);
        cmd.Parameters.AddWithValue("@fr",  npc.Fear);
        cmd.Parameters.AddWithValue("@tr",  npc.Trust);
        cmd.Parameters.AddWithValue("@in",  npc.Initiative);
        cmd.Parameters.AddWithValue("@tt",  npc.Trait.ToString());
        cmd.Parameters.AddWithValue("@fl",  npc.FollowerLevel);
        cmd.Parameters.AddWithValue("@ct",  JsonSerializer.Serialize(npc.CharTraits.Select(c => c.ToString()).ToList(), JsonOpts));
        cmd.Parameters.AddWithValue("@sp",  JsonSerializer.Serialize(npc.Specializations, JsonOpts));
        cmd.Parameters.AddWithValue("@em",  JsonSerializer.Serialize(npc.Emotions, JsonOpts));
        cmd.Parameters.AddWithValue("@gl",  npc.Goal);
        cmd.Parameters.AddWithValue("@dr",  npc.Dream);
        cmd.Parameters.AddWithValue("@de",  npc.Desire);
        cmd.Parameters.AddWithValue("@nd",  JsonSerializer.Serialize(npc.Needs, JsonOpts));
        cmd.Parameters.AddWithValue("@ss",  JsonSerializer.Serialize(npc.Stats, JsonOpts));
        cmd.Parameters.AddWithValue("@at",  npc.ActiveTask);
        cmd.Parameters.AddWithValue("@tdl", npc.TaskDaysLeft);
        cmd.Parameters.AddWithValue("@trr", npc.TaskRewardResId);
        cmd.Parameters.AddWithValue("@tra", npc.TaskRewardAmt);
        cmd.Parameters.AddWithValue("@me",  JsonSerializer.Serialize(npc.Memory, JsonOpts));
        cmd.ExecuteNonQuery();
    }

    private static void SeedLocations(SQLiteConnection conn, Random rnd)
    {
        // City
        long cityId = InsertLocation(conn, "Новый Харьков", LocationType.City, 0, new(), 10, true);
        // District
        long distId = InsertLocation(conn, "Центральный район", LocationType.District, (int)cityId, new(), 20, true);
        // Street
        long streetId = InsertLocation(conn, "ул. Выживших", LocationType.Street, (int)distId, new(), 25, true);
        // Building
        long bldId = InsertLocation(conn, "Жилой дом №1", LocationType.Building, (int)streetId,
            new() { ["Дерево"] = 15, ["Металлолом"] = 10 }, 20, true);
        // Floors
        for (int floor = 1; floor <= 5; floor++)
        {
            long floorId = InsertLocation(conn, $"Этаж {floor}", LocationType.Floor, (int)bldId,
                new(), 15 + rnd.Next(0, 20), floor <= 2);
            // Apartments on each floor
            for (int apt = 1; apt <= 4; apt++)
            {
                var nodes = new Dictionary<string, double>();
                var resNames = ResourceTypes.All.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 4));
                foreach (var r in resNames) nodes[r] = rnd.Next(5, 30);
                InsertLocation(conn, $"Квартира {floor}{apt:00}", LocationType.Apartment, (int)floorId,
                    nodes, rnd.Next(10, 40), false);
            }
        }
    }

    private static long InsertLocation(SQLiteConnection conn, string name, LocationType type,
        int parentId, Dictionary<string, double> nodes, double danger, bool explored)
    {
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Locations (Name,Type,ParentId,ResourceNodes,DangerLevel,IsExplored)
            VALUES (@n,@t,@p,@r,@d,@e)", conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@t", type.ToString());
        cmd.Parameters.AddWithValue("@p", parentId);
        cmd.Parameters.AddWithValue("@r", JsonSerializer.Serialize(nodes, JsonOpts));
        cmd.Parameters.AddWithValue("@d", danger);
        cmd.Parameters.AddWithValue("@e", explored ? 1 : 0);
        cmd.ExecuteNonQuery();
        return conn.LastInsertRowId;
    }

    private static void SeedQuests(SQLiteConnection conn, Random rnd)
    {
        var templates = QuestTemplates.All.OrderBy(_ => rnd.Next()).Take(3);
        foreach (var t in templates)
        {
            using var cmd = new SQLiteCommand(@"
                INSERT INTO Quests
                  (Title,Description,Source,Status,AssignedNpcId,
                   DaysRequired,DaysRemaining,RewardResourceId,RewardAmount,FaithCost)
                VALUES (@ti,@de,@so,@st,0,@dr,@drr,@rr,@ra,@fc)", conn);
            cmd.Parameters.AddWithValue("@ti",  t.Title);
            cmd.Parameters.AddWithValue("@de",  t.Desc);
            cmd.Parameters.AddWithValue("@so",  "AI");
            cmd.Parameters.AddWithValue("@st",  "Available");
            cmd.Parameters.AddWithValue("@dr",  t.Days);
            cmd.Parameters.AddWithValue("@drr", t.Days);
            cmd.Parameters.AddWithValue("@rr",  t.ResId);
            cmd.Parameters.AddWithValue("@ra",  t.Reward);
            cmd.Parameters.AddWithValue("@fc",  t.FaithCost);
            cmd.ExecuteNonQuery();
        }
    }

    // =========================================================
    // Reset
    // =========================================================

    public void ResetDatabase()
    {
        using var conn = OpenConnection();
        ExecuteNQ(conn, "DELETE FROM Player");
        ExecuteNQ(conn, "DELETE FROM Npcs");
        ExecuteNQ(conn, "DELETE FROM Resources");
        ExecuteNQ(conn, "DELETE FROM Quests");
        ExecuteNQ(conn, "DELETE FROM Locations");
        try { ExecuteNQ(conn, "DELETE FROM sqlite_sequence"); } catch { }
        SeedIfEmpty(conn);
    }

    // =========================================================
    // Read
    // =========================================================

    public Player? GetPlayer()
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("SELECT * FROM Player LIMIT 1", conn);
        using var rdr  = cmd.ExecuteReader();
        return rdr.Read() ? ReadPlayer(rdr) : null;
    }

    public List<Npc> GetAllNpcs()
    {
        var list = new List<Npc>();
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("SELECT * FROM Npcs ORDER BY Id", conn);
        using var rdr  = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(ReadNpc(rdr));
        return list;
    }

    public List<Resource> GetAllResources()
    {
        var list = new List<Resource>();
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("SELECT * FROM Resources ORDER BY Id", conn);
        using var rdr  = cmd.ExecuteReader();
        while (rdr.Read())
            list.Add(new Resource
            {
                Id       = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Name     = rdr.GetString(rdr.GetOrdinal("Name")),
                Amount   = rdr.GetDouble(rdr.GetOrdinal("Amount")),
                Category = rdr.GetString(rdr.GetOrdinal("Category")),
            });
        return list;
    }

    public List<Quest> GetAllQuests()
    {
        var list = new List<Quest>();
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("SELECT * FROM Quests ORDER BY Id", conn);
        using var rdr  = cmd.ExecuteReader();
        while (rdr.Read())
        {
            list.Add(new Quest
            {
                Id               = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Title            = rdr.GetString(rdr.GetOrdinal("Title")),
                Description      = rdr.GetString(rdr.GetOrdinal("Description")),
                Source           = Enum.TryParse<QuestSource>(rdr.GetString(rdr.GetOrdinal("Source")), out var qs) ? qs : QuestSource.AI,
                Status           = Enum.TryParse<QuestStatus>(rdr.GetString(rdr.GetOrdinal("Status")), out var st) ? st : QuestStatus.Available,
                AssignedNpcId    = rdr.GetInt32(rdr.GetOrdinal("AssignedNpcId")),
                DaysRequired     = rdr.GetInt32(rdr.GetOrdinal("DaysRequired")),
                DaysRemaining    = rdr.GetInt32(rdr.GetOrdinal("DaysRemaining")),
                RewardResourceId = rdr.GetInt32(rdr.GetOrdinal("RewardResourceId")),
                RewardAmount     = rdr.GetDouble(rdr.GetOrdinal("RewardAmount")),
                FaithCost        = rdr.GetDouble(rdr.GetOrdinal("FaithCost")),
            });
        }
        return list;
    }

    public List<Location> GetAllLocations()
    {
        var list = new List<Location>();
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("SELECT * FROM Locations ORDER BY Id", conn);
        using var rdr  = cmd.ExecuteReader();
        while (rdr.Read())
        {
            var nodesJson = rdr.IsDBNull(rdr.GetOrdinal("ResourceNodes")) ? "{}"
                : rdr.GetString(rdr.GetOrdinal("ResourceNodes"));
            Dictionary<string, double> nodes;
            try { nodes = JsonSerializer.Deserialize<Dictionary<string, double>>(nodesJson) ?? new(); }
            catch { nodes = new(); }

            list.Add(new Location
            {
                Id            = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Name          = rdr.GetString(rdr.GetOrdinal("Name")),
                Type          = Enum.TryParse<LocationType>(rdr.GetString(rdr.GetOrdinal("Type")), out var lt) ? lt : LocationType.Apartment,
                ParentId      = rdr.GetInt32(rdr.GetOrdinal("ParentId")),
                ResourceNodes = nodes,
                DangerLevel   = rdr.GetDouble(rdr.GetOrdinal("DangerLevel")),
                IsExplored    = rdr.GetInt32(rdr.GetOrdinal("IsExplored")) == 1,
            });
        }
        return list;
    }

    // =========================================================
    // Write
    // =========================================================

    public void SavePlayer(Player p)
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand(
            "UPDATE Player SET FaithPoints=@fp,AltarLevel=@al,CurrentDay=@cd,BarrierSize=@bs,TerritoryControl=@tc WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@fp", p.FaithPoints);
        cmd.Parameters.AddWithValue("@al", p.AltarLevel);
        cmd.Parameters.AddWithValue("@cd", p.CurrentDay);
        cmd.Parameters.AddWithValue("@bs", p.BarrierSize);
        cmd.Parameters.AddWithValue("@tc", p.TerritoryControl);
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveNpc(Npc n)
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand(@"
            UPDATE Npcs SET
                Health=@hp, Faith=@fa, Stamina=@st, Chakra=@ck,
                Fear=@fr, Trust=@tr, Initiative=@in, FollowerLevel=@fl,
                CharTraits=@ct, Specializations=@sp, Emotions=@em,
                Goal=@gl, Dream=@dr, Desire=@de,
                Needs=@nd, Stats=@ss,
                ActiveTask=@at, TaskDaysLeft=@tdl, TaskRewardResId=@trr, TaskRewardAmt=@tra,
                Memory=@me
            WHERE Id=@id", conn);

        cmd.Parameters.AddWithValue("@hp",  n.Health);
        cmd.Parameters.AddWithValue("@fa",  n.Faith);
        cmd.Parameters.AddWithValue("@st",  n.Stamina);
        cmd.Parameters.AddWithValue("@ck",  n.Chakra);
        cmd.Parameters.AddWithValue("@fr",  n.Fear);
        cmd.Parameters.AddWithValue("@tr",  n.Trust);
        cmd.Parameters.AddWithValue("@in",  n.Initiative);
        cmd.Parameters.AddWithValue("@fl",  n.FollowerLevel);
        cmd.Parameters.AddWithValue("@ct",  JsonSerializer.Serialize(n.CharTraits.Select(c => c.ToString()).ToList(), JsonOpts));
        cmd.Parameters.AddWithValue("@sp",  JsonSerializer.Serialize(n.Specializations, JsonOpts));
        cmd.Parameters.AddWithValue("@em",  JsonSerializer.Serialize(n.Emotions, JsonOpts));
        cmd.Parameters.AddWithValue("@gl",  n.Goal);
        cmd.Parameters.AddWithValue("@dr",  n.Dream);
        cmd.Parameters.AddWithValue("@de",  n.Desire);
        cmd.Parameters.AddWithValue("@nd",  JsonSerializer.Serialize(n.Needs, JsonOpts));
        cmd.Parameters.AddWithValue("@ss",  JsonSerializer.Serialize(n.Stats, JsonOpts));
        cmd.Parameters.AddWithValue("@at",  n.ActiveTask);
        cmd.Parameters.AddWithValue("@tdl", n.TaskDaysLeft);
        cmd.Parameters.AddWithValue("@trr", n.TaskRewardResId);
        cmd.Parameters.AddWithValue("@tra", n.TaskRewardAmt);
        cmd.Parameters.AddWithValue("@me",  JsonSerializer.Serialize(n.Memory, JsonOpts));
        cmd.Parameters.AddWithValue("@id",  n.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveResource(Resource r)
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("UPDATE Resources SET Amount=@a WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@a",  r.Amount);
        cmd.Parameters.AddWithValue("@id", r.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveQuest(Quest q)
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand(@"
            UPDATE Quests SET
                Status=@st, AssignedNpcId=@an, DaysRemaining=@dr
            WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@st", q.Status.ToString());
        cmd.Parameters.AddWithValue("@an", q.AssignedNpcId);
        cmd.Parameters.AddWithValue("@dr", q.DaysRemaining);
        cmd.Parameters.AddWithValue("@id", q.Id);
        cmd.ExecuteNonQuery();
    }

    public void InsertQuest(Quest q)
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand(@"
            INSERT INTO Quests
              (Title,Description,Source,Status,AssignedNpcId,
               DaysRequired,DaysRemaining,RewardResourceId,RewardAmount,FaithCost)
            VALUES (@ti,@de,@so,@st,@an,@dq,@dr,@rr,@ra,@fc)", conn);
        cmd.Parameters.AddWithValue("@ti", q.Title);
        cmd.Parameters.AddWithValue("@de", q.Description);
        cmd.Parameters.AddWithValue("@so", q.Source.ToString());
        cmd.Parameters.AddWithValue("@st", q.Status.ToString());
        cmd.Parameters.AddWithValue("@an", q.AssignedNpcId);
        cmd.Parameters.AddWithValue("@dq", q.DaysRequired);
        cmd.Parameters.AddWithValue("@dr", q.DaysRemaining);
        cmd.Parameters.AddWithValue("@rr", q.RewardResourceId);
        cmd.Parameters.AddWithValue("@ra", q.RewardAmount);
        cmd.Parameters.AddWithValue("@fc", q.FaithCost);
        cmd.ExecuteNonQuery();
        q.Id = (int)conn.LastInsertRowId;
    }

    // =========================================================
    // Private helpers
    // =========================================================

    private static Player ReadPlayer(SQLiteDataReader rdr) => new()
    {
        Id               = rdr.GetInt32(rdr.GetOrdinal("Id")),
        Name             = rdr.GetString(rdr.GetOrdinal("Name")),
        FaithPoints      = rdr.GetDouble(rdr.GetOrdinal("FaithPoints")),
        AltarLevel       = rdr.GetInt32(rdr.GetOrdinal("AltarLevel")),
        CurrentDay       = rdr.GetInt32(rdr.GetOrdinal("CurrentDay")),
        BarrierSize      = GetDoubleOrDefault(rdr, "BarrierSize"),
        TerritoryControl = GetIntOrDefault(rdr, "TerritoryControl"),
    };

    private static Npc ReadNpc(SQLiteDataReader rdr)
    {
        var npc = new Npc
        {
            Id           = rdr.GetInt32(rdr.GetOrdinal("Id")),
            Name         = rdr.GetString(rdr.GetOrdinal("Name")),
            Age          = rdr.GetInt32(rdr.GetOrdinal("Age")),
            Gender       = Enum.TryParse<Gender>(GetStringOrDefault(rdr, "Gender", "Male"), out var g) ? g : Gender.Male,
            Profession   = rdr.GetString(rdr.GetOrdinal("Profession")),
            Description  = GetStringOrDefault(rdr, "Description"),
            Health       = rdr.GetDouble(rdr.GetOrdinal("Health")),
            Faith        = rdr.GetDouble(rdr.GetOrdinal("Faith")),
            Stamina      = GetDoubleOrDefault(rdr, "Stamina", 100),
            Chakra       = GetDoubleOrDefault(rdr, "Chakra",  50),
            Fear         = GetDoubleOrDefault(rdr, "Fear",    10),
            Trust        = GetDoubleOrDefault(rdr, "Trust",   50),
            Initiative   = GetDoubleOrDefault(rdr, "Initiative", 50),
            Trait        = Enum.TryParse<NpcTrait>(GetStringOrDefault(rdr, "Trait", "None"), out var t) ? t : NpcTrait.None,
            FollowerLevel= GetIntOrDefault(rdr, "FollowerLevel"),
            Goal         = GetStringOrDefault(rdr, "Goal"),
            Dream        = GetStringOrDefault(rdr, "Dream"),
            Desire       = GetStringOrDefault(rdr, "Desire"),
            ActiveTask   = GetStringOrDefault(rdr, "ActiveTask"),
            TaskDaysLeft    = GetIntOrDefault(rdr, "TaskDaysLeft"),
            TaskRewardResId = GetIntOrDefault(rdr, "TaskRewardResId"),
            TaskRewardAmt   = GetDoubleOrDefault(rdr, "TaskRewardAmt"),
        };

        npc.Stats           = DeserializeOrDefault<Dictionary<int, double>>(rdr, "Stats")   ?? new();
        npc.Needs           = DeserializeOrDefault<List<Need>>(rdr, "Needs")                ?? new();
        npc.Emotions        = DeserializeOrDefault<List<Emotion>>(rdr, "Emotions")          ?? new();
        npc.Specializations = DeserializeOrDefault<List<string>>(rdr, "Specializations")   ?? new();
        npc.Memory          = DeserializeOrDefault<List<MemoryEntry>>(rdr, "Memory")        ?? new();

        var charTraitStrings = DeserializeOrDefault<List<string>>(rdr, "CharTraits") ?? new();
        npc.CharTraits = charTraitStrings
            .Where(s => Enum.TryParse<CharacterTrait>(s, out _))
            .Select(s => Enum.Parse<CharacterTrait>(s))
            .ToList();

        // Migrate old Hunger/Thirst to Needs if Needs list is empty
        if (npc.Needs.Count == 0)
        {
            var hunger = GetDoubleOrDefault(rdr, "Hunger");
            var thirst = GetDoubleOrDefault(rdr, "Thirst");
            npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(npc.Id));
            NeedSystem.SatisfyNeed(npc, "Еда",  Math.Max(0, 100 - hunger));
            NeedSystem.SatisfyNeed(npc, "Вода", Math.Max(0, 100 - thirst));
        }

        return npc;
    }

    private static T? DeserializeOrDefault<T>(SQLiteDataReader rdr, string col) where T : class
    {
        try
        {
            int ord = rdr.GetOrdinal(col);
            if (rdr.IsDBNull(ord)) return null;
            return JsonSerializer.Deserialize<T>(rdr.GetString(ord));
        }
        catch { return null; }
    }

    private static string GetStringOrDefault(SQLiteDataReader rdr, string col, string def = "")
    {
        try
        {
            int ord = rdr.GetOrdinal(col);
            return rdr.IsDBNull(ord) ? def : rdr.GetString(ord);
        }
        catch { return def; }
    }

    private static double GetDoubleOrDefault(SQLiteDataReader rdr, string col, double def = 0)
    {
        try
        {
            int ord = rdr.GetOrdinal(col);
            return rdr.IsDBNull(ord) ? def : rdr.GetDouble(ord);
        }
        catch { return def; }
    }

    private static int GetIntOrDefault(SQLiteDataReader rdr, string col, int def = 0)
    {
        try
        {
            int ord = rdr.GetOrdinal(col);
            return rdr.IsDBNull(ord) ? def : rdr.GetInt32(ord);
        }
        catch { return def; }
    }

    private SQLiteConnection OpenConnection()
    {
        var conn = new SQLiteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static void ExecuteNQ(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private static object? ExecuteScalar(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        return cmd.ExecuteScalar();
    }

    // ── NPC generation helpers ──────────────────────────────────────────────

    private static Dictionary<int, double> GenerateStats(string profession, Random rnd)
    {
        var stats = new Dictionary<int, double>();
        for (int i = 1; i <= 30; i++)
            stats[i] = rnd.Next(15, 55);

        int[] bonuses = profession switch
        {
            "Механик"  => new[] { 1, 2, 3, 7, 11, 12, 26 },
            "Медик"    => new[] { 2, 4, 5, 12, 13, 14, 17, 28 },
            "Охотник"  => new[] { 1, 2, 3, 7, 9, 17, 19, 29 },
            "Инженер"  => new[] { 2, 6, 11, 12, 15, 18, 20, 25 },
            _          => new[] { 1, 3, 6, 12, 17, 23 },
        };

        foreach (var id in bonuses)
            stats[id] = Math.Min(100, stats[id] + rnd.Next(28, 45));

        return stats;
    }

    private static List<string> GenerateSpecializations(string profession, Random rnd)
    {
        var all = new[]
        {
            "Стрельба", "Рукопашный бой", "Медицина", "Инженерия", "Выживание",
            "Разведка", "Лидерство", "Торговля", "Механика", "Электроника",
            "Кулинария", "Охота", "Строительство", "Химия", "Связь",
        };
        int count = rnd.Next(2, 8);
        return all.OrderBy(_ => rnd.Next()).Take(count).ToList();
    }
}
