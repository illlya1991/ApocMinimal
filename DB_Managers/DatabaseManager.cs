// DatabaseManager.cs - полный файл с изменениями

using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Windows;
using ApocMinimal.Models.StatisticsData;
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

        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataBase", "apoc_minimal_template.db");

        _templateSave = new OneSave(templatePath, true);

        string savesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
        Directory.CreateDirectory(savesPath);

        for (int i = 1; i <= _maxSavesCount; i++)
        {
            _ListSaves.Add(new OneSave(Path.Combine(savesPath, $"apocSave_{i}.db")));
        }
        _thisSave = _ListSaves[0];
        InitializeDatabase();
    }

    public List<OneSave> ListSaves { get { return _ListSaves; } }

    public OneSave ThisSave { get { return _thisSave; } set { _thisSave = value; } }

    private void InitializeDatabase()
    {
        for (int i = 0; i < _ListSaves.Count; i++)
        {
            OneSave item = _ListSaves[i];
            item._active = false;
            try
            {
                OpenConnection(item._connectionString);
                if (IsTableExistsSafe())
                {
                    object? scalarResult = ExecuteScalar("SELECT CurrentDay FROM Player Limit 1");
                    long currentDay = 0;
                    if (scalarResult != null)
                    {
                        currentDay = (long)scalarResult;
                    }
                    item._active = currentDay > 1;
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки при проверке
            }
        }
        OpenConnection("");
    }

    public bool HasAnyActiveSave()
    {
        if (_ListSaves == null) return false;
        for (int i = 0; i < _ListSaves.Count; i++)
        {
            if (_ListSaves[i]._active) return true;
        }
        return false;
    }

    // DatabaseManager.cs - исправленный фрагмент (строки 120-140)

    private void MigrateColumns()
    {
        // Исправление: разделяем строки на отдельные переменные
        string[] tablesToCheck = { "Player", "Npcs", "Locations" };

        for (int i = 0; i < tablesToCheck.Length; i++)
        {
            try
            {
                ExecuteNQ($"ALTER TABLE {tablesToCheck[i]} ADD COLUMN Statistics TEXT DEFAULT '{{}}'");
            }
            catch { }
        }

        string[] oldColumnsToRemove = { "Stats", "StatsVersion", "StatsBaseValues", "StatsDeviations" };

        for (int i = 0; i < oldColumnsToRemove.Length; i++)
        {
            try
            {
                ExecuteNQ($"ALTER TABLE Npcs DROP COLUMN {oldColumnsToRemove[i]}");
            }
            catch { }
        }

        // Исправление: правильно объявляем массив кортежей
        var columnsToAdd = new (string table, string column, string definition)[]
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
        ("Npcs",   "Hunger",           "REAL DEFAULT 0"),
        ("Npcs",   "Thirst",           "REAL DEFAULT 0"),
        ("Npcs",   "Trait",            "TEXT DEFAULT 'None'"),
        ("Npcs",   "ActiveTask",       "TEXT DEFAULT ''"),
        ("Npcs",   "TaskDaysLeft",     "INTEGER DEFAULT 0"),
        ("Npcs",   "TaskRewardResId",  "INTEGER DEFAULT 0"),
        ("Npcs",   "TaskRewardAmt",    "REAL DEFAULT 0"),
        ("Player", "CurrentDay",       "INTEGER DEFAULT 0"),
        ("Player", "PlayerActionsToday","INTEGER DEFAULT 0"),
        ("Npcs",   "CombatInitiative", "REAL DEFAULT 50"),
        ("Locations","Status",         "TEXT DEFAULT 'Dangerous'"),
        ("Locations","MonsterTypeName","TEXT DEFAULT ''"),
        ("Locations","MapState",       "TEXT DEFAULT 'Current'"),
        };

        for (int i = 0; i < columnsToAdd.Length; i++)
        {
            try
            {
                ExecuteNQ($"ALTER TABLE {columnsToAdd[i].table} ADD COLUMN {columnsToAdd[i].column} {columnsToAdd[i].definition}");
            }
            catch { }
        }

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

        using (var cmd = new SQLiteCommand(
            "INSERT INTO Player (Name,FaithPoints,AltarLevel,CurrentDay,BarrierSize,TerritoryControl) " +
            "VALUES (@n,@fp,@al,@cd,@bs,@tc)", _conn))
        {
            cmd.Parameters.AddWithValue("@n", "Божество");
            cmd.Parameters.AddWithValue("@fp", 0.0);
            cmd.Parameters.AddWithValue("@al", 1);
            cmd.Parameters.AddWithValue("@cd", 1);
            cmd.Parameters.AddWithValue("@bs", 0.0);
            cmd.Parameters.AddWithValue("@tc", 0);
            cmd.ExecuteNonQuery();
        }

        var rnd = new Random(42);

        string[][] npcsData = new string[][]
        {
            new string[] { "Алексей", "32", "Male", "Механик", "90", "40", "Leader" },
            new string[] { "Мария",   "27", "Female", "Медик",   "85", "60", "None" },
            new string[] { "Иван",    "45", "Male", "Охотник",  "95", "30", "Coward" },
            new string[] { "Анна",    "23", "Female", "Инженер", "80", "55", "Loner" },
        };

        for (int i = 0; i < npcsData.Length; i++)
        {
            string[] data = npcsData[i];
            Npc npc = new Npc();
            npc.Name = data[0];
            npc.Age = int.Parse(data[1]);
            npc.Gender = data[2] == "Male" ? Gender.Male : Gender.Female;
            npc.Profession = data[3];
            npc.Health = double.Parse(data[4]);
            npc.Faith = double.Parse(data[5]);
            npc.Stamina = 100;
            npc.Chakra = rnd.Next(30, 70);
            npc.Fear = rnd.Next(5, 30);
            npc.Trust = rnd.Next(40, 70);
            npc.Initiative = rnd.Next(35, 75);
            npc.CombatInitiative = rnd.Next(30, 80);
            npc.Trait = ParseNpcTrait(data[6]);
            npc.FollowerLevel = 0;
            npc.Description = NpcDescriptions.All[rnd.Next(NpcDescriptions.All.Length)];
            npc.Goal = NpcGoals.Goals[rnd.Next(NpcGoals.Goals.Length)];
            npc.Dream = NpcGoals.Dreams[rnd.Next(NpcGoals.Dreams.Length)];
            npc.Desire = NpcGoals.Desires[rnd.Next(NpcGoals.Desires.Length)];
            npc.Stats = GenerateStatistics(npc.Profession, rnd);

            npc.CharTraits = GenerateCharacterTraits(rnd);
            npc.Emotions = GenerateEmotions(rnd);
            npc.Needs = NeedSystem.InitialiseNeeds(npc, rnd);
            npc.Specializations = GenerateSpecializations(npc.Profession, rnd);

            InsertNpc(npc);
        }

        string[][] resourcesData = new string[][]
        {
            new string[] { "Еда",         "50", "Базовый" },
            new string[] { "Вода",        "40", "Базовый" },
            new string[] { "Медикаменты", "15", "Медицинский" },
            new string[] { "Дерево",      "30", "Материал" },
            new string[] { "Инструменты", "10", "Материал" },
        };

        for (int i = 0; i < resourcesData.Length; i++)
        {
            using var cmd = new SQLiteCommand(
                "INSERT INTO Resources (Name,Amount,Category) VALUES (@n,@a,@c)", _conn);
            cmd.Parameters.AddWithValue("@n", resourcesData[i][0]);
            cmd.Parameters.AddWithValue("@a", double.Parse(resourcesData[i][1]));
            cmd.Parameters.AddWithValue("@c", resourcesData[i][2]);
            cmd.ExecuteNonQuery();
        }

        SeedLocations(rnd);
        SeedQuests(rnd);
    }

    private NpcTrait ParseNpcTrait(string value)
    {
        if (value == "Leader") return NpcTrait.Leader;
        if (value == "Coward") return NpcTrait.Coward;
        if (value == "Loner") return NpcTrait.Loner;
        return NpcTrait.None;
    }

    private List<CharacterTrait> GenerateCharacterTraits(Random rnd)
    {
        CharacterTrait[] allTraits = new CharacterTrait[]
        {
            CharacterTrait.Brave, CharacterTrait.Cowardly, CharacterTrait.Generous,
            CharacterTrait.Greedy, CharacterTrait.Curious, CharacterTrait.Lazy,
            CharacterTrait.Loyal, CharacterTrait.Treacherous, CharacterTrait.Empathetic,
            CharacterTrait.Paranoid
        };

        List<CharacterTrait> result = new List<CharacterTrait>();
        int firstIndex = rnd.Next(allTraits.Length);
        result.Add(allTraits[firstIndex]);

        int secondIndex;
        do
        {
            secondIndex = rnd.Next(allTraits.Length);
        } while (secondIndex == firstIndex);
        result.Add(allTraits[secondIndex]);

        return result;
    }

    private List<Emotion> GenerateEmotions(Random rnd)
    {
        string[] allEmotions = EmotionNames.All;

        List<string> selectedNames = new List<string>();
        for (int i = 0; i < allEmotions.Length && selectedNames.Count < 3; i++)
        {
            if (rnd.NextDouble() < 0.3)
            {
                selectedNames.Add(allEmotions[i]);
            }
        }

        while (selectedNames.Count < 3)
        {
            string name = allEmotions[rnd.Next(allEmotions.Length)];
            if (!selectedNames.Contains(name))
                selectedNames.Add(name);
        }

        double a = rnd.Next(10, 60);
        double b = rnd.Next(10, (int)(100 - a - 10));
        double c = 100 - a - b;

        List<Emotion> result = new List<Emotion>();
        result.Add(new Emotion(selectedNames[0], a));
        result.Add(new Emotion(selectedNames[1], b));
        result.Add(new Emotion(selectedNames[2], c));

        return result;
    }

    private static void InsertNpc(Npc npc)
    {
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Npcs
              (Name,Age,Gender,Profession,Description,Health,Faith,Stamina,Chakra,
               Fear,Trust,Initiative,CombatInitiative,Trait,FollowerLevel,CharTraits,Specializations,
               Emotions,Goal,Dream,Desire,Needs,Memory,Statistics)
            VALUES
              (@nm,@ag,@gn,@pr,@ds,@hp,@fa,@st,@ck,
               @fr,@tr,@in,@ci,@tt,@fl,@ct,@sp,
               @em,@gl,@dr,@de,@nd,@me,@stat)", _conn);

        cmd.Parameters.AddWithValue("@nm", npc.Name);
        cmd.Parameters.AddWithValue("@ag", npc.Age);
        cmd.Parameters.AddWithValue("@gn", npc.Gender.ToString());
        cmd.Parameters.AddWithValue("@pr", npc.Profession);
        cmd.Parameters.AddWithValue("@ds", npc.Description);
        cmd.Parameters.AddWithValue("@hp", npc.Health);
        cmd.Parameters.AddWithValue("@fa", npc.Faith);
        cmd.Parameters.AddWithValue("@st", npc.Stamina);
        cmd.Parameters.AddWithValue("@ck", npc.Chakra);
        cmd.Parameters.AddWithValue("@fr", npc.Fear);
        cmd.Parameters.AddWithValue("@tr", npc.Trust);
        cmd.Parameters.AddWithValue("@in", npc.Initiative);
        cmd.Parameters.AddWithValue("@ci", npc.CombatInitiative);
        cmd.Parameters.AddWithValue("@tt", npc.Trait.ToString());
        cmd.Parameters.AddWithValue("@fl", npc.FollowerLevel);

        List<string> traitStrings = new List<string>();
        for (int i = 0; i < npc.CharTraits.Count; i++)
            traitStrings.Add(npc.CharTraits[i].ToString());
        cmd.Parameters.AddWithValue("@ct", JsonSerializer.Serialize(traitStrings, JsonOpts));

        cmd.Parameters.AddWithValue("@sp", JsonSerializer.Serialize(npc.Specializations, JsonOpts));
        cmd.Parameters.AddWithValue("@em", JsonSerializer.Serialize(npc.Emotions, JsonOpts));
        cmd.Parameters.AddWithValue("@gl", npc.Goal);
        cmd.Parameters.AddWithValue("@dr", npc.Dream);
        cmd.Parameters.AddWithValue("@de", npc.Desire);
        cmd.Parameters.AddWithValue("@nd", JsonSerializer.Serialize(npc.Needs, JsonOpts));
        cmd.Parameters.AddWithValue("@me", JsonSerializer.Serialize(npc.Memory, JsonOpts));
        cmd.Parameters.AddWithValue("@stat", JsonSerializer.Serialize(npc.Stats, JsonOpts));

        cmd.ExecuteNonQuery();
    }

    private static void SeedLocations(Random rnd)
    {
        long cityId = InsertLocation("Новый Харьков", LocationType.City, 0, new Dictionary<string, double>(), 10, true);
        long distId = InsertLocation("Центральный район", LocationType.District, (int)cityId, new Dictionary<string, double>(), 20, true);
        long streetId = InsertLocation("ул. Выживших", LocationType.Street, (int)distId, new Dictionary<string, double>(), 25, true);
        long bldId = InsertLocation("Жилой дом №1", LocationType.Building, (int)streetId,
            new Dictionary<string, double>() { { "Дерево", 15 }, { "Металлолом", 10 } }, 20, true);

        for (int floor = 1; floor <= 5; floor++)
        {
            long floorId = InsertLocation($"Этаж {floor}", LocationType.Floor, (int)bldId,
                new Dictionary<string, double>(), 15 + rnd.Next(0, 20), floor <= 2);

            for (int apt = 1; apt <= 4; apt++)
            {
                Dictionary<string, double> nodes = new Dictionary<string, double>();
                string[] allResources = ResourceTypes.All;

                int resCount = rnd.Next(1, 4);
                for (int r = 0; r < resCount; r++)
                {
                    string resName = allResources[rnd.Next(allResources.Length)];
                    if (!nodes.ContainsKey(resName))
                        nodes[resName] = rnd.Next(5, 30);
                }

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
        var templates = QuestTemplates.All;

        List<int> indices = new List<int>();
        for (int i = 0; i < templates.Length; i++)
            indices.Add(i);

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        for (int i = 0; i < 3 && i < indices.Count; i++)
        {
            var t = templates[indices[i]];
            using var cmd = new SQLiteCommand(@"
                INSERT INTO Quests
                  (Title,Description,Source,Status,AssignedNpcId,
                   DaysRequired,DaysRemaining,RewardResourceId,RewardAmount,FaithCost)
                VALUES (@ti,@de,@so,@st,0,@dr,@drr,@rr,@ra,@fc)", _conn);
            cmd.Parameters.AddWithValue("@ti", t.Title);
            cmd.Parameters.AddWithValue("@de", t.Desc);
            cmd.Parameters.AddWithValue("@so", "AI");
            cmd.Parameters.AddWithValue("@st", "Available");
            cmd.Parameters.AddWithValue("@dr", t.Days);
            cmd.Parameters.AddWithValue("@drr", t.Days);
            cmd.Parameters.AddWithValue("@rr", t.ResId);
            cmd.Parameters.AddWithValue("@ra", t.Reward);
            cmd.Parameters.AddWithValue("@fc", t.FaithCost);
            cmd.ExecuteNonQuery();
        }
    }

    public List<Technique> GetAllTechniques()
    {
        List<Technique> list = new List<Technique>();
        using var cmd = new SQLiteCommand("SELECT * FROM Techniques ORDER BY AltarLevel, Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
            list.Add(ReadTechnique(rdr));
        return list;
    }

    public void InsertTechnique(Technique t)
    {
        using var cmd = new SQLiteCommand(@"
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
        using var cmd = new SQLiteCommand(
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

    public void ResetDatabase()
    {
        if (!File.Exists(_templateSave._fileName))
        {
            throw new FileNotFoundException($"Файл шаблону не знайдено: {_templateSave._fileName}");
        }

        CloseDatabaseConnections();

        if (File.Exists(_thisSave._fileName))
        {
            File.Delete(_thisSave._fileName);
        }

        File.Copy(_templateSave._fileName, _thisSave._fileName);

        OpenConnection(_thisSave._connectionString);
    }

    public void DeleteSave(OneSave value)
    {
        CloseDatabaseConnections();

        if (File.Exists(value._fileName))
        {
            File.Delete(value._fileName);
        }
        InitializeDatabase();
    }

    private void CloseDatabaseConnections()
    {
        if (_conn != null)
        {
            if (_conn.State == ConnectionState.Open)
                _conn.Close();
            _conn.Dispose();
        }
        _conn = new SQLiteConnection();
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

    public Player? GetPlayer()
    {
        using var cmd = new SQLiteCommand("SELECT * FROM Player LIMIT 1", _conn);
        using var rdr = cmd.ExecuteReader();
        return rdr.Read() ? ReadPlayer(rdr) : null;
    }

    public List<Npc> GetAllNpcs()
    {
        List<Npc> list = new List<Npc>();
        using var cmd = new SQLiteCommand("SELECT * FROM Npcs ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
            list.Add(ReadNpc(rdr));
        return list;
    }

    public List<Resource> GetAllResources()
    {
        List<Resource> list = new List<Resource>();
        using var cmd = new SQLiteCommand("SELECT * FROM Resources ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            Resource res = new Resource();
            res.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
            res.Name = rdr.GetString(rdr.GetOrdinal("Name"));
            res.Amount = rdr.GetDouble(rdr.GetOrdinal("Amount"));
            res.Category = rdr.GetString(rdr.GetOrdinal("Category"));
            list.Add(res);
        }
        return list;
    }

    public List<Quest> GetAllQuests()
    {
        List<Quest> list = new List<Quest>();
        using var cmd = new SQLiteCommand("SELECT * FROM Quests ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            Quest q = new Quest();
            q.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
            q.Title = rdr.GetString(rdr.GetOrdinal("Title"));
            q.Description = rdr.GetString(rdr.GetOrdinal("Description"));

            string sourceStr = rdr.GetString(rdr.GetOrdinal("Source"));
            q.Source = (sourceStr == "Player") ? QuestSource.Player : QuestSource.AI;

            string statusStr = rdr.GetString(rdr.GetOrdinal("Status"));
            if (statusStr == "Available") q.Status = QuestStatus.Available;
            else if (statusStr == "Active") q.Status = QuestStatus.Active;
            else if (statusStr == "Completed") q.Status = QuestStatus.Completed;
            else q.Status = QuestStatus.Failed;

            q.AssignedNpcId = rdr.GetInt32(rdr.GetOrdinal("AssignedNpcId"));
            q.DaysRequired = rdr.GetInt32(rdr.GetOrdinal("DaysRequired"));
            q.DaysRemaining = rdr.GetInt32(rdr.GetOrdinal("DaysRemaining"));
            q.RewardResourceId = rdr.GetInt32(rdr.GetOrdinal("RewardResourceId"));
            q.RewardAmount = rdr.GetDouble(rdr.GetOrdinal("RewardAmount"));
            q.FaithCost = rdr.GetDouble(rdr.GetOrdinal("FaithCost"));
            list.Add(q);
        }
        return list;
    }

    public List<Location> GetAllLocations()
    {
        List<Location> list = new List<Location>();
        using var cmd = new SQLiteCommand("SELECT * FROM Locations ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            Location loc = new Location();
            loc.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
            loc.Name = rdr.GetString(rdr.GetOrdinal("Name"));

            string typeStr = rdr.GetString(rdr.GetOrdinal("Type"));
            if (typeStr == "City") loc.Type = LocationType.City;
            else if (typeStr == "District") loc.Type = LocationType.District;
            else if (typeStr == "Street") loc.Type = LocationType.Street;
            else if (typeStr == "Building") loc.Type = LocationType.Building;
            else if (typeStr == "Floor") loc.Type = LocationType.Floor;
            else loc.Type = LocationType.Apartment;

            loc.ParentId = rdr.GetInt32(rdr.GetOrdinal("ParentId"));

            string nodesJson = rdr.IsDBNull(rdr.GetOrdinal("ResourceNodes")) ? "{}"
                : rdr.GetString(rdr.GetOrdinal("ResourceNodes"));
            try
            {
                loc.ResourceNodes = JsonSerializer.Deserialize<Dictionary<string, double>>(nodesJson) ?? new Dictionary<string, double>();
            }
            catch
            {
                loc.ResourceNodes = new Dictionary<string, double>();
            }

            loc.DangerLevel = rdr.GetDouble(rdr.GetOrdinal("DangerLevel"));
            loc.IsExplored = rdr.GetInt32(rdr.GetOrdinal("IsExplored")) == 1;

            string statusStr = GetStringOrDefault(rdr, "Status", "Dangerous");
            if (statusStr == "Cleared") loc.Status = LocationStatus.Cleared;
            else loc.Status = LocationStatus.Dangerous;

            loc.MonsterTypeName = GetStringOrDefault(rdr, "MonsterTypeName");

            string mapStateStr = GetStringOrDefault(rdr, "MapState", "Current");
            if (mapStateStr == "Template") loc.MapState = MapState.Template;
            else if (mapStateStr == "ApocStart") loc.MapState = MapState.ApocStart;
            else loc.MapState = MapState.Current;

            list.Add(loc);
        }
        return list;
    }

    public void SavePlayer(Player p)
    {
        using var cmd = new SQLiteCommand(
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
            Needs=@nd, Memory=@me, Statistics=@stat
        WHERE Id=@id", _conn);

        cmd.Parameters.AddWithValue("@hp", n.Health);
        cmd.Parameters.AddWithValue("@fa", n.Faith);
        cmd.Parameters.AddWithValue("@st", n.Stamina);
        cmd.Parameters.AddWithValue("@ck", n.Chakra);
        cmd.Parameters.AddWithValue("@fr", n.Fear);
        cmd.Parameters.AddWithValue("@tr", n.Trust);
        cmd.Parameters.AddWithValue("@in", n.Initiative);
        cmd.Parameters.AddWithValue("@ci", n.CombatInitiative);
        cmd.Parameters.AddWithValue("@fl", n.FollowerLevel);

        List<string> traitStrings = new List<string>();
        for (int i = 0; i < n.CharTraits.Count; i++)
            traitStrings.Add(n.CharTraits[i].ToString());
        cmd.Parameters.AddWithValue("@ct", JsonSerializer.Serialize(traitStrings, JsonOpts));

        cmd.Parameters.AddWithValue("@sp", JsonSerializer.Serialize(n.Specializations, JsonOpts));
        cmd.Parameters.AddWithValue("@em", JsonSerializer.Serialize(n.Emotions, JsonOpts));
        cmd.Parameters.AddWithValue("@gl", n.Goal);
        cmd.Parameters.AddWithValue("@dr", n.Dream);
        cmd.Parameters.AddWithValue("@de", n.Desire);
        cmd.Parameters.AddWithValue("@nd", JsonSerializer.Serialize(n.Needs, JsonOpts));
        cmd.Parameters.AddWithValue("@me", JsonSerializer.Serialize(n.Memory, JsonOpts));
        cmd.Parameters.AddWithValue("@stat", JsonSerializer.Serialize(n.Stats, JsonOpts));
        cmd.Parameters.AddWithValue("@id", n.Id);

        SaveModifiersForNpc(n.Id, n.Stats);
        cmd.ExecuteNonQuery();
    }

    // DatabaseManager.cs - исправленный метод SaveModifiersForNpc (строка 747)

    private void SaveModifiersForNpc(int npcId, Statistics stats)
    {
        ExecuteNQ($"DELETE FROM NpcModifiers WHERE NpcId = {npcId}");

        for (int i = 0; i < stats.AllStats.Count; i++)
        {
            Characteristic stat = stats.AllStats[i];

            // Исправление: явное преобразование типов
            List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
            for (int j = 0; j < permMods.Count; j++)
            {
                PermanentModifier mod = permMods[j];
                string isActiveStr = mod.IsActiveFlag ? "1" : "0";
                ExecuteNQ($@"
                INSERT INTO NpcModifiers (NpcId, StatId, ModifierId, Name, Source, Type, Value, ModifierClass, IsActive)
                VALUES ({npcId}, '{stat.Id}', '{mod.Id}', '{mod.Name}', '{mod.Source}', 
                       {(int)mod.Type}, {mod.Value}, 'Permanent', {isActiveStr})");
            }

            List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
            for (int j = 0; j < indMods.Count; j++)
            {
                IndependentModifier mod = indMods[j];
                ExecuteNQ($@"
                INSERT INTO NpcModifiers (NpcId, StatId, ModifierId, Name, Source, Type, Value, ModifierClass, 
                       TimeUnit, Duration, Remaining)
                VALUES ({npcId}, '{stat.Id}', '{mod.Id}', '{mod.Name}', '{mod.Source}', 
                       {(int)mod.Type}, {mod.Value}, 'Independent', 
                       {(int)mod.TimeUnit}, {mod.Duration}, {mod.Remaining})");
            }
        }
    }

    // DatabaseManager.cs - исправленный метод LoadModifiersForNpc

    private Statistics LoadModifiersForNpc(int npcId, Statistics stats)
    {
        using var cmd = new SQLiteCommand($"SELECT * FROM NpcModifiers WHERE NpcId = {npcId}", _conn);
        using var rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            string statId = rdr.GetString(rdr.GetOrdinal("StatId"));
            Characteristic stat = stats.GetById(statId);
            if (stat == null) continue;

            string modifierClass = rdr.GetString(rdr.GetOrdinal("ModifierClass"));

            if (modifierClass == "Permanent")
            {
                string modId = rdr.GetString(rdr.GetOrdinal("ModifierId"));
                string modName = rdr.GetString(rdr.GetOrdinal("Name"));
                string modSource = rdr.GetString(rdr.GetOrdinal("Source"));
                ModifierType modType = (ModifierType)rdr.GetInt32(rdr.GetOrdinal("Type"));
                double modValue = rdr.GetDouble(rdr.GetOrdinal("Value"));
                int isActive = rdr.GetInt32(rdr.GetOrdinal("IsActive"));

                PermanentModifier mod = new PermanentModifier(modId, modName, modSource, modType, modValue);
                if (isActive == 0)
                    mod.Deactivate();
                stat.AddModifier(mod);
            }
            else if (modifierClass == "Independent")
            {
                string modId = rdr.GetString(rdr.GetOrdinal("ModifierId"));
                string modName = rdr.GetString(rdr.GetOrdinal("Name"));
                string modSource = rdr.GetString(rdr.GetOrdinal("Source"));
                ModifierType modType = (ModifierType)rdr.GetInt32(rdr.GetOrdinal("Type"));
                double modValue = rdr.GetDouble(rdr.GetOrdinal("Value"));
                TimeUnit timeUnit = (TimeUnit)rdr.GetInt32(rdr.GetOrdinal("TimeUnit"));
                int duration = rdr.GetInt32(rdr.GetOrdinal("Duration"));

                IndependentModifier mod = new IndependentModifier(modId, modName, modSource, modType, modValue, timeUnit, duration);
                stat.AddModifier(mod);
            }
        }

        return stats;
    }

    public void SaveResource(Resource r)
    {
        using var cmd = new SQLiteCommand("UPDATE Resources SET Amount=@a WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@a", r.Amount);
        cmd.Parameters.AddWithValue("@id", r.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveQuest(Quest q)
    {
        using var cmd = new SQLiteCommand(@"
            UPDATE Quests SET
                Status=@st, AssignedNpcId=@an, DaysRemaining=@dr
            WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@st", q.Status.ToString());
        cmd.Parameters.AddWithValue("@an", q.AssignedNpcId);
        cmd.Parameters.AddWithValue("@dr", q.DaysRemaining);
        cmd.Parameters.AddWithValue("@id", q.Id);
        cmd.ExecuteNonQuery();
    }

    private void SeedTechniquesIfEmpty()
    {
        var count = (long)(ExecuteScalar("SELECT COUNT(*) FROM Techniques") ?? 0L);
        if (count > 0) return;

        object[][] seeds = new object[][]
        {
            new object[] { "Удар силы",         "Базовый физический удар.",              1, "Genin",       "Physical", 5.0,  5.0,  10.0 },
            new object[] { "Волна чакры",       "Слабый выброс энергии.",                1, "Genin",       "Energy",   5.0, 10.0,   5.0 },
            new object[] { "Острый ум",         "Кратковременное усиление концентрации.",1, "Genin",       "Mental",   5.0,  8.0,   3.0 },
            new object[] { "Огненный шар",      "Техника огненной чакры.",               2, "EliteGenin",  "Energy",  10.0, 20.0,  10.0 },
            new object[] { "Железный кулак",    "Усиленный физический удар.",            2, "EliteGenin",  "Physical",10.0, 15.0,  20.0 },
            new object[] { "Иллюзия страха",    "Ментальная атака, вызывает панику.",    3, "Chunin",      "Mental",  20.0, 25.0,  15.0 },
            new object[] { "Водяной хлыст",     "Техника воды — дальняя атака.",         3, "Chunin",      "Energy",  20.0, 30.0,  15.0 },
            new object[] { "Каменная кожа",     "Защитное физическое укрепление тела.",  4, "EliteChunin", "Physical",30.0, 35.0,  30.0 },
            new object[] { "Молниеносный удар", "Сверхбыстрая атака с разряды молнии.", 4, "EliteChunin", "Physical",30.0, 30.0,  25.0 },
            new object[] { "Взрыв чакры",       "Мощный выброс чакры во все стороны.",   5, "Jonin",       "Energy",  50.0, 60.0,  30.0 },
            new object[] { "Контроль разума",   "Высшая ментальная техника подавления.", 5, "Jonin",       "Mental",  50.0, 50.0,  25.0 },
            new object[] { "Совершенная форма", "Тело достигает пика физических возм.",  6, "EliteJonin",  "Physical",70.0, 60.0,  50.0 },
            new object[] { "Клон чакры",        "Создание нескольких энергетических копий.",7,"Anbu",      "Energy", 100.0, 80.0,  40.0 },
            new object[] { "Природная сила",    "Слияние с природной энергией.",         8, "Sannin",      "Energy", 150.0,100.0,  60.0 },
            new object[] { "Бездна",            "Абсолютная ментальная пустота.",        10,"Kage",        "Mental", 300.0,150.0, 100.0 },
        };

        for (int i = 0; i < seeds.Length; i++)
        {
            object[] seed = seeds[i];
            using var cmd = new SQLiteCommand(@"
                INSERT INTO Techniques (Name,Description,AltarLevel,TechLevel,TechType,FaithCost,ChakraCost,StaminaCost,RequiredStats)
                VALUES (@nm,@ds,@al,@tl,@tt,@fc,@cc,@sc,'{}')", _conn);
            cmd.Parameters.AddWithValue("@nm", (string)seed[0]);
            cmd.Parameters.AddWithValue("@ds", (string)seed[1]);
            cmd.Parameters.AddWithValue("@al", (int)seed[2]);
            cmd.Parameters.AddWithValue("@tl", (string)seed[3]);
            cmd.Parameters.AddWithValue("@tt", (string)seed[4]);
            cmd.Parameters.AddWithValue("@fc", (double)seed[5]);
            cmd.Parameters.AddWithValue("@cc", (double)seed[6]);
            cmd.Parameters.AddWithValue("@sc", (double)seed[7]);
            cmd.ExecuteNonQuery();
        }
    }

    private static Technique ReadTechnique(SQLiteDataReader rdr)
    {
        Technique t = new Technique();
        t.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
        t.Name = rdr.GetString(rdr.GetOrdinal("Name"));
        t.Description = GetStringOrDefault(rdr, "Description");
        t.AltarLevel = rdr.GetInt32(rdr.GetOrdinal("AltarLevel"));

        string techLevelStr = GetStringOrDefault(rdr, "TechLevel", "Genin");
        if (techLevelStr == "Genin") t.TechLevel = TechniqueLevel.Genin;
        else if (techLevelStr == "EliteGenin") t.TechLevel = TechniqueLevel.EliteGenin;
        else if (techLevelStr == "Chunin") t.TechLevel = TechniqueLevel.Chunin;
        else if (techLevelStr == "EliteChunin") t.TechLevel = TechniqueLevel.EliteChunin;
        else if (techLevelStr == "Jonin") t.TechLevel = TechniqueLevel.Jonin;
        else if (techLevelStr == "EliteJonin") t.TechLevel = TechniqueLevel.EliteJonin;
        else if (techLevelStr == "Anbu") t.TechLevel = TechniqueLevel.Anbu;
        else if (techLevelStr == "Sannin") t.TechLevel = TechniqueLevel.Sannin;
        else if (techLevelStr == "Jinchuriki") t.TechLevel = TechniqueLevel.Jinchuriki;
        else if (techLevelStr == "Kage") t.TechLevel = TechniqueLevel.Kage;
        else t.TechLevel = TechniqueLevel.Genin;

        string techTypeStr = GetStringOrDefault(rdr, "TechType", "Energy");
        if (techTypeStr == "Physical") t.TechType = TechniqueType.Physical;
        else if (techTypeStr == "Mental") t.TechType = TechniqueType.Mental;
        else t.TechType = TechniqueType.Energy;

        t.FaithCost = GetDoubleOrDefault(rdr, "FaithCost");
        t.ChakraCost = GetDoubleOrDefault(rdr, "ChakraCost");
        t.StaminaCost = GetDoubleOrDefault(rdr, "StaminaCost");
        t.RequiredStats = DeserializeOrDefault<Dictionary<int, double>>(rdr, "RequiredStats") ?? new Dictionary<int, double>();

        return t;
    }

    private static Player ReadPlayer(SQLiteDataReader rdr)
    {
        Player p = new Player();
        p.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
        p.Name = rdr.GetString(rdr.GetOrdinal("Name"));
        p.FaithPoints = rdr.GetDouble(rdr.GetOrdinal("FaithPoints"));
        p.AltarLevel = rdr.GetInt32(rdr.GetOrdinal("AltarLevel"));
        p.CurrentDay = rdr.GetInt32(rdr.GetOrdinal("CurrentDay"));
        p.BarrierSize = GetDoubleOrDefault(rdr, "BarrierSize");
        p.TerritoryControl = GetIntOrDefault(rdr, "TerritoryControl");
        p.PlayerActionsToday = GetIntOrDefault(rdr, "PlayerActionsToday");
        return p;
    }

    private Npc ReadNpc(SQLiteDataReader rdr)
    {
        Npc npc = new Npc();
        npc.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
        npc.Name = rdr.GetString(rdr.GetOrdinal("Name"));
        npc.Age = rdr.GetInt32(rdr.GetOrdinal("Age"));

        string genderStr = GetStringOrDefault(rdr, "Gender", "Male");
        npc.Gender = (genderStr == "Female") ? Gender.Female : Gender.Male;

        npc.Profession = rdr.GetString(rdr.GetOrdinal("Profession"));
        npc.Description = GetStringOrDefault(rdr, "Description");
        npc.Health = rdr.GetDouble(rdr.GetOrdinal("Health"));
        npc.Faith = rdr.GetDouble(rdr.GetOrdinal("Faith"));
        npc.Stamina = GetDoubleOrDefault(rdr, "Stamina", 100);
        npc.Chakra = GetDoubleOrDefault(rdr, "Chakra", 50);
        npc.Fear = GetDoubleOrDefault(rdr, "Fear", 10);
        npc.Trust = GetDoubleOrDefault(rdr, "Trust", 50);
        npc.Initiative = GetDoubleOrDefault(rdr, "Initiative", 50);
        npc.CombatInitiative = GetDoubleOrDefault(rdr, "CombatInitiative", 50);

        string traitStr = GetStringOrDefault(rdr, "Trait", "None");
        if (traitStr == "Leader") npc.Trait = NpcTrait.Leader;
        else if (traitStr == "Coward") npc.Trait = NpcTrait.Coward;
        else if (traitStr == "Loner") npc.Trait = NpcTrait.Loner;
        else npc.Trait = NpcTrait.None;

        npc.FollowerLevel = GetIntOrDefault(rdr, "FollowerLevel");
        npc.Goal = GetStringOrDefault(rdr, "Goal");
        npc.Dream = GetStringOrDefault(rdr, "Dream");
        npc.Desire = GetStringOrDefault(rdr, "Desire");
        npc.ActiveTask = GetStringOrDefault(rdr, "ActiveTask");
        npc.TaskDaysLeft = GetIntOrDefault(rdr, "TaskDaysLeft");
        npc.TaskRewardResId = GetIntOrDefault(rdr, "TaskRewardResId");
        npc.TaskRewardAmt = GetDoubleOrDefault(rdr, "TaskRewardAmt");

        // Загрузка Statistics из единого поля
        string statsJson = GetStringOrDefault(rdr, "Statistics", "{}");
        try
        {
            Statistics loadedStats = JsonSerializer.Deserialize<Statistics>(statsJson);
            if (loadedStats != null)
                npc.Stats = loadedStats;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки Statistics для NPC {npc.Id}: {ex.Message}");
        }

        // Загрузка модификаторов
        npc.Stats = LoadModifiersForNpc(npc.Id, npc.Stats);

        npc.Needs = DeserializeOrDefault<List<Need>>(rdr, "Needs") ?? new List<Need>();
        npc.Emotions = DeserializeOrDefault<List<Emotion>>(rdr, "Emotions") ?? new List<Emotion>();
        npc.Specializations = DeserializeOrDefault<List<string>>(rdr, "Specializations") ?? new List<string>();
        npc.Memory = DeserializeOrDefault<List<MemoryEntry>>(rdr, "Memory") ?? new List<MemoryEntry>();

        List<string> charTraitStrings = DeserializeOrDefault<List<string>>(rdr, "CharTraits") ?? new List<string>();
        npc.CharTraits = new List<CharacterTrait>();
        for (int i = 0; i < charTraitStrings.Count; i++)
        {
            string traitStr2 = charTraitStrings[i];
            if (traitStr2 == "Brave") npc.CharTraits.Add(CharacterTrait.Brave);
            else if (traitStr2 == "Cowardly") npc.CharTraits.Add(CharacterTrait.Cowardly);
            else if (traitStr2 == "Generous") npc.CharTraits.Add(CharacterTrait.Generous);
            else if (traitStr2 == "Greedy") npc.CharTraits.Add(CharacterTrait.Greedy);
            else if (traitStr2 == "Curious") npc.CharTraits.Add(CharacterTrait.Curious);
            else if (traitStr2 == "Lazy") npc.CharTraits.Add(CharacterTrait.Lazy);
            else if (traitStr2 == "Loyal") npc.CharTraits.Add(CharacterTrait.Loyal);
            else if (traitStr2 == "Treacherous") npc.CharTraits.Add(CharacterTrait.Treacherous);
            else if (traitStr2 == "Empathetic") npc.CharTraits.Add(CharacterTrait.Empathetic);
            else if (traitStr2 == "Paranoid") npc.CharTraits.Add(CharacterTrait.Paranoid);
        }

        if (npc.Needs.Count == 0)
        {
            double hunger = GetDoubleOrDefault(rdr, "Hunger");
            double thirst = GetDoubleOrDefault(rdr, "Thirst");
            npc.Needs = NeedSystem.InitialiseNeeds(npc, new Random(npc.Id));
            NeedSystem.SatisfyNeed(npc, "Еда", Math.Max(0, 100 - hunger));
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
            if (_conn != null)
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
                _conn.Dispose();
            }
            _conn = new SQLiteConnection();
            return;
        }

        bool connectionStringChanged = _conn != null && _conn.ConnectionString != ThisConn;

        if (_conn == null || connectionStringChanged)
        {
            if (_conn != null)
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
                _conn.Dispose();
            }

            _conn = new SQLiteConnection(ThisConn);
            _conn.Open();
        }
        else if (_conn.State != ConnectionState.Open)
        {
            _conn.Open();
        }
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

    private static Statistics GenerateStatistics(string profession, Random rnd)
    {
        Statistics stats = new Statistics(100);

        for (int i = 0; i < 10; i++)
        {
            int baseValue = rnd.Next(70, 121);
            stats.AllStats[i].BaseValue = baseValue;
        }

        for (int i = 10; i < 22; i++)
        {
            int baseValue = rnd.Next(70, 121);
            stats.AllStats[i].BaseValue = baseValue;
        }

        for (int i = 22; i < 30; i++)
        {
            stats.AllStats[i].BaseValue = 100;
            int deviation = rnd.Next(0, 51);
            stats.AllStats[i].SetDeviation(deviation);
        }

        int[] professionBonuses;
        if (profession == "Механик")
            professionBonuses = new int[] { 1, 2, 3, 7, 11, 12, 26 };
        else if (profession == "Медик")
            professionBonuses = new int[] { 2, 4, 5, 12, 13, 14, 17, 28 };
        else if (profession == "Охотник")
            professionBonuses = new int[] { 1, 2, 3, 7, 9, 17, 19, 29 };
        else if (profession == "Инженер")
            professionBonuses = new int[] { 2, 6, 11, 12, 15, 18, 20, 25 };
        else
            professionBonuses = new int[] { 1, 3, 6, 12, 17, 23 };

        int bonusAmount = rnd.Next(28, 46);

        for (int i = 0; i < professionBonuses.Length; i++)
        {
            int statIndex = professionBonuses[i] - 1;
            if (statIndex >= 0 && statIndex < stats.AllStats.Count)
            {
                int newBaseValue = stats.AllStats[statIndex].BaseValue + bonusAmount;
                if (newBaseValue > 150) newBaseValue = 150;
                stats.AllStats[statIndex].BaseValue = newBaseValue;
            }
        }

        for (int i = 0; i < 22; i++)
        {
            int deviation = rnd.Next(-15, 16);
            stats.AllStats[i].AddDeviation(deviation);
        }

        for (int i = 22; i < 30; i++)
        {
            int extraDeviation = rnd.Next(0, 31);
            stats.AllStats[i].AddDeviation(extraDeviation);
        }

        return stats;
    }

    private static List<string> GenerateSpecializations(string profession, Random rnd)
    {
        string[] all = new string[]
        {
            "Стрельба", "Рукопашный бой", "Медицина", "Инженерия", "Выживание",
            "Разведка", "Лидерство", "Торговля", "Механика", "Электроника",
            "Кулинария", "Охота", "Строительство", "Химия", "Связь",
        };

        int count = rnd.Next(2, 8);
        List<string> result = new List<string>();

        List<int> indices = new List<int>();
        for (int i = 0; i < all.Length; i++)
            indices.Add(i);

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        for (int i = 0; i < count && i < indices.Count; i++)
            result.Add(all[indices[i]]);

        return result;
    }

    // DatabaseManager.cs - изменённые функции (вставьте вместо старых версий)

    public List<PlayerActionGroup> GetPlayerActionGroups()
    {
        List<PlayerActionGroup> groups = new List<PlayerActionGroup>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, Name, Icon, DisplayOrder, IsActive FROM ActionGroups WHERE IsActive = 1 ORDER BY DisplayOrder", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                PlayerActionGroup group = new PlayerActionGroup();
                group.Id = Convert.ToInt32(reader["Id"]);
                group.Name = reader["Name"]?.ToString() ?? string.Empty;
                group.Icon = reader["Icon"]?.ToString() ?? string.Empty;
                group.DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]);
                group.IsActive = Convert.ToBoolean(reader["IsActive"]);
                groups.Add(group);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerActionGroups error: {ex.Message}"); }
        return groups;
    }

    public List<PlayerGameAction> GetAllPlayerGameActions()
    {
        List<PlayerGameAction> actions = new List<PlayerGameAction>();
        try
        {
            using var cmd = new SQLiteCommand(@"SELECT Id, GroupId, ActionKey, DisplayName, Description, 
    HandlerMethod, ConsumesAction, DisplayOrder, IsActive 
    FROM GameActions WHERE IsActive = 1 ORDER BY GroupId, DisplayOrder", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                PlayerGameAction action = new PlayerGameAction();
                action.Id = Convert.ToInt32(reader["Id"]);
                action.GroupId = Convert.ToInt32(reader["GroupId"]);
                action.ActionKey = reader["ActionKey"]?.ToString() ?? string.Empty;
                action.DisplayName = reader["DisplayName"]?.ToString() ?? string.Empty;
                action.Description = reader["Description"]?.ToString() ?? string.Empty;
                action.HandlerMethod = reader["HandlerMethod"]?.ToString() ?? string.Empty;
                action.ConsumesAction = Convert.ToBoolean(reader["ConsumesAction"]);
                action.DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]);
                action.IsActive = Convert.ToBoolean(reader["IsActive"]);
                actions.Add(action);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetAllPlayerGameActions error: {ex.Message}"); }
        return actions;
    }

    public List<PlayerActionParam> GetPlayerActionParams(int actionId)
    {
        List<PlayerActionParam> parameters = new List<PlayerActionParam>();
        try
        {
            using var cmd = new SQLiteCommand(@"SELECT Id, ActionId, ParamTypeId, ParamKey, DisplayName, 
    OrderIndex, IsRequired, FilterCondition, DataSource, ValidationRules, DefaultValue 
    FROM ActionParams WHERE ActionId = @actionId ORDER BY OrderIndex", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                PlayerActionParam param = new PlayerActionParam();
                param.Id = Convert.ToInt32(reader["Id"]);
                param.ActionId = Convert.ToInt32(reader["ActionId"]);
                param.ParamTypeId = Convert.ToInt32(reader["ParamTypeId"]);
                param.ParamKey = reader["ParamKey"]?.ToString() ?? string.Empty;
                param.DisplayName = reader["DisplayName"]?.ToString() ?? string.Empty;
                param.OrderIndex = Convert.ToInt32(reader["OrderIndex"]);
                param.IsRequired = Convert.ToBoolean(reader["IsRequired"]);
                param.FilterCondition = reader["FilterCondition"]?.ToString() ?? string.Empty;
                param.DataSource = reader["DataSource"]?.ToString() ?? string.Empty;
                param.ValidationRules = reader["ValidationRules"]?.ToString() ?? string.Empty;
                param.DefaultValue = reader["DefaultValue"]?.ToString() ?? string.Empty;
                parameters.Add(param);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerActionParams error: {ex.Message}"); }
        return parameters;
    }

    public List<PlayerHandlerParamMapping> GetPlayerHandlerParamMappings(int actionId)
    {
        List<PlayerHandlerParamMapping> mappings = new List<PlayerHandlerParamMapping>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, ActionId, HandlerId, HandlerParamName, ActionParamKey FROM HandlerParamMapping WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                PlayerHandlerParamMapping mapping = new PlayerHandlerParamMapping();
                mapping.Id = Convert.ToInt32(reader["Id"]);
                mapping.ActionId = Convert.ToInt32(reader["ActionId"]);
                mapping.HandlerId = Convert.ToInt32(reader["HandlerId"]);
                mapping.HandlerParamName = reader["HandlerParamName"]?.ToString() ?? string.Empty;
                mapping.ActionParamKey = reader["ActionParamKey"]?.ToString() ?? string.Empty;
                mappings.Add(mapping);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerHandlerParamMappings error: {ex.Message}"); }
        return mappings;
    }

    public PlayerResultTemplate? GetPlayerResultTemplate(int actionId)
    {
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, ActionId, SuccessTemplate, FailTemplate, Color FROM ResultTemplates WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                PlayerResultTemplate template = new PlayerResultTemplate();
                template.Id = Convert.ToInt32(reader["Id"]);
                template.ActionId = Convert.ToInt32(reader["ActionId"]);
                template.SuccessTemplate = reader["SuccessTemplate"]?.ToString() ?? string.Empty;
                template.FailTemplate = reader["FailTemplate"]?.ToString() ?? string.Empty;
                template.Color = reader["Color"]?.ToString() ?? "normal";
                return template;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerResultTemplate error: {ex.Message}"); }
        return null;
    }

    public List<PlayerParamType> GetPlayerParamTypes()
    {
        List<PlayerParamType> types = new List<PlayerParamType>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, Name, ControlType, ValueType, IsList FROM ParamTypes", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                PlayerParamType type = new PlayerParamType();
                type.Id = Convert.ToInt32(reader["Id"]);
                type.Name = reader["Name"]?.ToString() ?? string.Empty;
                type.ControlType = reader["ControlType"]?.ToString() ?? string.Empty;
                type.ValueType = reader["ValueType"]?.ToString() ?? string.Empty;
                type.IsList = Convert.ToBoolean(reader["IsList"]);
                types.Add(type);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerParamTypes error: {ex.Message}"); }
        return types;
    }

    public Technique? GetTechniqueById(int id)
    {
        using var cmd = new SQLiteCommand("SELECT * FROM Techniques WHERE Id = @id", _conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var rdr = cmd.ExecuteReader();
        return rdr.Read() ? ReadTechnique(rdr) : null;
    }

    public List<Technique> GetTechniquesByAltarLevel(int altarLevel)
    {
        List<Technique> list = new List<Technique>();
        using var cmd = new SQLiteCommand("SELECT * FROM Techniques WHERE AltarLevel <= @level ORDER BY AltarLevel, Id", _conn);
        cmd.Parameters.AddWithValue("@level", altarLevel);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
            list.Add(ReadTechnique(rdr));
        return list;
    }

    public int GetFollowerCountAtLevel(int followerLevel)
    {
        using var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Npcs WHERE FollowerLevel = @level AND Health > 0", _conn);
        cmd.Parameters.AddWithValue("@level", followerLevel);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
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