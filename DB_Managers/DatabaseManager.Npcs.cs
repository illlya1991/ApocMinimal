using System.Data.SQLite;
using System.Text.Json;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.StatisticsData;
using ApocMinimal.Systems;

namespace ApocMinimal.Database;

public partial class DatabaseManager
{
    public void RegenerateAllNpcs(List<Npc> newNpcs)
    {
        using var tx = _conn.BeginTransaction();
        try
        {
            new SQLiteCommand("DELETE FROM NpcModifiers", _conn, tx).ExecuteNonQuery();
            new SQLiteCommand("DELETE FROM Npcs", _conn, tx).ExecuteNonQuery();
            foreach (var npc in newNpcs) InsertNpcTx(npc, tx);
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }
    public void SaveNpcInTransaction(Npc n, SQLiteTransaction transaction)
    {
        bool hasEvo = HasEvolutionLevelColumn();
        string evoSet = hasEvo ? ", EvolutionLevel=@el" : "";
        using var cmd = new SQLiteCommand($@"
        UPDATE Npcs SET
            Health=@hp, Devotion=@fa, Stamina=@st, Energy=@ck,
            Fear=@fr, Trust=@tr, Initiative=@in, CombatInitiative=@ci, FollowerLevel=@fl{evoSet},
            CharTraits=@ct, Specializations=@sp, Emotions=@em,
            Goal=@gl, Dream=@dr, Desire=@de,
            Needs=@nd, Memory=@me, Statistics=@stat, LocationId=@li,
            LearnedTechIds=@lti
        WHERE Id=@id", _conn, transaction);

        cmd.Parameters.AddWithValue("@hp", n.Health);
        cmd.Parameters.AddWithValue("@fa", n.Devotion);
        cmd.Parameters.AddWithValue("@st", n.Stamina);
        cmd.Parameters.AddWithValue("@ck", n.Energy);
        cmd.Parameters.AddWithValue("@fr", n.Fear);
        cmd.Parameters.AddWithValue("@tr", n.Trust);
        cmd.Parameters.AddWithValue("@in", n.Initiative);
        cmd.Parameters.AddWithValue("@ci", n.CombatInitiative);
        cmd.Parameters.AddWithValue("@fl", n.FollowerLevel);
        if (hasEvo) cmd.Parameters.AddWithValue("@el", n.EvolutionLevel);

        cmd.Parameters.AddWithValue("@ct", JsonSerializer.Serialize(n.CharTraits.Select(t => t.ToString()).ToList(), JsonOpts));
        cmd.Parameters.AddWithValue("@sp", JsonSerializer.Serialize(n.Specializations, JsonOpts));
        cmd.Parameters.AddWithValue("@em", JsonSerializer.Serialize(n.Emotions, JsonOpts));
        cmd.Parameters.AddWithValue("@gl", n.Goal ?? "");
        cmd.Parameters.AddWithValue("@dr", n.Dream ?? "");
        cmd.Parameters.AddWithValue("@de", n.Desire ?? "");
        cmd.Parameters.AddWithValue("@nd", JsonSerializer.Serialize(n.Needs, JsonOpts));
        cmd.Parameters.AddWithValue("@me", JsonSerializer.Serialize(n.Memory, JsonOpts));
        cmd.Parameters.AddWithValue("@stat", JsonSerializer.Serialize(n.Stats, JsonOpts));
        cmd.Parameters.AddWithValue("@id", n.Id);
        cmd.Parameters.AddWithValue("@li", n.LocationId);
        cmd.Parameters.AddWithValue("@lti", JsonSerializer.Serialize(n.LearnedTechIds, JsonOpts));

        cmd.ExecuteNonQuery();

        // Сохраняем модификаторы в той же транзакции
        SaveModifiersForNpcInTransaction(n.Id, n.Stats, transaction);
    }

    private void SaveModifiersForNpcInTransaction(int npcId, Statistics stats, SQLiteTransaction transaction)
    {
        // Удаляем старые модификаторы
        using var delCmd = new SQLiteCommand("DELETE FROM NpcModifiers WHERE NpcId=@id", _conn, transaction);
        delCmd.Parameters.AddWithValue("@id", npcId);
        delCmd.ExecuteNonQuery();

        // Вставляем новые модификаторы
        for (int i = 0; i < stats.AllStats.Count; i++)
        {
            Characteristic stat = stats.AllStats[i];

            foreach (var mod in stat.GetModifiersByType<PermanentModifier>())
            {
                using var cmd = new SQLiteCommand(@"
                INSERT INTO NpcModifiers (NpcId, StatId, ModifierId, Name, Source, ModifierType, Value, ModifierClass, IsActive)
                VALUES (@nid,@sid,@mid,@nm,@src,@mty,@val,'Permanent',@act)", _conn, transaction);

                cmd.Parameters.AddWithValue("@nid", npcId);
                cmd.Parameters.AddWithValue("@sid", stat.Id);
                cmd.Parameters.AddWithValue("@mid", mod.Id);
                cmd.Parameters.AddWithValue("@nm", mod.Name);
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
                VALUES (@nid,@sid,@mid,@nm,@src,@mty,@val,'Independent',@tu,@dur,@rem)", _conn, transaction);

                cmd.Parameters.AddWithValue("@nid", npcId);
                cmd.Parameters.AddWithValue("@sid", stat.Id);
                cmd.Parameters.AddWithValue("@mid", mod.Id);
                cmd.Parameters.AddWithValue("@nm", mod.Name);
                cmd.Parameters.AddWithValue("@src", mod.Source);
                cmd.Parameters.AddWithValue("@mty", (int)mod.Type);
                cmd.Parameters.AddWithValue("@val", mod.Value);
                cmd.Parameters.AddWithValue("@tu", (int)mod.TimeUnit);
                cmd.Parameters.AddWithValue("@dur", mod.Duration);
                cmd.Parameters.AddWithValue("@rem", mod.Remaining);
                cmd.ExecuteNonQuery();
            }
        }
    }

    private void InsertNpcTx(Npc n, SQLiteTransaction tx)
    {
        bool hasEvo = HasEvolutionLevelColumn();
        string evoCol = hasEvo ? ", EvolutionLevel" : "";
        string evoVal = hasEvo ? ",@el" : "";
        var traitStrings = n.CharTraits.Select(t => t.ToString()).ToList();
        using var cmd = new SQLiteCommand($@"
            INSERT INTO Npcs (Name, Age, Gender, Profession, Description,
                Health, Devotion, Stamina, Energy, Fear, Trust, Initiative, CombatInitiative,
                Trait, FollowerLevel{evoCol}, Goal, Dream, Desire, ActiveTask, TaskDaysLeft, TaskRewardResId, TaskRewardAmt,
                Statistics, CharTraits, Specializations, Emotions, Needs, Memory)
            VALUES (@nm,@ag,@gn,@pr,@ds,
                @hp,@fa,@st,@ck,@fr,@tr,@in,@ci,
                @tt,@fl{evoVal},@gl,@dr,@de,@at,@td,@rr,@ra,
                @stat,@ct,@sp,@em,@nd,@me)", _conn, tx);
        cmd.Parameters.AddWithValue("@nm", n.Name);
        cmd.Parameters.AddWithValue("@ag", n.Age);
        cmd.Parameters.AddWithValue("@gn", n.Gender == Gender.Female ? "Female" : "Male");
        cmd.Parameters.AddWithValue("@pr", n.Profession);
        cmd.Parameters.AddWithValue("@ds", n.Description ?? "");
        cmd.Parameters.AddWithValue("@hp", n.Health);
        cmd.Parameters.AddWithValue("@fa", n.Devotion);
        cmd.Parameters.AddWithValue("@st", n.Stamina);
        cmd.Parameters.AddWithValue("@ck", n.Energy);
        cmd.Parameters.AddWithValue("@fr", n.Fear);
        cmd.Parameters.AddWithValue("@tr", n.Trust);
        cmd.Parameters.AddWithValue("@in", n.Initiative);
        cmd.Parameters.AddWithValue("@ci", n.CombatInitiative);
        cmd.Parameters.AddWithValue("@tt", n.Trait.ToString());
        cmd.Parameters.AddWithValue("@fl", n.FollowerLevel);
        if (hasEvo) cmd.Parameters.AddWithValue("@el", n.EvolutionLevel);
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
        bool hasEvo = HasEvolutionLevelColumn();
        string evoSet = hasEvo ? ", EvolutionLevel=@el" : "";
        using var cmd = new SQLiteCommand($@"
        UPDATE Npcs SET
            Health=@hp, Devotion=@fa, Stamina=@st, Energy=@ck,
            Fear=@fr, Trust=@tr, Initiative=@in, CombatInitiative=@ci, FollowerLevel=@fl{evoSet},
            CharTraits=@ct, Specializations=@sp, Emotions=@em,
            Goal=@gl, Dream=@dr, Desire=@de,
            Needs=@nd, Memory=@me, Statistics=@stat, LocationId=@li,
            LearnedTechIds=@lti
        WHERE Id=@id", _conn);

        cmd.Parameters.AddWithValue("@hp", n.Health);
        cmd.Parameters.AddWithValue("@fa", n.Devotion);
        cmd.Parameters.AddWithValue("@st", n.Stamina);
        cmd.Parameters.AddWithValue("@ck", n.Energy);
        cmd.Parameters.AddWithValue("@fr", n.Fear);
        cmd.Parameters.AddWithValue("@tr", n.Trust);
        cmd.Parameters.AddWithValue("@in", n.Initiative);
        cmd.Parameters.AddWithValue("@ci", n.CombatInitiative);
        cmd.Parameters.AddWithValue("@fl", n.FollowerLevel);
        if (hasEvo) cmd.Parameters.AddWithValue("@el", n.EvolutionLevel);

        cmd.Parameters.AddWithValue("@ct", JsonSerializer.Serialize(n.CharTraits.Select(t => t.ToString()).ToList(), JsonOpts));
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
        cmd.Parameters.AddWithValue("@lti", JsonSerializer.Serialize(n.LearnedTechIds, JsonOpts));

        SaveModifiersForNpc(n.Id, n.Stats);
        cmd.ExecuteNonQuery();
    }

    public int GetFollowerCountAtLevel(int followerLevel)
    {
        using var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Npcs WHERE FollowerLevel = @level AND Health > 0", _conn);
        cmd.Parameters.AddWithValue("@level", followerLevel);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
    }

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

                var mod = new PermanentModifier(modId, modName, modSource, modType, modValue);
                if (isActive == 0) mod.Deactivate();
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

                var mod = new IndependentModifier(modId, modName, modSource, modType, modValue, timeUnit, duration);
                stat.AddModifier(mod);
            }
        }

        return stats;
    }

    private Npc ReadNpc(SQLiteDataReader rdr)
    {
        var npc = new Npc();
        npc.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
        npc.Name = rdr.GetString(rdr.GetOrdinal("Name"));
        npc.Age = rdr.GetInt32(rdr.GetOrdinal("Age"));

        string genderStr = GetStringOrDefault(rdr, "Gender", "Male");
        npc.Gender = (genderStr == "Female") ? Gender.Female : Gender.Male;

        npc.Profession = rdr.GetString(rdr.GetOrdinal("Profession"));
        npc.Description = GetStringOrDefault(rdr, "Description");
        npc.Health = rdr.GetDouble(rdr.GetOrdinal("Health"));
        npc.Devotion = GetDoubleOrDefault(rdr, "Devotion", 20);
        npc.Stamina = GetDoubleOrDefault(rdr, "Stamina", 100);
        npc.Energy = GetDoubleOrDefault(rdr, "Energy", 50);
        npc.Fear = GetDoubleOrDefault(rdr, "Fear", 10);
        npc.Trust = GetDoubleOrDefault(rdr, "Trust", 50);
        npc.Initiative = GetDoubleOrDefault(rdr, "Initiative", 50);
        npc.CombatInitiative = GetDoubleOrDefault(rdr, "CombatInitiative", 50);

        string traitStr = GetStringOrDefault(rdr, "Trait", "None");
        npc.Trait = traitStr switch
        {
            "Leader" => NpcTrait.Leader,
            "Coward" => NpcTrait.Coward,
            "Loner"  => NpcTrait.Loner,
            _        => NpcTrait.None,
        };

        npc.FollowerLevel = GetIntOrDefault(rdr, "FollowerLevel");
        npc.EvolutionLevel = GetIntOrDefault(rdr, "EvolutionLevel");
        npc.Goal = GetStringOrDefault(rdr, "Goal");
        npc.Dream = GetStringOrDefault(rdr, "Dream");
        npc.Desire = GetStringOrDefault(rdr, "Desire");
        npc.ActiveTask = GetStringOrDefault(rdr, "ActiveTask");
        npc.TaskDaysLeft = GetIntOrDefault(rdr, "TaskDaysLeft");
        npc.TaskRewardResId = GetIntOrDefault(rdr, "TaskRewardResId");
        npc.TaskRewardAmt = GetDoubleOrDefault(rdr, "TaskRewardAmt");

        string statsJson = GetStringOrDefault(rdr, "Statistics", "{}");
        try
        {
            var loadedStats = JsonSerializer.Deserialize<Statistics>(statsJson);
            if (loadedStats != null) npc.Stats = loadedStats;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Ошибка Statistics NPC {npc.Id}: {ex.Message}"); }

        npc.Stats = LoadModifiersForNpc(npc.Id, npc.Stats);

        npc.Needs = DeserializeOrDefault<List<Need>>(rdr, "Needs") ?? new();
        npc.Emotions = DeserializeOrDefault<List<Emotion>>(rdr, "Emotions") ?? new();
        npc.Specializations = DeserializeOrDefault<List<string>>(rdr, "Specializations") ?? new();
        npc.Memory = DeserializeOrDefault<List<MemoryEntry>>(rdr, "Memory") ?? new();

        var charTraitStrings = DeserializeOrDefault<List<string>>(rdr, "CharTraits") ?? new();
        npc.CharTraits = new List<CharacterTrait>();
        foreach (var s in charTraitStrings)
        {
            if (Enum.TryParse<CharacterTrait>(s, out var ct)) npc.CharTraits.Add(ct);
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
        npc.LearnedTechIds = DeserializeOrDefault<List<string>>(rdr, "LearnedTechIds") ?? new();

        return npc;
    }

    private static bool? _hasEvolutionLevel = null;

    private bool HasEvolutionLevelColumn()
    {
        if (_hasEvolutionLevel.HasValue) return _hasEvolutionLevel.Value;
        using var cmd = new SQLiteCommand(
            "SELECT COUNT(*) FROM pragma_table_info('Npcs') WHERE name='EvolutionLevel'", _conn);
        _hasEvolutionLevel = (long)(cmd.ExecuteScalar() ?? 0L) > 0;
        return _hasEvolutionLevel.Value;
    }

    public List<Npc> GetAllNpcsOptimized()
    {
        lock (_npcCacheLock)
        {
            if (_npcCache.Count > 0 && (DateTime.Now - _lastNpcLoad) < _cacheExpiry)
            {
                System.Diagnostics.Debug.WriteLine($"  NPC: возвращено из кэша {_npcCache.Count} NPC");
                return _npcCache.Values.ToList();
            }
        }

        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        var npcs = new List<Npc>();

        bool hasEvo = HasEvolutionLevelColumn();
        string evoCol = hasEvo ? ", EvolutionLevel" : "";

        using var cmd = new SQLiteCommand($@"
        SELECT Id, Name, Age, Gender, Profession, Description,
               Health, Devotion, Stamina, Energy, Fear, Trust,
               Initiative, CombatInitiative, Trait, FollowerLevel{evoCol},
               Goal, Dream, Desire, ActiveTask, TaskDaysLeft,
               TaskRewardResId, TaskRewardAmt, Statistics, CharTraits,
               Specializations, Emotions, Needs, Memory, LocationId, LearnedTechIds
        FROM Npcs ORDER BY Id", _conn);

        using var rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            var npc = ReadNpcFast(rdr, hasEvo);
            npcs.Add(npc);
        }

        LoadAllModifiersBatch(npcs);

        lock (_npcCacheLock)
        {
            _npcCache.Clear();
            foreach (var npc in npcs)
                _npcCache[npc.Id] = npc;
            _lastNpcLoad = DateTime.Now;
        }

        totalSw.Stop();
        System.Diagnostics.Debug.WriteLine($"=== GetAllNpcsOptimized: {npcs.Count} NPC за {totalSw.ElapsedMilliseconds} мс ===");

        return npcs;
    }

    private Npc ReadNpcFast(SQLiteDataReader rdr, bool hasEvolutionLevel = true)
    {
        var npc = new Npc();

        // Используем GetOrdinal один раз для каждого поля
        var idOrdinal = rdr.GetOrdinal("Id");
        var nameOrdinal = rdr.GetOrdinal("Name");
        var ageOrdinal = rdr.GetOrdinal("Age");
        var genderOrdinal = rdr.GetOrdinal("Gender");
        var professionOrdinal = rdr.GetOrdinal("Profession");
        var descriptionOrdinal = rdr.GetOrdinal("Description");
        var healthOrdinal = rdr.GetOrdinal("Health");
        var devotionOrdinal = rdr.GetOrdinal("Devotion");
        var staminaOrdinal = rdr.GetOrdinal("Stamina");
        var energyOrdinal = rdr.GetOrdinal("Energy");
        var fearOrdinal = rdr.GetOrdinal("Fear");
        var trustOrdinal = rdr.GetOrdinal("Trust");
        var initiativeOrdinal = rdr.GetOrdinal("Initiative");
        var combatInitOrdinal = rdr.GetOrdinal("CombatInitiative");
        var traitOrdinal = rdr.GetOrdinal("Trait");
        var followerLevelOrdinal = rdr.GetOrdinal("FollowerLevel");
        int evolutionLevelOrdinal = hasEvolutionLevel ? rdr.GetOrdinal("EvolutionLevel") : -1;
        var goalOrdinal = rdr.GetOrdinal("Goal");
        var dreamOrdinal = rdr.GetOrdinal("Dream");
        var desireOrdinal = rdr.GetOrdinal("Desire");
        var activeTaskOrdinal = rdr.GetOrdinal("ActiveTask");
        var taskDaysLeftOrdinal = rdr.GetOrdinal("TaskDaysLeft");
        var taskRewardResIdOrdinal = rdr.GetOrdinal("TaskRewardResId");
        var taskRewardAmtOrdinal = rdr.GetOrdinal("TaskRewardAmt");
        var statisticsOrdinal = rdr.GetOrdinal("Statistics");
        var charTraitsOrdinal = rdr.GetOrdinal("CharTraits");
        var specializationsOrdinal = rdr.GetOrdinal("Specializations");
        var emotionsOrdinal = rdr.GetOrdinal("Emotions");
        var needsOrdinal = rdr.GetOrdinal("Needs");
        var memoryOrdinal = rdr.GetOrdinal("Memory");
        var locationIdOrdinal = rdr.GetOrdinal("LocationId");
        var learnedTechIdsOrdinal = rdr.GetOrdinal("LearnedTechIds");

        npc.Id = rdr.GetInt32(idOrdinal);
        npc.Name = rdr.GetString(nameOrdinal);
        npc.Age = rdr.GetInt32(ageOrdinal);

        string genderStr = rdr.IsDBNull(genderOrdinal) ? "Male" : rdr.GetString(genderOrdinal);
        npc.Gender = genderStr == "Female" ? Gender.Female : Gender.Male;

        npc.Profession = rdr.IsDBNull(professionOrdinal) ? "" : rdr.GetString(professionOrdinal);
        npc.Description = rdr.IsDBNull(descriptionOrdinal) ? "" : rdr.GetString(descriptionOrdinal);
        npc.Health = rdr.GetDouble(healthOrdinal);
        npc.Devotion = rdr.IsDBNull(devotionOrdinal) ? 20 : rdr.GetDouble(devotionOrdinal);
        npc.Stamina = rdr.IsDBNull(staminaOrdinal) ? 100 : rdr.GetDouble(staminaOrdinal);
        npc.Energy = rdr.IsDBNull(energyOrdinal) ? 50 : rdr.GetDouble(energyOrdinal);
        npc.Fear = rdr.IsDBNull(fearOrdinal) ? 10 : rdr.GetDouble(fearOrdinal);
        npc.Trust = rdr.IsDBNull(trustOrdinal) ? 50 : rdr.GetDouble(trustOrdinal);
        npc.Initiative = rdr.IsDBNull(initiativeOrdinal) ? 50 : rdr.GetDouble(initiativeOrdinal);
        npc.CombatInitiative = rdr.IsDBNull(combatInitOrdinal) ? 50 : rdr.GetDouble(combatInitOrdinal);

        string traitStr = rdr.IsDBNull(traitOrdinal) ? "None" : rdr.GetString(traitOrdinal);
        npc.Trait = traitStr switch
        {
            "Leader" => NpcTrait.Leader,
            "Coward" => NpcTrait.Coward,
            "Loner" => NpcTrait.Loner,
            _ => NpcTrait.None,
        };

        npc.FollowerLevel = rdr.IsDBNull(followerLevelOrdinal) ? 0 : rdr.GetInt32(followerLevelOrdinal);
        npc.EvolutionLevel = evolutionLevelOrdinal >= 0 && !rdr.IsDBNull(evolutionLevelOrdinal)
            ? rdr.GetInt32(evolutionLevelOrdinal) : 0;
        npc.Goal = rdr.IsDBNull(goalOrdinal) ? "" : rdr.GetString(goalOrdinal);
        npc.Dream = rdr.IsDBNull(dreamOrdinal) ? "" : rdr.GetString(dreamOrdinal);
        npc.Desire = rdr.IsDBNull(desireOrdinal) ? "" : rdr.GetString(desireOrdinal);
        npc.ActiveTask = rdr.IsDBNull(activeTaskOrdinal) ? "" : rdr.GetString(activeTaskOrdinal);
        npc.TaskDaysLeft = rdr.IsDBNull(taskDaysLeftOrdinal) ? 0 : rdr.GetInt32(taskDaysLeftOrdinal);
        npc.TaskRewardResId = rdr.IsDBNull(taskRewardResIdOrdinal) ? 0 : rdr.GetInt32(taskRewardResIdOrdinal);
        npc.TaskRewardAmt = rdr.IsDBNull(taskRewardAmtOrdinal) ? 0 : rdr.GetDouble(taskRewardAmtOrdinal);

        // Загрузка JSON полей (отложенная, только если нужно)
        string statsJson = rdr.IsDBNull(statisticsOrdinal) ? "{}" : rdr.GetString(statisticsOrdinal);
        try
        {
            var loadedStats = JsonSerializer.Deserialize<Statistics>(statsJson);
            if (loadedStats != null) npc.Stats = loadedStats;
        }
        catch { }

        // Модификаторы будут загружены позже
        npc.Needs = LoadJsonList<Need>(rdr, needsOrdinal);
        npc.Emotions = LoadJsonList<Emotion>(rdr, emotionsOrdinal);
        npc.Specializations = LoadJsonList<string>(rdr, specializationsOrdinal);
        npc.Memory = LoadJsonList<MemoryEntry>(rdr, memoryOrdinal);
        npc.LearnedTechIds = LoadJsonList<string>(rdr, learnedTechIdsOrdinal);

        var charTraitStrings = LoadJsonList<string>(rdr, charTraitsOrdinal);
        npc.CharTraits = new List<CharacterTrait>();
        foreach (var s in charTraitStrings)
        {
            if (Enum.TryParse<CharacterTrait>(s, out var ct))
                npc.CharTraits.Add(ct);
        }

        npc.LocationId = rdr.IsDBNull(locationIdOrdinal) ? 0 : rdr.GetInt32(locationIdOrdinal);

        return npc;
    }

    private List<T> LoadJsonList<T>(SQLiteDataReader rdr, int ordinal) where T : class
    {
        if (rdr.IsDBNull(ordinal)) return new List<T>();
        try
        {
            return JsonSerializer.Deserialize<List<T>>(rdr.GetString(ordinal)) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private void LoadAllModifiersBatch(List<Npc> npcs)
    {
        if (npcs.Count == 0) return;

        const int batchSize = 500;

        for (int i = 0; i < npcs.Count; i += batchSize)
        {
            var batch = npcs.Skip(i).Take(batchSize).ToList();
            var ids = string.Join(",", batch.Select(n => n.Id));

            using var cmd = new SQLiteCommand($"SELECT * FROM NpcModifiers WHERE NpcId IN ({ids}) ORDER BY NpcId", _conn);
            using var rdr = cmd.ExecuteReader();

            var modifiersByNpc = new Dictionary<int, List<(string StatId, Modifier Mod)>>();

            while (rdr.Read())
            {
                var npcId = rdr.GetInt32(rdr.GetOrdinal("NpcId"));
                var statId = rdr.GetString(rdr.GetOrdinal("StatId"));
                var modifierClass = rdr.GetString(rdr.GetOrdinal("ModifierClass"));

                if (!modifiersByNpc.ContainsKey(npcId))
                    modifiersByNpc[npcId] = new List<(string, Modifier)>();

                if (modifierClass == "Permanent")
                {
                    var mod = new PermanentModifier(
                        rdr.GetString(rdr.GetOrdinal("ModifierId")),
                        rdr.GetString(rdr.GetOrdinal("Name")),
                        rdr.GetString(rdr.GetOrdinal("Source")),
                        (ModifierType)rdr.GetInt32(rdr.GetOrdinal("ModifierType")),
                        rdr.GetDouble(rdr.GetOrdinal("Value"))
                    );
                    if (rdr.GetInt32(rdr.GetOrdinal("IsActive")) == 0)
                        mod.Deactivate();
                    modifiersByNpc[npcId].Add((statId, mod));
                }
                else if (modifierClass == "Independent")
                {
                    var mod = new IndependentModifier(
                        rdr.GetString(rdr.GetOrdinal("ModifierId")),
                        rdr.GetString(rdr.GetOrdinal("Name")),
                        rdr.GetString(rdr.GetOrdinal("Source")),
                        (ModifierType)rdr.GetInt32(rdr.GetOrdinal("ModifierType")),
                        rdr.GetDouble(rdr.GetOrdinal("Value")),
                        (TimeUnit)rdr.GetInt32(rdr.GetOrdinal("TimeUnit")),
                        rdr.GetInt32(rdr.GetOrdinal("Duration"))
                    );
                    modifiersByNpc[npcId].Add((statId, mod));
                }
            }

            // Применяем модификаторы к NPC в этой партии
            foreach (var npc in batch)
            {
                if (modifiersByNpc.TryGetValue(npc.Id, out var mods))
                {
                    foreach (var (statId, mod) in mods)
                    {
                        var stat = npc.Stats.GetById(statId);
                        stat?.AddModifier(mod);
                    }
                }
            }
        }
    }

}
