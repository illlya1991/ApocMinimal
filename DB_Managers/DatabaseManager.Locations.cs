using System.Data.SQLite;
using System.Text.Json;
using ApocMinimal.Models.LocationData;

namespace ApocMinimal.Database;

public partial class DatabaseManager
{
    

    public List<Location> GetAllLocations()
    {
        lock (_locationCacheLock)
        {
            if (_locationCache.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"  Локации: возвращено из кэша {_locationCache.Count} локаций");
                return _locationCache.Values.ToList();
            }
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var list = new List<Location>();

        using var cmd = new SQLiteCommand("SELECT * FROM Locations ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            var loc = new Location();
            loc.Id       = rdr.GetInt32(rdr.GetOrdinal("Id"));
            loc.Name     = rdr.GetString(rdr.GetOrdinal("Name"));
            loc.ParentId = GetIntOrDefault(rdr, "ParentId", 0);

            string typeStr = GetStringOrDefault(rdr, "Type", "Building");
            loc.Type = Enum.TryParse<LocationType>(typeStr, out var lt) ? lt : LocationType.Building;

            string msStr = GetStringOrDefault(rdr, "MapState", "Current");
            loc.MapState = Enum.TryParse<MapState>(msStr, out var ms) ? ms : MapState.Current;

            string ctStr = GetStringOrDefault(rdr, "CommercialType", "None");
            loc.CommercialType = Enum.TryParse<CommercialType>(ctStr, out var ct) ? ct : CommercialType.None;

            loc.MonsterTypeName = GetStringOrDefault(rdr, "MonsterTypeName", "");

            // ResourceNodes — JSON dict (через свойство — устанавливает IsDirty, очистим ниже)
            string rnJson = GetStringOrDefault(rdr, "ResourceNodes", "{}");
            try
            {
                var nodes = JsonSerializer.Deserialize<Dictionary<string, double>>(rnJson, JsonOpts);
                loc.ResourceNodes = nodes ?? new();
            }
            catch { loc.ResourceNodes = new(); }

            loc.DangerLevel = GetDoubleOrDefault(rdr, "DangerLevel", 0);
            loc.IsExplored  = GetIntOrDefault(rdr, "IsExplored", 0) == 1;

            string statusStr = GetStringOrDefault(rdr, "Status", "Dangerous");
            loc.Status = Enum.TryParse<LocationStatus>(statusStr, out var ls) ? ls : LocationStatus.Dangerous;

            loc.ClearDirty();
            list.Add(loc);
        }

        lock (_locationCacheLock)
        {
            _locationCache.Clear();
            foreach (var loc in list)
                _locationCache[loc.Id] = loc;
        }

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"  Локаций: {list.Count} за {sw.ElapsedMilliseconds} мс");
        return list;
    }
    public void DeleteAllLocations() => ExecuteNQ("DELETE FROM Locations");

    public int InsertLocation(Location loc)
    {
        using var cmd = new SQLiteCommand(@"
        INSERT INTO Locations (Name, Type, ParentId, ResourceNodes, DangerLevel, IsExplored, Status, MonsterTypeName, MapState, CommercialType)
        VALUES (@nm, @ty, @pi, @rn, @dl, @ie, @st, @mt, @ms, @ct)", _conn);
        cmd.Parameters.AddWithValue("@nm", loc.Name);
        cmd.Parameters.AddWithValue("@ty", loc.Type.ToString());
        cmd.Parameters.AddWithValue("@pi", loc.ParentId);
        cmd.Parameters.AddWithValue("@rn", JsonSerializer.Serialize(loc.ResourceNodes, JsonOpts));
        cmd.Parameters.AddWithValue("@dl", loc.DangerLevel);
        cmd.Parameters.AddWithValue("@ie", loc.IsExplored ? 1 : 0);
        cmd.Parameters.AddWithValue("@st", loc.Status.ToString());
        cmd.Parameters.AddWithValue("@mt", loc.MonsterTypeName);
        cmd.Parameters.AddWithValue("@ms", loc.MapState.ToString());
        cmd.Parameters.AddWithValue("@ct", loc.CommercialType.ToString());
        cmd.ExecuteNonQuery();
        return (int)_conn.LastInsertRowId;
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

    public void SaveLocationsBatch(IEnumerable<Location> locations)
    {
        var locList = locations.ToList();
        if (locList.Count == 0) return;

        using var transaction = _conn.BeginTransaction();
        try
        {
            foreach (var loc in locList)
            {
                using var cmd = new SQLiteCommand(
                    "UPDATE Locations SET ResourceNodes=@rn, DangerLevel=@dl, IsExplored=@ie, Status=@st WHERE Id=@id",
                    _conn, transaction);
                cmd.Parameters.AddWithValue("@rn", JsonSerializer.Serialize(loc.ResourceNodes, JsonOpts));
                cmd.Parameters.AddWithValue("@dl", loc.DangerLevel);
                cmd.Parameters.AddWithValue("@ie", loc.IsExplored ? 1 : 0);
                cmd.Parameters.AddWithValue("@st", loc.Status.ToString());
                cmd.Parameters.AddWithValue("@id", loc.Id);
                cmd.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }

    public void SetInitialResources(string saveId)
    {
        ExecuteNQ("UPDATE Resources SET Amount=10 WHERE Name='Еда'");
        ExecuteNQ("UPDATE Resources SET Amount=10 WHERE Name='Вода'");
        ExecuteNQ("UPDATE Resources SET Amount=1 WHERE Name NOT IN ('Еда','Вода') AND Id IN (SELECT Id FROM Resources WHERE Name NOT IN ('Еда','Вода') LIMIT 10)");
    }
}
