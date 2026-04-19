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
    private const int MaxSavesCount = 3;
    private OneSave _thisSave;
    private OneSave _templateSave;
    private List<OneSave> _ListSaves;
    private static SQLiteConnection _conn = new SQLiteConnection();

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public DatabaseManager()
    {
        _ListSaves = new List<OneSave>();

        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataBase", "apoc_minimal_template.db");

        _templateSave = new OneSave(templatePath, true);

        string savesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
        Directory.CreateDirectory(savesPath);

        for (int i = 1; i <= MaxSavesCount; i++)
        {
            _ListSaves.Add(new OneSave(Path.Combine(savesPath, $"apocSave_{i}.db")));
        }
        _thisSave = _ListSaves[0];
        InitializeDatabase();
    }

    public List<OneSave> ListSaves { get { return _ListSaves; } }

    public OneSave ThisSave { get { return _thisSave; } set { _thisSave = value; } }

    public string CurrentSaveId => Path.GetFileNameWithoutExtension(_thisSave._fileName);

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
                        currentDay = (long)scalarResult;
                    item._active = currentDay > 1;
                    item._currentDay = (int)currentDay;

                    object? altarResult = ExecuteScalar("SELECT AltarLevel FROM Player Limit 1");
                    if (altarResult != null)
                        item._altarLevel = (int)(long)altarResult;

                    object? faithResult = ExecuteScalar("SELECT FaithPoints FROM Player Limit 1");
                    if (faithResult != null)
                        item._faithPoints = Convert.ToDouble(faithResult);
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

    public void OpenCurrentSave()
    {
        if (!string.IsNullOrEmpty(_thisSave._connectionString))
            OpenConnection(_thisSave._connectionString);
    }

    public void DeleteAllLocations()
    {
        ExecuteNQ("DELETE FROM Locations");
    }

    public int InsertLocation(Location loc)
    {
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Locations (Name, Type, ParentId, ResourceNodes, DangerLevel, IsExplored, Status, MonsterTypeName, MapState)
            VALUES (@nm, @ty, @pi, @rn, @dl, @ie, @st, @mt, @ms)", _conn);
        cmd.Parameters.AddWithValue("@nm", loc.Name);
        cmd.Parameters.AddWithValue("@ty", loc.Type.ToString());
        cmd.Parameters.AddWithValue("@pi", loc.ParentId);
        cmd.Parameters.AddWithValue("@rn", JsonSerializer.Serialize(loc.ResourceNodes, JsonOpts));
        cmd.Parameters.AddWithValue("@dl", loc.DangerLevel);
        cmd.Parameters.AddWithValue("@ie", loc.IsExplored ? 1 : 0);
        cmd.Parameters.AddWithValue("@st", loc.Status.ToString());
        cmd.Parameters.AddWithValue("@mt", loc.MonsterTypeName);
        cmd.Parameters.AddWithValue("@ms", loc.MapState.ToString());
        cmd.ExecuteNonQuery();
        return (int)_conn.LastInsertRowId;
    }

    public void SetInitialResources(string saveId)
    {
        ExecuteNQ("UPDATE Resources SET Amount=10 WHERE Name='Еда'");
        ExecuteNQ("UPDATE Resources SET Amount=10 WHERE Name='Вода'");
        ExecuteNQ("UPDATE Resources SET Amount=1 WHERE Name NOT IN ('Еда','Вода') AND Id IN (SELECT Id FROM Resources WHERE Name NOT IN ('Еда','Вода') LIMIT 10)");
    }

    public void SetInitialQuests(string saveId)
    {
        var catalog = GetQuestCatalog(999);
        for (int i = 0; i < Math.Min(3, catalog.Count); i++)
            PurchaseQuest(saveId, catalog[i], QuestType.OneTime);
    }

    public void ResetDatabase()
    {
        if (!File.Exists(_templateSave._fileName))
            throw new FileNotFoundException($"Файл шаблону не знайдено: {_templateSave._fileName}");

        // Ensure template DB has Rovne map; generate it once if missing
        EnsureTemplateHasMap();

        CloseDatabaseConnections();

        if (File.Exists(_thisSave._fileName))
            File.Delete(_thisSave._fileName);

        File.Copy(_templateSave._fileName, _thisSave._fileName);

        OpenConnection(_thisSave._connectionString);

        EnsureNpcLocationColumn();
        EnsureNpcModifiersSchema();
        EnsurePlayerSchema();

        // Re-randomise resource nodes on all map locations
        var locations = GetAllLocations();
        var rnd = new Random();
        ApocMinimal.Systems.MapInitializer.InitialiseMapResources(locations, rnd);
        foreach (var loc in locations)
            SaveLocation(loc);

        // Place all NPCs at starting floor (first explored Floor by Id)
        object? startIdObj = ExecuteScalar("SELECT MIN(Id) FROM Locations WHERE Type='Floor' AND IsExplored=1");
        int startLocId = startIdObj is long sl ? (int)sl : 1;
        ExecuteNQ($"UPDATE Npcs SET LocationId={startLocId}");

        SetInitialResources(CurrentSaveId);
        SetInitialQuests(CurrentSaveId);
    }

    private void EnsureTemplateHasMap()
    {
        OpenConnection(_templateSave._connectionString);
        object? cnt = ExecuteScalar("SELECT COUNT(*) FROM Locations");
        bool hasMap = cnt is long n && n > 0;
        if (!hasMap)
        {
            var (rovneLocations, _) = ApocMinimal.Systems.RovneMapInitializer.GenerateLocations();
            var idMap = new Dictionary<int, int>();
            foreach (var loc in rovneLocations)
            {
                int oldId = loc.Id;
                int newId = InsertLocation(loc);
                idMap[oldId] = newId;
                loc.Id = newId;
            }
            foreach (var loc in rovneLocations)
            {
                if (loc.ParentId > 0 && idMap.TryGetValue(loc.ParentId, out int newParentId))
                {
                    using var cmd = new SQLiteCommand("UPDATE Locations SET ParentId=@p WHERE Id=@id", _conn);
                    cmd.Parameters.AddWithValue("@p", newParentId);
                    cmd.Parameters.AddWithValue("@id", loc.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        OpenConnection(""); // close template connection
    }

    private void EnsureNpcLocationColumn()
    {
        try { ExecuteNQ("ALTER TABLE Npcs ADD COLUMN LocationId INTEGER NOT NULL DEFAULT 0"); }
        catch { }
    }

    public void EnsureNpcModifiersSchema()
    {
        // Add missing columns to NpcModifiers if they don't exist (schema migration)
        string[] alterStatements =
        {
            "ALTER TABLE NpcModifiers ADD COLUMN ModifierClass TEXT NOT NULL DEFAULT 'Permanent'",
            "ALTER TABLE NpcModifiers ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1",
            "ALTER TABLE NpcModifiers ADD COLUMN TimeUnit INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE NpcModifiers ADD COLUMN Duration INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE NpcModifiers ADD COLUMN Remaining INTEGER NOT NULL DEFAULT 0",
        };
        foreach (var sql in alterStatements)
        {
            try { ExecuteNQ(sql); } catch { }
        }
    }

    public void EnsurePlayerSchema()
    {
        string[] alters =
        {
            "ALTER TABLE Player ADD COLUMN BarrierLevel INTEGER NOT NULL DEFAULT 1",
            "ALTER TABLE Player ADD COLUMN ControlledZoneIds TEXT NOT NULL DEFAULT '[]'",
        };
        foreach (var sql in alters)
        {
            try { ExecuteNQ(sql); } catch { }
        }
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
            string questTypeStr = GetStringOrDefault(rdr, "QuestType", "OneTime");
            q.QuestType = Enum.TryParse<QuestType>(questTypeStr, out var qt) ? qt : QuestType.OneTime;
            q.LibraryId = GetIntOrDefault(rdr, "LibraryId");
            string completeTypeStr = GetStringOrDefault(rdr, "CompleteType", "Time");
            q.CompleteType = Enum.TryParse<CompleteType>(completeTypeStr, out var ctq) ? ctq : CompleteType.Time;
            q.CompleteProgress = GetDoubleOrDefault(rdr, "CompleteProgress");
            q.CompleteTarget = GetDoubleOrDefault(rdr, "CompleteTarget");
            q.DayTaken = GetIntOrDefault(rdr, "DayTaken");
            string rewardTypeStr = GetStringOrDefault(rdr, "RewardType", "Resource");
            q.RewardType = Enum.TryParse<RewardType>(rewardTypeStr, out var rtq) ? rtq : RewardType.Resource;
            q.RewardTechnique = GetStringOrDefault(rdr, "RewardTechnique");
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
            "UPDATE Player SET FaithPoints=@fp,AltarLevel=@al,CurrentDay=@cd,BarrierSize=@bs,BarrierLevel=@bl,TerritoryControl=@tc,PlayerActionsToday=@pa,ControlledZoneIds=@cz WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@fp", p.FaithPoints);
        cmd.Parameters.AddWithValue("@al", p.AltarLevel);
        cmd.Parameters.AddWithValue("@cd", p.CurrentDay);
        cmd.Parameters.AddWithValue("@bs", p.BarrierSize);
        cmd.Parameters.AddWithValue("@bl", p.BarrierLevel);
        cmd.Parameters.AddWithValue("@tc", p.TerritoryControl);
        cmd.Parameters.AddWithValue("@pa", p.PlayerActionsToday);
        cmd.Parameters.AddWithValue("@cz", System.Text.Json.JsonSerializer.Serialize(p.ControlledZoneIds, JsonOpts));
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveLocation(Location loc)
    {
        using var cmd = new SQLiteCommand(
            "UPDATE Locations SET ResourceNodes=@rn, DangerLevel=@dl, IsExplored=@ie, Status=@st WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@rn", JsonSerializer.Serialize(loc.ResourceNodes, JsonOpts));
        cmd.Parameters.AddWithValue("@dl", loc.DangerLevel);
        cmd.Parameters.AddWithValue("@ie", loc.IsExplored ? 1 : 0);
        cmd.Parameters.AddWithValue("@st", loc.Status.ToString());
        cmd.Parameters.AddWithValue("@id", loc.Id);
        cmd.ExecuteNonQuery();
    }

    public void RegenerateAllNpcs(List<Npc> newNpcs)
    {
        using var tx = _conn.BeginTransaction();
        try
        {
            new SQLiteCommand("DELETE FROM NpcModifiers", _conn, tx).ExecuteNonQuery();
            new SQLiteCommand("DELETE FROM Npcs", _conn, tx).ExecuteNonQuery();
            foreach (var npc in newNpcs)
                InsertNpcTx(npc, tx);
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    private void InsertNpcTx(Npc n, SQLiteTransaction tx)
    {
        var traitStrings = n.CharTraits.Select(t => t.ToString()).ToList();
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Npcs (Name, Age, Gender, Profession, Description,
                Health, Faith, Stamina, Chakra, Fear, Trust, Initiative, CombatInitiative,
                Trait, FollowerLevel, Goal, Dream, Desire, ActiveTask, TaskDaysLeft, TaskRewardResId, TaskRewardAmt,
                Statistics, CharTraits, Specializations, Emotions, Needs, Memory)
            VALUES (@nm,@ag,@gn,@pr,@ds,
                @hp,@fa,@st,@ck,@fr,@tr,@in,@ci,
                @tt,@fl,@gl,@dr,@de,@at,@td,@rr,@ra,
                @stat,@ct,@sp,@em,@nd,@me)", _conn, tx);
        cmd.Parameters.AddWithValue("@nm", n.Name);
        cmd.Parameters.AddWithValue("@ag", n.Age);
        cmd.Parameters.AddWithValue("@gn", n.Gender == Gender.Female ? "Female" : "Male");
        cmd.Parameters.AddWithValue("@pr", n.Profession);
        cmd.Parameters.AddWithValue("@ds", n.Description ?? "");
        cmd.Parameters.AddWithValue("@hp", n.Health);
        cmd.Parameters.AddWithValue("@fa", n.Faith);
        cmd.Parameters.AddWithValue("@st", n.Stamina);
        cmd.Parameters.AddWithValue("@ck", n.Chakra);
        cmd.Parameters.AddWithValue("@fr", n.Fear);
        cmd.Parameters.AddWithValue("@tr", n.Trust);
        cmd.Parameters.AddWithValue("@in", n.Initiative);
        cmd.Parameters.AddWithValue("@ci", n.CombatInitiative);
        cmd.Parameters.AddWithValue("@tt", n.Trait.ToString());
        cmd.Parameters.AddWithValue("@fl", n.FollowerLevel);
        cmd.Parameters.AddWithValue("@gl", n.Goal ?? "");
        cmd.Parameters.AddWithValue("@dr", n.Dream ?? "");
        cmd.Parameters.AddWithValue("@de", n.Desire ?? "");
        cmd.Parameters.AddWithValue("@at", n.ActiveTask ?? "");
        cmd.Parameters.AddWithValue("@td", n.TaskDaysLeft);
        cmd.Parameters.AddWithValue("@rr", n.TaskRewardResId);
        cmd.Parameters.AddWithValue("@ra", n.TaskRewardAmt);
        cmd.Parameters.AddWithValue("@stat", JsonSerializer.Serialize(n.Stats, JsonOpts));
        cmd.Parameters.AddWithValue("@ct", JsonSerializer.Serialize(traitStrings, JsonOpts));
        cmd.Parameters.AddWithValue("@sp", JsonSerializer.Serialize(n.Specializations, JsonOpts));
        cmd.Parameters.AddWithValue("@em", JsonSerializer.Serialize(n.Emotions, JsonOpts));
        cmd.Parameters.AddWithValue("@nd", JsonSerializer.Serialize(n.Needs, JsonOpts));
        cmd.Parameters.AddWithValue("@me", JsonSerializer.Serialize(n.Memory, JsonOpts));
        cmd.ExecuteNonQuery();
        long newId = (long)new SQLiteCommand("SELECT last_insert_rowid()", _conn, tx).ExecuteScalar();
        n.Id = (int)newId;
        SaveModifiersForNpc(n.Id, n.Stats);
    }

    public void SaveNpc(Npc n)
    {
        using var cmd = new SQLiteCommand(@"
        UPDATE Npcs SET
            Health=@hp, Faith=@fa, Stamina=@st, Chakra=@ck,
            Fear=@fr, Trust=@tr, Initiative=@in, CombatInitiative=@ci, FollowerLevel=@fl,
            CharTraits=@ct, Specializations=@sp, Emotions=@em,
            Goal=@gl, Dream=@dr, Desire=@de,
            Needs=@nd, Memory=@me, Statistics=@stat, LocationId=@li
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
        cmd.Parameters.AddWithValue("@li", n.LocationId);

        SaveModifiersForNpc(n.Id, n.Stats);
        cmd.ExecuteNonQuery();
    }

    // DatabaseManager.cs - исправленный метод SaveModifiersForNpc (строка 747)

    private void SaveModifiersForNpc(int npcId, Statistics stats)
    {
        using (var del = new SQLiteCommand("DELETE FROM NpcModifiers WHERE NpcId=@id", _conn))
        {
            del.Parameters.AddWithValue("@id", npcId);
            del.ExecuteNonQuery();
        }

        for (int i = 0; i < stats.AllStats.Count; i++)
        {
            Characteristic stat = stats.AllStats[i];

            foreach (var mod in stat.GetModifiersByType<PermanentModifier>())
            {
                using var cmd = new SQLiteCommand(@"
                    INSERT INTO NpcModifiers (NpcId, StatId, ModifierId, Name, Source, ModifierType, Value, ModifierClass, IsActive)
                    VALUES (@nid,@sid,@mid,@nm,@src,@mty,@val,'Permanent',@act)", _conn);
                cmd.Parameters.AddWithValue("@nid", npcId);
                cmd.Parameters.AddWithValue("@sid", stat.Id);
                cmd.Parameters.AddWithValue("@mid", mod.Id);
                cmd.Parameters.AddWithValue("@nm",  mod.Name);
                cmd.Parameters.AddWithValue("@src", mod.Source);
                cmd.Parameters.AddWithValue("@mty", (int)mod.Type);
                cmd.Parameters.AddWithValue("@val", mod.Value);
                cmd.Parameters.AddWithValue("@act", mod.IsActiveFlag ? 1 : 0);
                cmd.ExecuteNonQuery();
            }

            foreach (var mod in stat.GetModifiersByType<IndependentModifier>())
            {
                using var cmd = new SQLiteCommand(@"
                    INSERT INTO NpcModifiers (NpcId, StatId, ModifierId, Name, Source, ModifierType, Value, ModifierClass, TimeUnit, Duration, Remaining)
                    VALUES (@nid,@sid,@mid,@nm,@src,@mty,@val,'Independent',@tu,@dur,@rem)", _conn);
                cmd.Parameters.AddWithValue("@nid", npcId);
                cmd.Parameters.AddWithValue("@sid", stat.Id);
                cmd.Parameters.AddWithValue("@mid", mod.Id);
                cmd.Parameters.AddWithValue("@nm",  mod.Name);
                cmd.Parameters.AddWithValue("@src", mod.Source);
                cmd.Parameters.AddWithValue("@mty", (int)mod.Type);
                cmd.Parameters.AddWithValue("@val", mod.Value);
                cmd.Parameters.AddWithValue("@tu",  (int)mod.TimeUnit);
                cmd.Parameters.AddWithValue("@dur", mod.Duration);
                cmd.Parameters.AddWithValue("@rem", mod.Remaining);
                cmd.ExecuteNonQuery();
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
                ModifierType modType = (ModifierType)rdr.GetInt32(rdr.GetOrdinal("ModifierType"));
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
                ModifierType modType = (ModifierType)rdr.GetInt32(rdr.GetOrdinal("ModifierType"));
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
        p.BarrierLevel = GetIntOrDefault(rdr, "BarrierLevel", 1);
        p.TerritoryControl = GetIntOrDefault(rdr, "TerritoryControl");
        p.PlayerActionsToday = GetIntOrDefault(rdr, "PlayerActionsToday");
        string zonesJson = GetStringOrDefault(rdr, "ControlledZoneIds", "[]");
        try { p.ControlledZoneIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(zonesJson) ?? new(); }
        catch { p.ControlledZoneIds = new(); }
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

        npc.LocationId = GetIntOrDefault(rdr, "LocationId", 0);

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

    public List<ResourceCatalogEntry> GetResourceCatalog()
    {
        var list = new List<ResourceCatalogEntry>();
        if (!IsTableExistsSafe("ResourceCatalog")) return list;
        using var cmd = new SQLiteCommand("SELECT * FROM ResourceCatalog ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            list.Add(new ResourceCatalogEntry
            {
                Id            = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Name          = rdr.GetString(rdr.GetOrdinal("Name")),
                Category      = rdr.GetString(rdr.GetOrdinal("Category")),
                Rarity        = GetStringOrDefault(rdr, "Rarity", "Common"),
                Unit          = GetStringOrDefault(rdr, "Unit", "шт"),
                Weight        = GetDoubleOrDefault(rdr, "Weight", 0.5),
                SpoilageDays  = GetIntOrDefault(rdr, "SpoilageDays", 0),
                FoodRestore   = GetDoubleOrDefault(rdr, "FoodRestore", 0),
                WaterRestore  = GetDoubleOrDefault(rdr, "WaterRestore", 0),
                IsLocationNode = GetIntOrDefault(rdr, "IsLocationNode", 1) == 1,
                LocationWeight = GetIntOrDefault(rdr, "LocationWeight", 1),
                Quality = GetIntOrDefault(rdr, "Quality", 1),
            });
        }
        return list;
    }

    public Dictionary<string, double> GetGameConfig()
    {
        var config = new Dictionary<string, double>();
        if (!IsTableExistsSafe("GameConfig")) return config;
        using var cmd = new SQLiteCommand("SELECT Key, Value FROM GameConfig", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            string key = rdr.GetString(0);
            string val = rdr.GetString(1);
            if (double.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
                config[key] = d;
        }
        return config;
    }

    public List<QuestCatalogEntry> GetQuestCatalog(int altarLevel)
    {
        var list = new List<QuestCatalogEntry>();
        if (!IsTableExistsSafe("QuestCatalog")) return list;
        using var cmd = new SQLiteCommand("SELECT * FROM QuestCatalog WHERE MinAltarLevel <= @al ORDER BY MinAltarLevel, Id", _conn);
        cmd.Parameters.AddWithValue("@al", altarLevel);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            var entry = new QuestCatalogEntry
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Title = rdr.GetString(rdr.GetOrdinal("Title")),
                Description = GetStringOrDefault(rdr, "Description"),
                MinAltarLevel = GetIntOrDefault(rdr, "MinAltarLevel", 1),
                TakeCondStat = GetStringOrDefault(rdr, "TakeCondStat"),
                TakeCondValue = GetIntOrDefault(rdr, "TakeCondValue"),
                CompleteDays = GetIntOrDefault(rdr, "CompleteDays", 3),
                CompleteResource = GetStringOrDefault(rdr, "CompleteResource"),
                CompleteAmount = GetDoubleOrDefault(rdr, "CompleteAmount"),
                CompleteAction = GetStringOrDefault(rdr, "CompleteAction"),
                RewardResource = GetStringOrDefault(rdr, "RewardResource"),
                RewardAmount = GetDoubleOrDefault(rdr, "RewardAmount"),
                RewardTechnique = GetStringOrDefault(rdr, "RewardTechnique"),
                Category = GetStringOrDefault(rdr, "Category"),
            };
            string completeTypeStr = GetStringOrDefault(rdr, "CompleteType", "Time");
            entry.CompleteType = Enum.TryParse<CompleteType>(completeTypeStr, out var ct) ? ct : CompleteType.Time;
            string rewardTypeStr = GetStringOrDefault(rdr, "RewardType", "Resource");
            entry.RewardType = Enum.TryParse<RewardType>(rewardTypeStr, out var rt) ? rt : RewardType.Resource;
            int ordPot = -1;
            try { ordPot = rdr.GetOrdinal("PriceOneTime"); } catch { }
            if (ordPot >= 0) entry.PriceOneTime = rdr.IsDBNull(ordPot) ? (double?)null : rdr.GetDouble(ordPot);
            int ordPr = -1;
            try { ordPr = rdr.GetOrdinal("PriceRepeatable"); } catch { }
            if (ordPr >= 0) entry.PriceRepeatable = rdr.IsDBNull(ordPr) ? (double?)null : rdr.GetDouble(ordPr);
            int ordPe = -1;
            try { ordPe = rdr.GetOrdinal("PriceEternal"); } catch { }
            if (ordPe >= 0) entry.PriceEternal = rdr.IsDBNull(ordPe) ? (double?)null : rdr.GetDouble(ordPe);
            list.Add(entry);
        }
        return list;
    }

    public List<PlayerLibraryEntry> GetPlayerLibrary(string saveId)
    {
        var list = new List<PlayerLibraryEntry>();
        if (!IsTableExistsSafe("PlayerQuestLibrary")) return list;
        using var cmd = new SQLiteCommand("SELECT * FROM PlayerQuestLibrary WHERE SaveId = @sid", _conn);
        cmd.Parameters.AddWithValue("@sid", saveId);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            var entry = new PlayerLibraryEntry
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
                SaveId = GetStringOrDefault(rdr, "SaveId"),
                CatalogId = GetIntOrDefault(rdr, "CatalogId"),
                PublishesLeft = GetIntOrDefault(rdr, "PublishesLeft"),
                TimesCompleted = GetIntOrDefault(rdr, "TimesCompleted"),
            };
            string qtStr = GetStringOrDefault(rdr, "QuestType", "OneTime");
            entry.QuestType = Enum.TryParse<QuestType>(qtStr, out var qt) ? qt : QuestType.OneTime;
            list.Add(entry);
        }

        var catalogAll = GetQuestCatalog(999);
        var catalogMap = new Dictionary<int, QuestCatalogEntry>();
        for (int i = 0; i < catalogAll.Count; i++)
            catalogMap[catalogAll[i].Id] = catalogAll[i];

        for (int i = 0; i < list.Count; i++)
        {
            if (catalogMap.TryGetValue(list[i].CatalogId, out var cat))
                list[i].Catalog = cat;
        }
        return list;
    }

    public void SaveQuestHistory(QuestHistoryEntry entry)
    {
        if (!IsTableExistsSafe("QuestHistory")) return;
        using var cmd = new SQLiteCommand(@"
            INSERT INTO QuestHistory (SaveId, CatalogId, QuestTitle, NpcName, DayTaken, DayCompleted, RewardGiven)
            VALUES (@sid, @cid, @qt, @nn, @dt, @dc, @rg)", _conn);
        cmd.Parameters.AddWithValue("@sid", entry.SaveId);
        cmd.Parameters.AddWithValue("@cid", entry.CatalogId);
        cmd.Parameters.AddWithValue("@qt", entry.QuestTitle);
        cmd.Parameters.AddWithValue("@nn", entry.NpcName);
        cmd.Parameters.AddWithValue("@dt", entry.DayTaken);
        cmd.Parameters.AddWithValue("@dc", entry.DayCompleted);
        cmd.Parameters.AddWithValue("@rg", entry.RewardGiven);
        cmd.ExecuteNonQuery();
    }

    public List<QuestHistoryEntry> GetQuestHistory(string saveId)
    {
        var list = new List<QuestHistoryEntry>();
        if (!IsTableExistsSafe("QuestHistory")) return list;
        using var cmd = new SQLiteCommand("SELECT * FROM QuestHistory WHERE SaveId = @sid ORDER BY DayCompleted DESC", _conn);
        cmd.Parameters.AddWithValue("@sid", saveId);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            list.Add(new QuestHistoryEntry
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
                SaveId = GetStringOrDefault(rdr, "SaveId"),
                CatalogId = GetIntOrDefault(rdr, "CatalogId"),
                QuestTitle = GetStringOrDefault(rdr, "QuestTitle"),
                NpcName = GetStringOrDefault(rdr, "NpcName"),
                DayTaken = GetIntOrDefault(rdr, "DayTaken"),
                DayCompleted = GetIntOrDefault(rdr, "DayCompleted"),
                RewardGiven = GetStringOrDefault(rdr, "RewardGiven"),
            });
        }
        return list;
    }

    public void PurchaseQuest(string saveId, QuestCatalogEntry catalog, QuestType questType)
    {
        int publishesLeft = questType == QuestType.Eternal ? -1 : (questType == QuestType.Repeatable ? 10 : 1);
        using var cmd = new SQLiteCommand(
            "INSERT INTO PlayerQuestLibrary (SaveId, CatalogId, PublishesLeft, TimesCompleted, QuestType) VALUES (@sid, @cid, @pl, 0, @qt)", _conn);
        cmd.Parameters.AddWithValue("@sid", saveId);
        cmd.Parameters.AddWithValue("@cid", catalog.Id);
        cmd.Parameters.AddWithValue("@pl", publishesLeft);
        cmd.Parameters.AddWithValue("@qt", questType.ToString());
        cmd.ExecuteNonQuery();
    }

    public void UpdateLibraryEntry(PlayerLibraryEntry entry)
    {
        using var cmd = new SQLiteCommand(
            "UPDATE PlayerQuestLibrary SET PublishesLeft=@pl, TimesCompleted=@tc WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@pl", entry.PublishesLeft);
        cmd.Parameters.AddWithValue("@tc", entry.TimesCompleted);
        cmd.Parameters.AddWithValue("@id", entry.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteLibraryEntry(int id)
    {
        using var cmd = new SQLiteCommand("DELETE FROM PlayerQuestLibrary WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void SaveQuestFull(Quest q)
    {
        if (q.Id == 0)
        {
            using var ins = new SQLiteCommand(@"
                INSERT INTO Quests (Title, Description, Source, Status, AssignedNpcId, DaysRequired, DaysRemaining,
                    RewardResourceId, RewardAmount, FaithCost, QuestType, LibraryId,
                    CompleteType, CompleteProgress, CompleteTarget, DayTaken, RewardType, RewardTechnique)
                VALUES (@ti,@de,@so,@st,@an,@dre,@drm,@rri,@ra,@fc,@qt,@li,@cty,@cp,@ct2,@dt,@rty,@rte)", _conn);
            ins.Parameters.AddWithValue("@ti", q.Title);
            ins.Parameters.AddWithValue("@de", q.Description);
            ins.Parameters.AddWithValue("@so", q.Source.ToString());
            ins.Parameters.AddWithValue("@st", q.Status.ToString());
            ins.Parameters.AddWithValue("@an", q.AssignedNpcId);
            ins.Parameters.AddWithValue("@dre", q.DaysRequired);
            ins.Parameters.AddWithValue("@drm", q.DaysRemaining);
            ins.Parameters.AddWithValue("@rri", q.RewardResourceId);
            ins.Parameters.AddWithValue("@ra", q.RewardAmount);
            ins.Parameters.AddWithValue("@fc", q.FaithCost);
            ins.Parameters.AddWithValue("@qt", q.QuestType.ToString());
            ins.Parameters.AddWithValue("@li", q.LibraryId);
            ins.Parameters.AddWithValue("@cty", q.CompleteType.ToString());
            ins.Parameters.AddWithValue("@cp", q.CompleteProgress);
            ins.Parameters.AddWithValue("@ct2", q.CompleteTarget);
            ins.Parameters.AddWithValue("@dt", q.DayTaken);
            ins.Parameters.AddWithValue("@rty", q.RewardType.ToString());
            ins.Parameters.AddWithValue("@rte", q.RewardTechnique);
            ins.ExecuteNonQuery();
            q.Id = (int)_conn.LastInsertRowId;
        }
        else
        {
            using var upd = new SQLiteCommand(@"
                UPDATE Quests SET Status=@st, AssignedNpcId=@an, DaysRemaining=@drm,
                    QuestType=@qt, LibraryId=@li,
                    CompleteType=@cty, CompleteProgress=@cp, CompleteTarget=@ct2,
                    DayTaken=@dt, RewardType=@rty, RewardTechnique=@rte
                WHERE Id=@id", _conn);
            upd.Parameters.AddWithValue("@st", q.Status.ToString());
            upd.Parameters.AddWithValue("@an", q.AssignedNpcId);
            upd.Parameters.AddWithValue("@drm", q.DaysRemaining);
            upd.Parameters.AddWithValue("@qt", q.QuestType.ToString());
            upd.Parameters.AddWithValue("@li", q.LibraryId);
            upd.Parameters.AddWithValue("@cty", q.CompleteType.ToString());
            upd.Parameters.AddWithValue("@cp", q.CompleteProgress);
            upd.Parameters.AddWithValue("@ct2", q.CompleteTarget);
            upd.Parameters.AddWithValue("@dt", q.DayTaken);
            upd.Parameters.AddWithValue("@rty", q.RewardType.ToString());
            upd.Parameters.AddWithValue("@rte", q.RewardTechnique);
            upd.Parameters.AddWithValue("@id", q.Id);
            upd.ExecuteNonQuery();
        }
    }

    public void DeleteQuest(int questId)
    {
        using var cmd = new SQLiteCommand("DELETE FROM Quests WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@id", questId);
        cmd.ExecuteNonQuery();
    }

    public void EnsureResourceShopTable()
    {
        ExecuteNQ(@"CREATE TABLE IF NOT EXISTS ResourceShop (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SaveId TEXT NOT NULL,
            ResourceName TEXT NOT NULL,
            UNIQUE(SaveId, ResourceName)
        )");
    }

    public List<string> GetShopUnlocks(string saveId)
    {
        var list = new List<string>();
        if (!IsTableExistsSafe("ResourceShop")) return list;
        using var cmd = new SQLiteCommand("SELECT ResourceName FROM ResourceShop WHERE SaveId=@s", _conn);
        cmd.Parameters.AddWithValue("@s", saveId);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(rdr.GetString(0));
        return list;
    }

    public void UnlockShopResource(string saveId, string resourceName)
    {
        EnsureResourceShopTable();
        using var cmd = new SQLiteCommand(
            "INSERT OR IGNORE INTO ResourceShop(SaveId, ResourceName) VALUES(@s,@r)", _conn);
        cmd.Parameters.AddWithValue("@s", saveId);
        cmd.Parameters.AddWithValue("@r", resourceName);
        cmd.ExecuteNonQuery();
    }

    public List<int> GetAppliedExchanges(string saveId)
    {
        EnsureAppliedExchangesTable();
        var list = new List<int>();
        using var cmd = new SQLiteCommand("SELECT ExchangeId FROM AppliedExchanges WHERE SaveId=@s", _conn);
        cmd.Parameters.AddWithValue("@s", saveId);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(rdr.GetInt32(0));
        return list;
    }

    public void SaveAppliedExchange(string saveId, int exchangeId)
    {
        EnsureAppliedExchangesTable();
        using var cmd = new SQLiteCommand(
            "INSERT OR IGNORE INTO AppliedExchanges(SaveId,ExchangeId) VALUES(@s,@e)", _conn);
        cmd.Parameters.AddWithValue("@s", saveId);
        cmd.Parameters.AddWithValue("@e", exchangeId);
        cmd.ExecuteNonQuery();
    }

    private void EnsureAppliedExchangesTable()
    {
        new SQLiteCommand(@"
            CREATE TABLE IF NOT EXISTS AppliedExchanges(
                SaveId TEXT NOT NULL,
                ExchangeId INTEGER NOT NULL,
                PRIMARY KEY(SaveId, ExchangeId)
            )", _conn).ExecuteNonQuery();
    }

}

public class OneSave
{
    public string _connectionString = "";
    public string _fileName = "";
    public bool _active = false;
    public int _currentDay = 0;
    public int _altarLevel = 0;
    public double _faithPoints = 0;

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