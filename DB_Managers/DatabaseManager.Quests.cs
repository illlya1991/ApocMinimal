using System.Data.SQLite;
using System.Text.Json;
using ApocMinimal.Models.PersonData.PlayerData;

namespace ApocMinimal.Database;

public partial class DatabaseManager
{
    public List<Quest> GetAllQuests()
    {
        var list = new List<Quest>();
        using var cmd = new SQLiteCommand("SELECT * FROM Quests ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            var q = new Quest();
            q.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
            q.Title = rdr.GetString(rdr.GetOrdinal("Title"));
            q.Description = rdr.GetString(rdr.GetOrdinal("Description"));
            string sourceStr = rdr.GetString(rdr.GetOrdinal("Source"));
            q.Source = sourceStr == "Player" ? QuestSource.Player : QuestSource.AI;
            string statusStr = rdr.GetString(rdr.GetOrdinal("Status"));
            q.Status = statusStr switch
            {
                "Available" => QuestStatus.Available,
                "Active"    => QuestStatus.Active,
                "Completed" => QuestStatus.Completed,
                _           => QuestStatus.Failed,
            };
            q.AssignedNpcId = rdr.GetInt32(rdr.GetOrdinal("AssignedNpcId"));
            q.DaysRequired = rdr.GetInt32(rdr.GetOrdinal("DaysRequired"));
            q.DaysRemaining = rdr.GetInt32(rdr.GetOrdinal("DaysRemaining"));
            q.RewardResourceId = rdr.GetInt32(rdr.GetOrdinal("RewardResourceId"));
            q.RewardAmount = rdr.GetDouble(rdr.GetOrdinal("RewardAmount"));
            q.OPCost = rdr.GetDouble(rdr.GetOrdinal("OPCost"));
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

    public void SaveQuest(Quest q)
    {
        using var cmd = new SQLiteCommand(@"
            UPDATE Quests SET Status=@st, AssignedNpcId=@an, DaysRemaining=@dr WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@st", q.Status.ToString());
        cmd.Parameters.AddWithValue("@an", q.AssignedNpcId);
        cmd.Parameters.AddWithValue("@dr", q.DaysRemaining);
        cmd.Parameters.AddWithValue("@id", q.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveQuestFull(Quest q)
    {
        if (q.Id == 0)
        {
            using var ins = new SQLiteCommand(@"
                INSERT INTO Quests (Title, Description, Source, Status, AssignedNpcId, DaysRequired, DaysRemaining,
                    RewardResourceId, RewardAmount, OPCost, QuestType, LibraryId,
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
            ins.Parameters.AddWithValue("@fc", q.OPCost);
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

    public List<QuestCatalogEntry> GetQuestCatalog(int altarLevel)
    {
        var list = new List<QuestCatalogEntry>();
        if (!IsTableExistsSafe("QuestCatalog")) return list;
        using var cmd = new SQLiteCommand("SELECT * FROM QuestCatalog WHERE MinTerminalLevel <= @al ORDER BY MinTerminalLevel, Id", _conn);
        cmd.Parameters.AddWithValue("@al", altarLevel);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            var entry = new QuestCatalogEntry
            {
                Id               = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Title            = rdr.GetString(rdr.GetOrdinal("Title")),
                Description      = GetStringOrDefault(rdr, "Description"),
                MinTerminalLevel = GetIntOrDefault(rdr, "MinTerminalLevel", 1),
                TakeCondStat     = GetStringOrDefault(rdr, "TakeCondStat"),
                TakeCondValue    = GetIntOrDefault(rdr, "TakeCondValue"),
                CompleteDays     = GetIntOrDefault(rdr, "CompleteDays", 3),
                CompleteResource = GetStringOrDefault(rdr, "CompleteResource"),
                CompleteAmount   = GetDoubleOrDefault(rdr, "CompleteAmount"),
                CompleteAction   = GetStringOrDefault(rdr, "CompleteAction"),
                RewardResource   = GetStringOrDefault(rdr, "RewardResource"),
                RewardAmount     = GetDoubleOrDefault(rdr, "RewardAmount"),
                RewardTechnique  = GetStringOrDefault(rdr, "RewardTechnique"),
                Category         = GetStringOrDefault(rdr, "Category"),
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
                Id             = rdr.GetInt32(rdr.GetOrdinal("Id")),
                SaveId         = GetStringOrDefault(rdr, "SaveId"),
                CatalogId      = GetIntOrDefault(rdr, "CatalogId"),
                PublishesLeft  = GetIntOrDefault(rdr, "PublishesLeft"),
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
            if (catalogMap.TryGetValue(list[i].CatalogId, out var cat))
                list[i].Catalog = cat;

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
                Id           = rdr.GetInt32(rdr.GetOrdinal("Id")),
                SaveId       = GetStringOrDefault(rdr, "SaveId"),
                CatalogId    = GetIntOrDefault(rdr, "CatalogId"),
                QuestTitle   = GetStringOrDefault(rdr, "QuestTitle"),
                NpcName      = GetStringOrDefault(rdr, "NpcName"),
                DayTaken     = GetIntOrDefault(rdr, "DayTaken"),
                DayCompleted = GetIntOrDefault(rdr, "DayCompleted"),
                RewardGiven  = GetStringOrDefault(rdr, "RewardGiven"),
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

    public void SetInitialQuests(string saveId)
    {
        var catalog = GetQuestCatalog(999);
        for (int i = 0; i < Math.Min(3, catalog.Count); i++)
            PurchaseQuest(saveId, catalog[i], QuestType.OneTime);
    }
}
