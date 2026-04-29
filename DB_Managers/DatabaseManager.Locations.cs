using System.Data.SQLite;
using System.Text.Json;
using ApocMinimal.Models.LocationData;

namespace ApocMinimal.Database;

public partial class DatabaseManager
{
    public List<Location> GetAllLocations()
    {
        var list = new List<Location>();
        using var cmd = new SQLiteCommand("SELECT * FROM Locations ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            var loc = new Location();
            loc.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
            loc.Name = rdr.GetString(rdr.GetOrdinal("Name"));

            string typeStr = rdr.GetString(rdr.GetOrdinal("Type"));
            loc.Type = typeStr switch
            {
                "City"       => LocationType.City,
                "District"   => LocationType.District,
                "Street"     => LocationType.Street,
                "Building"   => LocationType.Building,
                "Floor"      => LocationType.Floor,
                "Commercial" => LocationType.Commercial,
                _            => LocationType.Apartment,
            };

            loc.ParentId = rdr.GetInt32(rdr.GetOrdinal("ParentId"));

            string nodesJson = rdr.IsDBNull(rdr.GetOrdinal("ResourceNodes")) ? "{}"
                : rdr.GetString(rdr.GetOrdinal("ResourceNodes"));
            try { loc.ResourceNodes = JsonSerializer.Deserialize<Dictionary<string, double>>(nodesJson) ?? new(); }
            catch { loc.ResourceNodes = new(); }

            loc.DangerLevel = rdr.GetDouble(rdr.GetOrdinal("DangerLevel"));
            loc.IsExplored = rdr.GetInt32(rdr.GetOrdinal("IsExplored")) == 1;

            string statusStr = GetStringOrDefault(rdr, "Status", "Dangerous");
            loc.Status = statusStr == "Cleared" ? LocationStatus.Cleared : LocationStatus.Dangerous;

            loc.MonsterTypeName = GetStringOrDefault(rdr, "MonsterTypeName");

            string mapStateStr = GetStringOrDefault(rdr, "MapState", "Current");
            loc.MapState = mapStateStr switch
            {
                "Template"  => MapState.Template,
                "ApocStart" => MapState.ApocStart,
                _           => MapState.Current,
            };

            string commercialTypeStr = GetStringOrDefault(rdr, "CommercialType", "None");
            loc.CommercialType = commercialTypeStr switch
            {
                "Shop"        => CommercialType.Shop,
                "Supermarket" => CommercialType.Supermarket,
                "Mall"        => CommercialType.Mall,
                "Market"      => CommercialType.Market,
                "Hairdresser" => CommercialType.Hairdresser,
                "BeautySalon" => CommercialType.BeautySalon,
                "Pharmacy"    => CommercialType.Pharmacy,
                "Hospital"    => CommercialType.Hospital,
                "Factory"     => CommercialType.Factory,
                "Hotel"       => CommercialType.Hotel,
                _             => CommercialType.None,
            };

            list.Add(loc);
        }
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
