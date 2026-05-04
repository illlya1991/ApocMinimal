using System.Data.SQLite;
using System.Text.Json;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;

namespace ApocMinimal.Database;

public partial class DatabaseManager
{
    public Player? GetPlayer()
    {
        using var cmd = new SQLiteCommand("SELECT * FROM Player WHERE Id = 1 LIMIT 1", _conn);
        using var rdr = cmd.ExecuteReader();
        return rdr.Read() ? ReadPlayer(rdr) : null;
    }

    public void SavePlayer(Player p)
    {
        string locCol = ",LocationId=@loc";

        using var cmd = new SQLiteCommand(
            $"UPDATE Player SET Name=@pn,Faction=@fc,DevPoints=@fp,TerminalLevel=@al,CurrentDay=@cd," +
            $"BarrierLevel=@bl,TerritoryControl=@tc,PlayerActionsToday=@pa,ControlledZoneIds=@cz,FactionCoeffs=@fcoeffs{locCol} WHERE Id=@id", _conn);
        cmd.Parameters.AddWithValue("@fp", p.DevPoints);
        cmd.Parameters.AddWithValue("@al", p.TerminalLevel);
        cmd.Parameters.AddWithValue("@fc", p.Faction.ToString());
        cmd.Parameters.AddWithValue("@cd", p.CurrentDay);
        cmd.Parameters.AddWithValue("@bl", p.BarrierLevel);
        cmd.Parameters.AddWithValue("@tc", p.TerritoryControl);
        cmd.Parameters.AddWithValue("@pa", p.PlayerActionsToday);
        cmd.Parameters.AddWithValue("@pn", p.Name);
        cmd.Parameters.AddWithValue("@cz", JsonSerializer.Serialize(p.ControlledZoneIds, JsonOpts));
        cmd.Parameters.AddWithValue("@fcoeffs", JsonSerializer.Serialize(p.FactionCoeffs, JsonOpts));
        cmd.Parameters.AddWithValue("@loc", p.LocationId);
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.ExecuteNonQuery();
    }

    private Player ReadPlayer(SQLiteDataReader rdr)
    {
        var p = new Player();
        p.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
        p.Name = rdr.GetString(rdr.GetOrdinal("Name"));
        p.DevPoints = GetDoubleOrDefault(rdr, "DevPoints");
        p.TerminalLevel = GetIntOrDefault(rdr, "TerminalLevel", 1);
        string factionStr = GetStringOrDefault(rdr, "Faction", "ElementMages");
        p.Faction = Enum.TryParse<PlayerFaction>(factionStr, out var f) ? f : PlayerFaction.ElementMages;
        p.CurrentDay = rdr.GetInt32(rdr.GetOrdinal("CurrentDay"));
        p.BarrierLevel = GetIntOrDefault(rdr, "BarrierLevel", 1);
        p.TerritoryControl = GetIntOrDefault(rdr, "TerritoryControl");
        p.PlayerActionsToday = GetIntOrDefault(rdr, "PlayerActionsToday");
        p.LocationId = GetIntOrDefault(rdr, "LocationId", 1);
        string zonesJson = GetStringOrDefault(rdr, "ControlledZoneIds", "[]");
        try { p.ControlledZoneIds = JsonSerializer.Deserialize<List<int>>(zonesJson) ?? new(); }
        catch 
        { 
            p.ControlledZoneIds = new();
            p.ControlledZoneIds.Add(p.LocationId);
            p.TerritoryControl = p.ControlledZoneIds.Count;
        }
        string coeffsJson = GetStringOrDefault(rdr, "FactionCoeffs", "{}");
        try { p.FactionCoeffs = JsonSerializer.Deserialize<FactionCoefficients>(coeffsJson) ?? new(); }
        catch { p.FactionCoeffs = new(); }
        return p;
    }
}
