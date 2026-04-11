using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Windows;
using ApocalypseSimulation.Models.StatisticsData;
using ApocMinimal.Models.DaysData;
using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.Systems;

namespace ApocMinimal.Database;

public class DatabaseManager
{
    private readonly int _maxSavesCount;
    private OneSave _thisSave;
    private OneSave _templateSave;
    private List<OneSave> _ListSaves;
    private static SQLiteConnection _conn = new SQLiteConnection();

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public DatabaseManager()
    {
        _maxSavesCount = 3;
        _ListSaves = new List<OneSave>();
        string NameTemplate = "apoc_minimal_template.db", NameSave = $"Saves\\apocSave_";
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataBase\\");
        _templateSave = new OneSave(dbPath + NameTemplate, true);

        for (int i = 1; i <= _maxSavesCount; i++)
        {
            _ListSaves.Add(new OneSave(dbPath + NameSave + i.ToString() + ".db"));
        }
        _thisSave = _ListSaves[0];
        InitializeDatabase();
    }

    public List<OneSave> ListSaves{ get { return _ListSaves; } }

    public int MaxSaves { get { return _maxSavesCount; } }
    public OneSave ThisSave { get { return _thisSave; } set { _thisSave = value; } }

    // =========================================================
    // Schema
    // =========================================================

    private void InitializeDatabase()
    {     
        foreach (var item in _ListSaves)
        {
            item._active = false;
            try
            {
                OpenConnection(item._connectionString);
                if (IsTableExistsSafe())
                {
                    object? result = ExecuteScalar("SELECT CurrentDay FROM Player Limit 1");
                    long currentDay = (result != null) ? (long)result : 0;
                    item._active = currentDay > 1;
                }
            }
            catch { }   
        }
        OpenConnection("");
    }
    private void InitializeDatabase_Old()
    {

        ExecuteNQ(@"
            CREATE TABLE IF NOT EXISTS Player (
                Id                   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name                 TEXT    NOT NULL,
                FaithPoints          REAL    DEFAULT 0,
                AltarLevel           INTEGER DEFAULT 1,
                CurrentDay           INTEGER DEFAULT 0,
                BarrierSize          REAL    DEFAULT 0,
                TerritoryControl     INTEGER DEFAULT 0,
                PlayerActionsToday   INTEGER DEFAULT 0
            )");

        ExecuteNQ(@"
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

        ExecuteNQ(@"
            CREATE TABLE IF NOT EXISTS Resources (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Name     TEXT NOT NULL,
                Amount   REAL DEFAULT 0,
                Category TEXT DEFAULT ''
            )");

        ExecuteNQ(@"
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

        ExecuteNQ(@"
            CREATE TABLE IF NOT EXISTS Locations (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Name            TEXT    NOT NULL,
                Type            TEXT    DEFAULT 'Apartment',
                ParentId        INTEGER DEFAULT 0,
                ResourceNodes   TEXT    DEFAULT '{}',
                DangerLevel     REAL    DEFAULT 0,
                IsExplored      INTEGER DEFAULT 0,
                Status          TEXT    DEFAULT 'Dangerous',
                MonsterTypeName TEXT    DEFAULT ''
            )");

        ExecuteNQ(@"
            CREATE TABLE IF NOT EXISTS Techniques (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Name         TEXT    NOT NULL,
                Description  TEXT    DEFAULT '',
                AltarLevel   INTEGER DEFAULT 1,
                TechLevel    TEXT    DEFAULT 'Genin',
                TechType     TEXT    DEFAULT 'Energy',
                FaithCost    REAL    DEFAULT 0,
                ChakraCost   REAL    DEFAULT 0,
                StaminaCost  REAL    DEFAULT 0,
                RequiredStats TEXT   DEFAULT '{}'
            )");

        MigrateColumns();
        SeedIfEmpty();
        SeedTechniquesIfEmpty();
    }

    public bool HasAnyActiveSave()
    {
        return _ListSaves != null && _ListSaves.Any(save => save._active);
    }

    private void MigrateColumns()
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
            ("Player",     "CurrentDay",         "INTEGER DEFAULT 0"),
            ("Player",     "PlayerActionsToday", "INTEGER DEFAULT 0"),
            ("Npcs",       "CombatInitiative",   "REAL DEFAULT 50"),
            ("Locations",  "Status",             "TEXT DEFAULT 'Dangerous'"),
            ("Locations",  "MonsterTypeName",    "TEXT DEFAULT ''"),
            ("Locations",  "MapState",           "TEXT DEFAULT 'Current'"),
        };
        foreach (var (table, col, def) in cols)
        {
            try { ExecuteNQ($"ALTER TABLE {table} ADD COLUMN {col} {def}"); }
            catch { }
        }

        // НОВЫЕ колонки для Statistics
        try { ExecuteNQ("ALTER TABLE Npcs ADD COLUMN StatsBaseValues TEXT DEFAULT '[]'"); } catch { }
        try { ExecuteNQ("ALTER TABLE Npcs ADD COLUMN StatsDeviations TEXT DEFAULT '[]'"); } catch { }

        // НОВАЯ таблица для модификаторов
        ExecuteNQ(@"
        CREATE TABLE IF NOT EXISTS NpcModifiers (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            NpcId INTEGER NOT NULL,
            StatId TEXT NOT NULL,
            ModifierId TEXT NOT NULL,
            Name TEXT NOT NULL,
            Source TEXT NOT NULL,
            Type INTEGER NOT NULL,
            Value REAL NOT NULL,
            ModifierClass TEXT NOT NULL,
            IsActive INTEGER DEFAULT 1,
            TimeUnit INTEGER DEFAULT 0,
            Duration INTEGER DEFAULT 0,
            Remaining INTEGER DEFAULT 0,
            ConditionJson TEXT DEFAULT '{}'
        )");
    }

    private void SeedIfEmpty()
    {
        var count = (long)(ExecuteScalar("SELECT COUNT(*) FROM Player") ?? 0L);
        if (count > 0) return;

        // Player
        using (var cmd = new SQLiteCommand(
            "INSERT INTO Player (Name,FaithPoints,AltarLevel,CurrentDay,BarrierSize,TerritoryControl) " +
            "VALUES (@n,@fp,@al,@cd,@bs,@tc)", _conn))
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
                Initiative       = rnd.Next(35, 75),
                CombatInitiative = rnd.Next(30, 80),
                Trait            = trait,
                FollowerLevel = 0,
                Description = NpcDescriptions.All[rnd.Next(NpcDescriptions.All.Length)],
                Goal        = NpcGoals.Goals[rnd.Next(NpcGoals.Goals.Length)],
                Dream       = NpcGoals.Dreams[rnd.Next(NpcGoals.Dreams.Length)],
                Desire      = NpcGoals.Desires[rnd.Next(NpcGoals.Desires.Length)],
                Stats       = GenerateStatistics(prof, rnd),
            };

            npc.CharTraits     = CharacterTraitExtensions.GeneratePair(rnd).ToList();
            npc.Emotions       = EmotionNames.GenerateRandom(rnd);
            npc.Needs          = NeedSystem.InitialiseNeeds(npc, rnd);
            npc.Specializations = GenerateSpecializations(prof, rnd);

            InsertNpc(npc);
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
                "INSERT INTO Resources (Name,Amount,Category) VALUES (@n,@a,@c)", _conn);
            cmd.Parameters.AddWithValue("@n", rname);
            cmd.Parameters.AddWithValue("@a", amt);
            cmd.Parameters.AddWithValue("@c", cat);
            cmd.ExecuteNonQuery();
        }

        // Seed map (City → District → Building → Floor → Apartments)
        SeedLocations(rnd);

        // Seed quests
        SeedQuests(rnd);
    }

    private static void InsertNpc(Npc npc)
    {
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Npcs
              (Name,Age,Gender,Profession,Description,Health,Faith,Stamina,Chakra,
               Fear,Trust,Initiative,CombatInitiative,Trait,FollowerLevel,CharTraits,Specializations,
               Emotions,Goal,Dream,Desire,Needs,Stats,
               ActiveTask,TaskDaysLeft,TaskRewardResId,TaskRewardAmt,Memory)
            VALUES
              (@nm,@ag,@gn,@pr,@ds,@hp,@fa,@st,@ck,
               @fr,@tr,@in,@ci,@tt,@fl,@ct,@sp,
               @em,@gl,@dr,@de,@nd,@ss,
               @at,@tdl,@trr,@tra,@me)", _conn);

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
        cmd.Parameters.AddWithValue("@ci",  npc.CombatInitiative);
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

    private static void SeedLocations(Random rnd)
    {
        // City
        long cityId = InsertLocation("Новый Харьков", LocationType.City, 0, new(), 10, true);
        // District
        long distId = InsertLocation("Центральный район", LocationType.District, (int)cityId, new(), 20, true);
        // Street
        long streetId = InsertLocation("ул. Выживших", LocationType.Street, (int)distId, new(), 25, true);
        // Building
        long bldId = InsertLocation("Жилой дом №1", LocationType.Building, (int)streetId,
            new() { ["Дерево"] = 15, ["Металлолом"] = 10 }, 20, true);
        // Floors
        for (int floor = 1; floor <= 5; floor++)
        {
            long floorId = InsertLocation($"Этаж {floor}", LocationType.Floor, (int)bldId,
                new(), 15 + rnd.Next(0, 20), floor <= 2);
            // Apartments on each floor
            for (int apt = 1; apt <= 4; apt++)
            {
                var nodes = new Dictionary<string, double>();
                var resNames = ResourceTypes.All.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 4));
                foreach (var r in resNames) nodes[r] = rnd.Next(5, 30);
                InsertLocation($"Квартира {floor}{apt:00}", LocationType.Apartment, (int)floorId,
                    nodes, rnd.Next(10, 40), false);
            }
        }
    }

    private static long InsertLocation(string name, LocationType type,
        int parentId, Dictionary<string, double> nodes, double danger, bool explored)
    {
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Locations (Name,Type,ParentId,ResourceNodes,DangerLevel,IsExplored)
            VALUES (@n,@t,@p,@r,@d,@e)", _conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@t", type.ToString());
        cmd.Parameters.AddWithValue("@p", parentId);
        cmd.Parameters.AddWithValue("@r", JsonSerializer.Serialize(nodes, JsonOpts));
        cmd.Parameters.AddWithValue("@d", danger);
        cmd.Parameters.AddWithValue("@e", explored ? 1 : 0);
        cmd.ExecuteNonQuery();
        return _conn.LastInsertRowId;
    }

    private static void SeedQuests(Random rnd)
    {
        var templates = QuestTemplates.All.OrderBy(_ => rnd.Next()).Take(3);
        foreach (var t in templates)
        {
            using var cmd = new SQLiteCommand(@"
                INSERT INTO Quests
                  (Title,Description,Source,Status,AssignedNpcId,
                   DaysRequired,DaysRemaining,RewardResourceId,RewardAmount,FaithCost)
                VALUES (@ti,@de,@so,@st,0,@dr,@drr,@rr,@ra,@fc)", _conn);
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

    public void SaveLocation(Location loc)
    {
        using var cmd  = new SQLiteCommand(
            "UPDATE Locations SET Status=@st, MonsterTypeName=@mt, MapState=@ms WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@st", loc.Status.ToString());
        cmd.Parameters.AddWithValue("@mt", loc.MonsterTypeName);
        cmd.Parameters.AddWithValue("@ms", loc.MapState.ToString());
        cmd.Parameters.AddWithValue("@id", loc.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Techniques ────────────────────────────────────────────────────

    public List<Technique> GetAllTechniques()
    {
        var list = new List<Technique>();
        using var cmd  = new SQLiteCommand("SELECT * FROM Techniques ORDER BY AltarLevel, Id", _conn);
        using var rdr  = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(ReadTechnique(rdr));
        return list;
    }

    public void InsertTechnique(Technique t)
    {
        using var cmd  = new SQLiteCommand(@"
            INSERT INTO Techniques
              (Name,Description,AltarLevel,TechLevel,TechType,FaithCost,ChakraCost,StaminaCost,RequiredStats)
            VALUES (@nm,@ds,@al,@tl,@tt,@fc,@cc,@sc,@rs)", _conn);
        cmd.Parameters.AddWithValue("@nm", t.Name);
        cmd.Parameters.AddWithValue("@ds", t.Description);
        cmd.Parameters.AddWithValue("@al", t.AltarLevel);
        cmd.Parameters.AddWithValue("@tl", t.TechLevel.ToString());
        cmd.Parameters.AddWithValue("@tt", t.TechType.ToString());
        cmd.Parameters.AddWithValue("@fc", t.FaithCost);
        cmd.Parameters.AddWithValue("@cc", t.ChakraCost);
        cmd.Parameters.AddWithValue("@sc", t.StaminaCost);
        cmd.Parameters.AddWithValue("@rs", JsonSerializer.Serialize(t.RequiredStats, JsonOpts));
        cmd.ExecuteNonQuery();
        t.Id = (int)_conn.LastInsertRowId;
    }

    public void SaveTechnique(Technique t)
    {
        using var cmd  = new SQLiteCommand(
            "UPDATE Techniques SET Name=@nm,Description=@ds,AltarLevel=@al,TechLevel=@tl," +
            "TechType=@tt,FaithCost=@fc,ChakraCost=@cc,StaminaCost=@sc,RequiredStats=@rs WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@nm", t.Name);
        cmd.Parameters.AddWithValue("@ds", t.Description);
        cmd.Parameters.AddWithValue("@al", t.AltarLevel);
        cmd.Parameters.AddWithValue("@tl", t.TechLevel.ToString());
        cmd.Parameters.AddWithValue("@tt", t.TechType.ToString());
        cmd.Parameters.AddWithValue("@fc", t.FaithCost);
        cmd.Parameters.AddWithValue("@cc", t.ChakraCost);
        cmd.Parameters.AddWithValue("@sc", t.StaminaCost);
        cmd.Parameters.AddWithValue("@rs", JsonSerializer.Serialize(t.RequiredStats, JsonOpts));
        cmd.Parameters.AddWithValue("@id", t.Id);
        cmd.ExecuteNonQuery();
    }

    // =========================================================
    // Reset
    // =========================================================

    public void ResetDatabase()
    {
        // Перевіряємо, чи існує файл шаблону
        if (!File.Exists(_templateSave._fileName))
        {
            throw new FileNotFoundException($"Файл шаблону не знайдено: {_templateSave._fileName}");
        }

        // Закриваємо всі підключення до БД (якщо відкриті)
        CloseDatabaseConnections();

        // Якщо файл БД існує - видаляємо
        if (File.Exists(_thisSave._fileName))
        {
            File.Delete(_thisSave._fileName);
        }

        // Копіюємо файл шаблону
        File.Copy(_templateSave._fileName, _thisSave._fileName);

        OpenConnection(_thisSave._connectionString);


        //ExecuteNQ("DELETE FROM Player");
        //ExecuteNQ("DELETE FROM Npcs");
        //ExecuteNQ("DELETE FROM Resources");
        //ExecuteNQ("DELETE FROM Quests");
        //ExecuteNQ("DELETE FROM Locations");
        //ExecuteNQ("DELETE FROM Techniques");
        //try { ExecuteNQ("DELETE FROM sqlite_sequence"); } catch { }
        //SeedIfEmpty();
        //SeedTechniquesIfEmpty();
    }

    public void DeleteSave(OneSave value)
    {
        // Закриваємо всі підключення до БД (якщо відкриті)
        CloseDatabaseConnections();

        // Якщо файл БД існує - видаляємо
        if (File.Exists(value._fileName))
        {
            File.Delete(value._fileName);
        }
        InitializeDatabase();
    }

    private void CloseDatabaseConnections()
    {
        // Закриваємо старе підключення, якщо воно існує
        if (_conn != null)
        {
            if (_conn.State == ConnectionState.Open)
                _conn.Close();
            _conn.Dispose();
        }
        _conn = new SQLiteConnection();
        // Примусовий збір сміття для звільнення файлу
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public bool IsTableExistsSafe(string tableName = "Player")
    {
        string sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
        using (var command = new SQLiteCommand(sql, _conn))
        {
            command.Parameters.AddWithValue("@name", tableName);
            long count = (long)command.ExecuteScalar();
            return count > 0;
        }
    }

    // =========================================================
    // Read
    // =========================================================

    public Player? GetPlayer()
    {
        using var cmd  = new SQLiteCommand("SELECT * FROM Player LIMIT 1", _conn);
        using var rdr  = cmd.ExecuteReader();
        return rdr.Read() ? ReadPlayer(rdr) : null;
    }

    public List<Npc> GetAllNpcs()
    {
        var list = new List<Npc>();
        using var cmd  = new SQLiteCommand("SELECT * FROM Npcs ORDER BY Id", _conn);
        using var rdr  = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(ReadNpc(rdr));
        return list;
    }

    public List<Resource> GetAllResources()
    {
        var list = new List<Resource>();
        using var cmd  = new SQLiteCommand("SELECT * FROM Resources ORDER BY Id", _conn);
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
        using var cmd  = new SQLiteCommand("SELECT * FROM Quests ORDER BY Id", _conn);
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
        using var cmd  = new SQLiteCommand("SELECT * FROM Locations ORDER BY Id", _conn);
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
                DangerLevel     = rdr.GetDouble(rdr.GetOrdinal("DangerLevel")),
                IsExplored      = rdr.GetInt32(rdr.GetOrdinal("IsExplored")) == 1,
                Status          = Enum.TryParse<LocationStatus>(GetStringOrDefault(rdr, "Status", "Dangerous"), out var ls) ? ls : LocationStatus.Dangerous,
                MonsterTypeName = GetStringOrDefault(rdr, "MonsterTypeName"),
                MapState        = Enum.TryParse<MapState>(GetStringOrDefault(rdr, "MapState", "Current"), out var ms) ? ms : MapState.Current,
            });
        }
        return list;
    }

    // =========================================================
    // Write
    // =========================================================

    public void SavePlayer(Player p)
    {
        using var cmd  = new SQLiteCommand(
            "UPDATE Player SET FaithPoints=@fp,AltarLevel=@al,CurrentDay=@cd,BarrierSize=@bs,TerritoryControl=@tc,PlayerActionsToday=@pa WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@fp", p.FaithPoints);
        cmd.Parameters.AddWithValue("@al", p.AltarLevel);
        cmd.Parameters.AddWithValue("@cd", p.CurrentDay);
        cmd.Parameters.AddWithValue("@bs", p.BarrierSize);
        cmd.Parameters.AddWithValue("@tc", p.TerritoryControl);
        cmd.Parameters.AddWithValue("@pa", p.PlayerActionsToday);
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveNpc(Npc n)
    {
        using var cmd = new SQLiteCommand(@"
        UPDATE Npcs SET
            Health=@hp, Faith=@fa, Stamina=@st, Chakra=@ck,
            Fear=@fr, Trust=@tr, Initiative=@in, CombatInitiative=@ci, FollowerLevel=@fl,
            CharTraits=@ct, Specializations=@sp, Emotions=@em,
            Goal=@gl, Dream=@dr, Desire=@de,
            Needs=@nd,
            StatsBaseValues=@sbv, StatsDeviations=@sdv,  -- НОВЫЕ ПОЛЯ
            ActiveTask=@at, TaskDaysLeft=@tdl, TaskRewardResId=@trr, TaskRewardAmt=@tra,
            Memory=@me
        WHERE Id=@id", _conn);

        cmd.Parameters.AddWithValue("@hp",  n.Health);
        cmd.Parameters.AddWithValue("@fa",  n.Faith);
        cmd.Parameters.AddWithValue("@st",  n.Stamina);
        cmd.Parameters.AddWithValue("@ck",  n.Chakra);
        cmd.Parameters.AddWithValue("@fr",  n.Fear);
        cmd.Parameters.AddWithValue("@tr",  n.Trust);
        cmd.Parameters.AddWithValue("@in",  n.Initiative);
        cmd.Parameters.AddWithValue("@ci",  n.CombatInitiative);
        cmd.Parameters.AddWithValue("@fl",  n.FollowerLevel);
        cmd.Parameters.AddWithValue("@ct",  JsonSerializer.Serialize(n.CharTraits.Select(c => c.ToString()).ToList(), JsonOpts));
        cmd.Parameters.AddWithValue("@sp",  JsonSerializer.Serialize(n.Specializations, JsonOpts));
        cmd.Parameters.AddWithValue("@em",  JsonSerializer.Serialize(n.Emotions, JsonOpts));
        cmd.Parameters.AddWithValue("@gl",  n.Goal);
        cmd.Parameters.AddWithValue("@dr",  n.Dream);
        cmd.Parameters.AddWithValue("@de",  n.Desire);
        cmd.Parameters.AddWithValue("@nd",  JsonSerializer.Serialize(n.Needs, JsonOpts));
        cmd.Parameters.AddWithValue("@at",  n.ActiveTask);
        cmd.Parameters.AddWithValue("@tdl", n.TaskDaysLeft);
        cmd.Parameters.AddWithValue("@trr", n.TaskRewardResId);
        cmd.Parameters.AddWithValue("@tra", n.TaskRewardAmt);
        cmd.Parameters.AddWithValue("@me",  JsonSerializer.Serialize(n.Memory, JsonOpts));
        cmd.Parameters.AddWithValue("@id",  n.Id);

        // === НОВЫЕ параметры для Statistics ===
        cmd.Parameters.AddWithValue("@sbv", JsonSerializer.Serialize(n.Stats.GetBaseValuesArray(), JsonOpts));
        cmd.Parameters.AddWithValue("@sdv", JsonSerializer.Serialize(n.Stats.GetDeviationsArray(), JsonOpts));

        // Сохранить модификаторы в отдельную таблицу
        SaveModifiersForNpc(n.Id, n.Stats);

        cmd.ExecuteNonQuery();
    }

    // Сохранение модификаторов NPC
    private void SaveModifiersForNpc(int npcId, Statistics stats)
    {
        // Удалить старые модификаторы
        ExecuteNQ($"DELETE FROM NpcModifiers WHERE NpcId = {npcId}");

        foreach (var stat in stats.AllStats)
        {
            foreach (var mod in stat.GetModifiersByType<PermanentModifier>())
            {
                ExecuteNQ($@"
                INSERT INTO NpcModifiers (NpcId, StatId, ModifierId, Name, Source, Type, Value, ModifierClass, IsActive)
                VALUES ({npcId}, '{stat.Id}', '{mod.Id}', '{mod.Name}', '{mod.Source}', 
                       '{(int)mod.Type}', {mod.Value}, 'Permanent', {(mod.IsActive() ? 1 : 0)})");
            }

            foreach (var mod in stat.GetModifiersByType<IndependentModifier>())
            {
                ExecuteNQ($@"
                INSERT INTO NpcModifiers (NpcId, StatId, ModifierId, Name, Source, Type, Value, ModifierClass, 
                       TimeUnit, Duration, Remaining)
                VALUES ({npcId}, '{stat.Id}', '{mod.Id}', '{mod.Name}', '{mod.Source}', 
                       '{(int)mod.Type}', {mod.Value}, 'Independent', 
                       '{(int)mod.TimeUnit}', {mod.Duration}, {mod.Remaining})");
            }

            // DependentModifier требует сохранения условия (сложнее, можно пока пропустить)
        }
    }

    // Загрузка модификаторов NPC
    private Statistics LoadModifiersForNpc(int npcId, Statistics stats)
    {
        using var cmd = new SQLiteCommand($"SELECT * FROM NpcModifiers WHERE NpcId = {npcId}", _conn);
        using var rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            string statId = rdr.GetString(rdr.GetOrdinal("StatId"));
            var stat = stats.GetById(statId);
            if (stat == null) continue;

            string modifierClass = rdr.GetString(rdr.GetOrdinal("ModifierClass"));

            if (modifierClass == "Permanent")
            {
                var mod = new PermanentModifier(
                    rdr.GetString(rdr.GetOrdinal("ModifierId")),
                    rdr.GetString(rdr.GetOrdinal("Name")),
                    rdr.GetString(rdr.GetOrdinal("Source")),
                    (ModifierType)rdr.GetInt32(rdr.GetOrdinal("Type")),
                    rdr.GetDouble(rdr.GetOrdinal("Value"))
                );
                if (rdr.GetInt32(rdr.GetOrdinal("IsActive")) == 0)
                    mod.Deactivate();
                stat.AddModifier(mod);
            }
            else if (modifierClass == "Independent")
            {
                var mod = new IndependentModifier(
                    rdr.GetString(rdr.GetOrdinal("ModifierId")),
                    rdr.GetString(rdr.GetOrdinal("Name")),
                    rdr.GetString(rdr.GetOrdinal("Source")),
                    (ModifierType)rdr.GetInt32(rdr.GetOrdinal("Type")),
                    rdr.GetDouble(rdr.GetOrdinal("Value")),
                    (TimeUnit)rdr.GetInt32(rdr.GetOrdinal("TimeUnit")),
                    rdr.GetInt32(rdr.GetOrdinal("Duration"))
                );
                // Восстановить оставшееся время
                // (нужно добавить поле Remaining в таблицу)
                stat.AddModifier(mod);
            }
        }

        return stats;
    }

    public void SaveResource(Resource r)
    {
        using var cmd  = new SQLiteCommand("UPDATE Resources SET Amount=@a WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@a",  r.Amount);
        cmd.Parameters.AddWithValue("@id", r.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveQuest(Quest q)
    {
        using var cmd  = new SQLiteCommand(@"
            UPDATE Quests SET
                Status=@st, AssignedNpcId=@an, DaysRemaining=@dr
            WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@st", q.Status.ToString());
        cmd.Parameters.AddWithValue("@an", q.AssignedNpcId);
        cmd.Parameters.AddWithValue("@dr", q.DaysRemaining);
        cmd.Parameters.AddWithValue("@id", q.Id);
        cmd.ExecuteNonQuery();
    }

    public void InsertQuest(Quest q)
    {
        using var cmd  = new SQLiteCommand(@"
            INSERT INTO Quests
              (Title,Description,Source,Status,AssignedNpcId,
               DaysRequired,DaysRemaining,RewardResourceId,RewardAmount,FaithCost)
            VALUES (@ti,@de,@so,@st,@an,@dq,@dr,@rr,@ra,@fc)", _conn);
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
        q.Id = (int)_conn.LastInsertRowId;
    }

    private void SeedTechniquesIfEmpty()
    {
        var count = (long)(ExecuteScalar("SELECT COUNT(*) FROM Techniques") ?? 0L);
        if (count > 0) return;

        var seeds = new[]
        {
            // (Name, Desc, AltarLevel, TechLevel, TechType, FaithCost, ChakraCost, StaminaCost)
            ("Удар силы",         "Базовый физический удар.",              1, TechniqueLevel.Genin,       TechniqueType.Physical, 5.0,  5.0,  10.0),
            ("Волна чакры",       "Слабый выброс энергии.",                1, TechniqueLevel.Genin,       TechniqueType.Energy,   5.0, 10.0,   5.0),
            ("Острый ум",         "Кратковременное усиление концентрации.",1, TechniqueLevel.Genin,       TechniqueType.Mental,   5.0,  8.0,   3.0),
            ("Огненный шар",      "Техника огненной чакры.",               2, TechniqueLevel.EliteGenin,  TechniqueType.Energy,  10.0, 20.0,  10.0),
            ("Железный кулак",    "Усиленный физический удар.",            2, TechniqueLevel.EliteGenin,  TechniqueType.Physical,10.0, 15.0,  20.0),
            ("Иллюзия страха",    "Ментальная атака, вызывает панику.",    3, TechniqueLevel.Chunin,      TechniqueType.Mental,  20.0, 25.0,  15.0),
            ("Водяной хлыст",     "Техника воды — дальняя атака.",         3, TechniqueLevel.Chunin,      TechniqueType.Energy,  20.0, 30.0,  15.0),
            ("Каменная кожа",     "Защитное физическое укрепление тела.",  4, TechniqueLevel.EliteChunin, TechniqueType.Physical,30.0, 35.0,  30.0),
            ("Молниеносный удар", "Сверхбыстрая атака с разряды молнии.", 4, TechniqueLevel.EliteChunin, TechniqueType.Physical,30.0, 30.0,  25.0),
            ("Взрыв чакры",       "Мощный выброс чакры во все стороны.",  5, TechniqueLevel.Jonin,       TechniqueType.Energy,  50.0, 60.0,  30.0),
            ("Контроль разума",   "Высшая ментальная техника подавления.", 5, TechniqueLevel.Jonin,       TechniqueType.Mental,  50.0, 50.0,  25.0),
            ("Совершенная форма", "Тело достигает пика физических возм.",  6, TechniqueLevel.EliteJonin,  TechniqueType.Physical,70.0, 60.0,  50.0),
            ("Клон чакры",        "Создание нескольких энергетических копий.",7,TechniqueLevel.Anbu,      TechniqueType.Energy, 100.0, 80.0,  40.0),
            ("Природная сила",    "Слияние с природной энергией.",         8, TechniqueLevel.Sannin,      TechniqueType.Energy, 150.0,100.0,  60.0),
            ("Бездна",            "Абсолютная ментальная пустота.",        10, TechniqueLevel.Kage,       TechniqueType.Mental, 300.0,150.0, 100.0),
        };

        foreach (var (nm, ds, al, tl, tt, fc, cc, sc) in seeds)
        {
            using var cmd = new SQLiteCommand(@"
                INSERT INTO Techniques (Name,Description,AltarLevel,TechLevel,TechType,FaithCost,ChakraCost,StaminaCost,RequiredStats)
                VALUES (@nm,@ds,@al,@tl,@tt,@fc,@cc,@sc,'{}')", _conn);
            cmd.Parameters.AddWithValue("@nm", nm);
            cmd.Parameters.AddWithValue("@ds", ds);
            cmd.Parameters.AddWithValue("@al", al);
            cmd.Parameters.AddWithValue("@tl", tl.ToString());
            cmd.Parameters.AddWithValue("@tt", tt.ToString());
            cmd.Parameters.AddWithValue("@fc", fc);
            cmd.Parameters.AddWithValue("@cc", cc);
            cmd.Parameters.AddWithValue("@sc", sc);
            cmd.ExecuteNonQuery();
        }
    }

    private static Technique ReadTechnique(SQLiteDataReader rdr) => new()
    {
        Id          = rdr.GetInt32(rdr.GetOrdinal("Id")),
        Name        = rdr.GetString(rdr.GetOrdinal("Name")),
        Description = GetStringOrDefault(rdr, "Description"),
        AltarLevel  = rdr.GetInt32(rdr.GetOrdinal("AltarLevel")),
        TechLevel   = Enum.TryParse<TechniqueLevel>(GetStringOrDefault(rdr, "TechLevel", "Genin"), out var tl) ? tl : TechniqueLevel.Genin,
        TechType    = Enum.TryParse<TechniqueType>(GetStringOrDefault(rdr, "TechType", "Energy"), out var tt) ? tt : TechniqueType.Energy,
        FaithCost   = GetDoubleOrDefault(rdr, "FaithCost"),
        ChakraCost  = GetDoubleOrDefault(rdr, "ChakraCost"),
        StaminaCost = GetDoubleOrDefault(rdr, "StaminaCost"),
        RequiredStats = DeserializeOrDefault<Dictionary<int, double>>(rdr, "RequiredStats") ?? new(),
    };

    // =========================================================
    // Private helpers
    // =========================================================

    private static Player ReadPlayer(SQLiteDataReader rdr) => new()
    {
        Id                   = rdr.GetInt32(rdr.GetOrdinal("Id")),
        Name                 = rdr.GetString(rdr.GetOrdinal("Name")),
        FaithPoints          = rdr.GetDouble(rdr.GetOrdinal("FaithPoints")),
        AltarLevel           = rdr.GetInt32(rdr.GetOrdinal("AltarLevel")),
        CurrentDay           = rdr.GetInt32(rdr.GetOrdinal("CurrentDay")),
        BarrierSize          = GetDoubleOrDefault(rdr, "BarrierSize"),
        TerritoryControl     = GetIntOrDefault(rdr, "TerritoryControl"),
        PlayerActionsToday   = GetIntOrDefault(rdr, "PlayerActionsToday"),
    };

    private Npc ReadNpc(SQLiteDataReader rdr)
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
            Initiative       = GetDoubleOrDefault(rdr, "Initiative", 50),
            CombatInitiative = GetDoubleOrDefault(rdr, "CombatInitiative", 50),
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

        // === ДОБАВИТЬ новую загрузку Statistics ===
        var baseValues = DeserializeOrDefault<int[]>(rdr, "StatsBaseValues");
        var deviations = DeserializeOrDefault<int[]>(rdr, "StatsDeviations");

        if (baseValues != null && deviations != null)
        {
            npc.Stats.LoadFromArrays(baseValues, deviations);
        }

        // Также загрузить модификаторы (отдельная таблица)
        npc.Stats = LoadModifiersForNpc(npc.Id, npc.Stats);

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

    private void OpenConnection(string ThisConn)
    {
        if (ThisConn == "")
        {
            // Закриваємо старе підключення, якщо воно існує
            if (_conn != null)
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
                _conn.Dispose();
            }
            _conn = new SQLiteConnection();
            return;
        }
        // Перевіряємо, чи змінився рядок підключення
        bool connectionStringChanged = _conn != null && _conn.ConnectionString != ThisConn;

        // Якщо підключення не існує або рядок змінився
        if (_conn == null || connectionStringChanged)
        {
            // Закриваємо старе підключення, якщо воно існує
            if (_conn != null)
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
                _conn.Dispose();
            }

            // Створюємо та відкриваємо нове підключення
            _conn = new SQLiteConnection(ThisConn);
            _conn.Open();
        }
        // Якщо рядок не змінився, але підключення закрите
        else if (_conn.State != ConnectionState.Open)
        {
            _conn.Open();
        }
        // Інакше - підключення вже відкрите з правильним рядком - нічого не робимо
    }

    private static void ExecuteNQ(string sql)
    {
        using var cmd = new SQLiteCommand(sql, _conn);
        cmd.ExecuteNonQuery();
    }

    private static object? ExecuteScalar(string sql)
    {
        using var cmd = new SQLiteCommand(sql, _conn);
        return cmd.ExecuteScalar();
    }

    // ── NPC generation helpers ──────────────────────────────────────────────

    /// <summary>
    /// Генерация характеристик NPC для новой модели Statistics
    /// </summary>
    private static Statistics GenerateStatistics(string profession, Random rnd)
    {
        // Создаём новый контейнер статистик с базовым значением 100
        var stats = new Statistics(defaultBaseValue: 100);

        // === 1. Базовые значения (70-120 для взрослого человека) ===
        // Физические характеристики (10 шт.) - индексы 0-9 в AllStats
        for (int i = 0; i < 10; i++)
        {
            int baseValue = rnd.Next(70, 121);  // 70-120
            stats.AllStats[i].BaseValue = baseValue;
        }

        // Ментальные характеристики (12 шт.) - индексы 10-21 в AllStats
        for (int i = 10; i < 22; i++)
        {
            int baseValue = rnd.Next(70, 121);  // 70-120
            stats.AllStats[i].BaseValue = baseValue;
        }

        // Энергетические характеристики (8 шт.) - индексы 22-29 в AllStats
        // По концепции: база ВСЕГДА 100, отклонение от -100 до 100
        for (int i = 22; i < 30; i++)
        {
            stats.AllStats[i].BaseValue = 100;
            // Начальное отклонение для энергетических (0-50)
            int deviation = rnd.Next(0, 51);
            stats.AllStats[i].SetDeviation(deviation);
        }

        // === 2. Бонусы от профессии ===
        // Маппинг: индексы характеристик из старой системы (1-30) -> новая система
        // Физические (1-10): Endurance=1, Toughness=2, Strength=3, RecoveryPhys=4, 
        //   Reflexes=5, Agility=6, Adaptation=7, Regeneration=8, Sensorics=9, Longevity=10
        // Ментальные (11-22): Focus=11, Memory=12, Logic=13, Deduction=14, Intelligence=15,
        //   Will=16, Learning=17, Flexibility=18, Intuition=19, SocialIntel=20, Creativity=21, Mathematics=22
        // Энергетические (23-30): EnergyReserve=23, EnergyRecovery=24, Control=25,
        //   Concentration=26, Output=27, Precision=28, EnergyResist=29, EnergySense=30

        var professionBonuses = profession switch
        {
            "Механик" => new[] { 1, 2, 3, 7, 11, 12, 26 },      // Выносливость, Стойкость, Сила, Адаптация, Фокус, Память, Концентрация
            "Медик" => new[] { 2, 4, 5, 12, 13, 14, 17, 28 }, // Стойкость, Восст.(физ), Рефлексы, Память, Логика, Дедукция, Обучение, Тонкость
            "Охотник" => new[] { 1, 2, 3, 7, 9, 17, 19, 29 },   // Выносливость, Стойкость, Сила, Адаптация, Сенсорика, Обучение, Интуиция, Устойчивость(энерг)
            "Инженер" => new[] { 2, 6, 11, 12, 15, 18, 20, 25 }, // Стойкость, Ловкость, Фокус, Память, Интеллект, Гибкость, Соц.интеллект, Контроль
            _ => new[] { 1, 3, 6, 12, 17, 23 }          // Выносливость, Сила, Ловкость, Память, Обучение, Запас энергии
        };

        // Применяем бонусы профессии (увеличиваем базовые значения)
        int bonusAmount = rnd.Next(28, 46);  // +28..+45 к базе

        foreach (var statIndex in professionBonuses)
        {
            // Конвертируем индекс из старой системы (1-30) в индекс списка AllStats (0-29)
            int listIndex = statIndex - 1;
            if (listIndex >= 0 && listIndex < stats.AllStats.Count)
            {
                int newBaseValue = stats.AllStats[listIndex].BaseValue + bonusAmount;
                stats.AllStats[listIndex].BaseValue = Math.Min(150, newBaseValue); // Ограничиваем 150
            }
        }

        // === 3. Случайные отклонения (вариативность) ===
        // Для физических и ментальных: небольшое отклонение -15..+15
        for (int i = 0; i < 22; i++)  // первые 22 = физические + ментальные
        {
            int deviation = rnd.Next(-15, 16);
            stats.AllStats[i].AddDeviation(deviation);
        }

        // Для энергетических: дополнительное отклонение 0..30 (сверх начального)
        for (int i = 22; i < 30; i++)
        {
            int extraDeviation = rnd.Next(0, 31);
            stats.AllStats[i].AddDeviation(extraDeviation);
            // Ограничение уже встроено в свойство Deviation (Energy: -100..100)
        }

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

    // =========================================================
    // Actions System
    // =========================================================

    public List<PlayerActionCategory> GetPlayerActionCategories()
    {
        var categories = new List<PlayerActionCategory>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, Name, DisplayOrder, IsActive FROM ActionCategories WHERE IsActive = 1 ORDER BY DisplayOrder", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new PlayerActionCategory
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DisplayOrder = reader.GetInt32(2),
                    IsActive = reader.GetBoolean(3)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerActionCategories error: {ex.Message}"); }
        return categories;
    }

    public List<PlayerActionDb> GetAllPlayerActionsDb()
    {
        var actions = new List<PlayerActionDb>();
        try
        {
            using var cmd = new SQLiteCommand(@"SELECT Id, CategoryId, ActionKey, DisplayName, Description, 
            RequiresTarget, RequiresResource, RequiresQuest, ConsumesAction, ExecutionOrder, IsActive 
            FROM GameActions WHERE IsActive = 1 ORDER BY ExecutionOrder", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                actions.Add(new PlayerActionDb
                {
                    Id = reader.GetInt32(0),
                    CategoryId = reader.GetInt32(1),
                    ActionKey = reader.GetString(2),
                    DisplayName = reader.GetString(3),
                    Description = reader.GetString(4),
                    RequiresTarget = reader.GetBoolean(5),
                    RequiresResource = reader.GetBoolean(6),
                    RequiresQuest = reader.GetBoolean(7),
                    ConsumesAction = reader.GetBoolean(8),
                    ExecutionOrder = reader.GetInt32(9),
                    IsActive = reader.GetBoolean(10)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetAllPlayerActionsDb error: {ex.Message}"); }
        return actions;
    }

    public List<PlayerActionCondition> GetPlayerActionConditions(int actionId)
    {
        var conditions = new List<PlayerActionCondition>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, ActionId, ConditionType, Operator, Value, ErrorMessage FROM ActionConditions WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                conditions.Add(new PlayerActionCondition
                {
                    Id = reader.GetInt32(0),
                    ActionId = reader.GetInt32(1),
                    ConditionType = reader.GetString(2),
                    Operator = reader.GetString(3),
                    Value = reader.GetString(4),
                    ErrorMessage = reader.GetString(5)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerActionConditions error: {ex.Message}"); }
        return conditions;
    }

    public List<PlayerActionEffect> GetPlayerActionEffects(int actionId)
    {
        var effects = new List<PlayerActionEffect>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, ActionId, EffectType, Target, Value, Formula FROM ActionEffects WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                effects.Add(new PlayerActionEffect
                {
                    Id = reader.GetInt32(0),
                    ActionId = reader.GetInt32(1),
                    EffectType = reader.GetString(2),
                    Target = reader.GetString(3),
                    Value = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                    Formula = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerActionEffects error: {ex.Message}"); }
        return effects;
    }

    public List<PlayerActionResourceRequirement> GetPlayerActionResourceRequirements(int actionId)
    {
        var requirements = new List<PlayerActionResourceRequirement>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, ActionId, ResourceName, Amount FROM ActionResourceRequirements WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                requirements.Add(new PlayerActionResourceRequirement
                {
                    Id = reader.GetInt32(0),
                    ActionId = reader.GetInt32(1),
                    ResourceName = reader.GetString(2),
                    Amount = reader.GetDouble(3)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerActionResourceRequirements error: {ex.Message}"); }
        return requirements;
    }


    // =========================================================
    // Actions System - ДОДАТИ ЦІ МЕТОДИ
    // =========================================================

    public List<GameActionDb> GetAllGameActions()
    {
        var actions = new List<GameActionDb>();
        try
        {
            using var cmd = new SQLiteCommand(@"SELECT Id, CategoryId, ActionKey, DisplayName, Description, 
            RequiresTarget, RequiresResource, RequiresQuest, ConsumesAction, ExecutionOrder, IsActive 
            FROM GameActions WHERE IsActive = 1 ORDER BY ExecutionOrder", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                actions.Add(new GameActionDb
                {
                    Id = reader.GetInt32(0),
                    CategoryId = reader.GetInt32(1),
                    ActionKey = reader.GetString(2),
                    DisplayName = reader.GetString(3),
                    Description = reader.GetString(4),
                    RequiresTarget = reader.GetBoolean(5),
                    RequiresResource = reader.GetBoolean(6),
                    RequiresQuest = reader.GetBoolean(7),
                    ConsumesAction = reader.GetBoolean(8),
                    ExecutionOrder = reader.GetInt32(9),
                    IsActive = reader.GetBoolean(10)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetAllGameActions error: {ex.Message}"); }
        return actions;
    }

    public List<ActionConditionDb> GetActionConditions(int actionId)
    {
        var conditions = new List<ActionConditionDb>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, ActionId, ConditionType, Operator, Value, ErrorMessage FROM ActionConditions WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                conditions.Add(new ActionConditionDb
                {
                    Id = reader.GetInt32(0),
                    ActionId = reader.GetInt32(1),
                    ConditionType = reader.GetString(2),
                    Operator = reader.GetString(3),
                    Value = reader.GetString(4),
                    ErrorMessage = reader.GetString(5)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetActionConditions error: {ex.Message}"); }
        return conditions;
    }

    public List<ActionEffectDb> GetActionEffects(int actionId)
    {
        var effects = new List<ActionEffectDb>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, ActionId, EffectType, Target, Value, Formula FROM ActionEffects WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                effects.Add(new ActionEffectDb
                {
                    Id = reader.GetInt32(0),
                    ActionId = reader.GetInt32(1),
                    EffectType = reader.GetString(2),
                    Target = reader.GetString(3),
                    Value = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                    Formula = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetActionEffects error: {ex.Message}"); }
        return effects;
    }

    public List<ActionResourceRequirementDb> GetActionResourceRequirements(int actionId)
    {
        var requirements = new List<ActionResourceRequirementDb>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, ActionId, ResourceName, Amount FROM ActionResourceRequirements WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                requirements.Add(new ActionResourceRequirementDb
                {
                    Id = reader.GetInt32(0),
                    ActionId = reader.GetInt32(1),
                    ResourceName = reader.GetString(2),
                    Amount = reader.GetDouble(3)
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetActionResourceRequirements error: {ex.Message}"); }
        return requirements;
    }
}

public class OneSave
{
    public string _connectionString = "";
    public string _fileName = "";
    public bool _active = false;

    public OneSave() { }
    public OneSave(string fileName)
        { _fileName = fileName; LoadConnectionString(); }
    public OneSave(string fileName, bool active)
    { _fileName = fileName; _active = active; LoadConnectionString(); }

    public void LoadConnectionString()
    {
        _connectionString = $"Data Source={_fileName};Version=3;";
    }
}