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