using System.Data.SQLite;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Database;

public partial class DatabaseManager
{
    public List<Resource> GetAllResources()
    {
        var list = new List<Resource>();
        using var cmd = new SQLiteCommand("SELECT * FROM Resources ORDER BY Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            list.Add(new Resource
            {
                Id       = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Name     = rdr.GetString(rdr.GetOrdinal("Name")),
                Amount   = rdr.GetDouble(rdr.GetOrdinal("Amount")),
                Category = rdr.GetString(rdr.GetOrdinal("Category")),
            });
        }
        return list;
    }

    public void SaveResource(Resource r)
    {
        using var cmd = new SQLiteCommand("UPDATE Resources SET Amount=@a WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@a", r.Amount);
        cmd.Parameters.AddWithValue("@id", r.Id);
        cmd.ExecuteNonQuery();
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
                Id              = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Name            = rdr.GetString(rdr.GetOrdinal("Name")),
                Category        = rdr.GetString(rdr.GetOrdinal("Category")),
                Rarity          = GetStringOrDefault(rdr, "Rarity", "Common"),
                Unit            = GetStringOrDefault(rdr, "Unit", "шт"),
                Weight          = GetDoubleOrDefault(rdr, "Weight", 0.5),
                SpoilageDays    = GetIntOrDefault(rdr, "SpoilageDays", 0),
                FoodRestore     = GetDoubleOrDefault(rdr, "FoodRestore", 0),
                WaterRestore    = GetDoubleOrDefault(rdr, "WaterRestore", 0),
                IsLocationNode  = GetIntOrDefault(rdr, "IsLocationNode", 1) == 1,
                LocationWeight  = GetIntOrDefault(rdr, "LocationWeight", 1),
                Quality         = GetIntOrDefault(rdr, "Quality", 1),
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
        using var cmd = new SQLiteCommand("INSERT OR IGNORE INTO ResourceShop(SaveId, ResourceName) VALUES(@s,@r)", _conn);
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
        using var cmd = new SQLiteCommand("INSERT OR IGNORE INTO AppliedExchanges(SaveId,ExchangeId) VALUES(@s,@e)", _conn);
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
