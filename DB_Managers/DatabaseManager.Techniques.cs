using System.Data.SQLite;
using System.Text.Json;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.Database;

public partial class DatabaseManager
{
    private static readonly JsonSerializerOptions JsonOptsPolymorphic = new() { WriteIndented = false };

    public List<Technique> GetAllTechniques()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var list = new List<Technique>();
        using var cmd = new SQLiteCommand("SELECT * FROM Techniques ORDER BY TerminalLevel, Id", _conn);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(ReadTechnique(rdr));

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"GetAllTechniques: {list.Count} техник за {sw.ElapsedMilliseconds} мс");
        return list;
    }

    public void InsertTechnique(Technique t)
    {
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Techniques
              (Name,Description,TerminalLevel,TechLevel,TechType,OPCost,EnergyCost,StaminaCost,
               RequiredStats,HealAmount,Faction,CatalogKey,ActivationModes)
            VALUES (@nm,@ds,@al,@tl,@tt,@fc,@cc,@sc,@rs,@ha,@fn,@ck,@am)", _conn);
        BindTechnique(cmd, t);
        cmd.ExecuteNonQuery();
        t.Id = (int)_conn.LastInsertRowId;
    }

    private void BindTechnique(SQLiteCommand cmd, Technique t)
    {
        cmd.Parameters.AddWithValue("@nm", t.Name);
        cmd.Parameters.AddWithValue("@ds", t.Description);
        cmd.Parameters.AddWithValue("@al", t.TerminalLevel);
        cmd.Parameters.AddWithValue("@tl", t.TechLevel.ToString());
        cmd.Parameters.AddWithValue("@tt", t.TechType.ToString());
        cmd.Parameters.AddWithValue("@fc", t.OPCost);
        cmd.Parameters.AddWithValue("@cc", t.EnergyCost);
        cmd.Parameters.AddWithValue("@sc", t.StaminaCost);
        cmd.Parameters.AddWithValue("@rs", JsonSerializer.Serialize(t.RequiredStats, JsonOpts));
        cmd.Parameters.AddWithValue("@ha", t.HealAmount);
        cmd.Parameters.AddWithValue("@fn", t.Faction);
        cmd.Parameters.AddWithValue("@ck", t.CatalogKey);
        cmd.Parameters.AddWithValue("@am", JsonSerializer.Serialize(t.ActivationModes, JsonOptsPolymorphic));
    }

    public List<Technique> GetTechniquesByFaction(string faction, int maxTerminalLevel)
    {
        var list = new List<Technique>();
        using var cmd = new SQLiteCommand(
            "SELECT * FROM Techniques WHERE (Faction=@fn OR Faction='') AND TerminalLevel<=@lvl ORDER BY TerminalLevel,TechType,Id", _conn);
        cmd.Parameters.AddWithValue("@fn", faction);
        cmd.Parameters.AddWithValue("@lvl", maxTerminalLevel);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(ReadTechnique(rdr));
        return list;
    }

    public void SaveTechnique(Technique t)
    {
        using var cmd = new SQLiteCommand(
            "UPDATE Techniques SET Name=@nm,Description=@ds,TerminalLevel=@al,TechLevel=@tl," +
            "TechType=@tt,OPCost=@fc,EnergyCost=@cc,StaminaCost=@sc,RequiredStats=@rs," +
            "HealAmount=@ha,Faction=@fn,CatalogKey=@ck,ActivationModes=@am WHERE Id=@id", _conn);
        BindTechnique(cmd, t);
        cmd.Parameters.AddWithValue("@id", t.Id);
        cmd.ExecuteNonQuery();
    }

    public Technique? GetTechniqueById(int id)
    {
        using var cmd = new SQLiteCommand("SELECT * FROM Techniques WHERE Id = @id", _conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var rdr = cmd.ExecuteReader();
        return rdr.Read() ? ReadTechnique(rdr) : null;
    }

    public List<Technique> GetTechniquesByTerminalLevel(int terminalLevel)
    {
        var list = new List<Technique>();
        using var cmd = new SQLiteCommand("SELECT * FROM Techniques WHERE TerminalLevel <= @level ORDER BY TerminalLevel, Id", _conn);
        cmd.Parameters.AddWithValue("@level", terminalLevel);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(ReadTechnique(rdr));
        return list;
    }

    // ── Tech inventory ──────────────────────────────────────────────────────

    public List<string> GetTechInventory(string saveId)
    {
        var list = new List<string>();
        using var cmd = new SQLiteCommand("SELECT TechKey FROM PlayerTechInventory WHERE SaveId=@s", _conn);
        cmd.Parameters.AddWithValue("@s", saveId);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(rdr.GetString(0));
        return list;
    }

    public void AddTechInventoryItem(string saveId, string techKey)
    {
        using var cmd = new SQLiteCommand("INSERT INTO PlayerTechInventory (SaveId,TechKey) VALUES (@s,@k)", _conn);
        cmd.Parameters.AddWithValue("@s", saveId);
        cmd.Parameters.AddWithValue("@k", techKey);
        cmd.ExecuteNonQuery();
    }

    public void RemoveTechInventoryItem(string saveId, string techKey)
    {
        using var cmd = new SQLiteCommand(
            "DELETE FROM PlayerTechInventory WHERE SaveId=@s AND TechKey=@k AND Id=(" +
            "SELECT Id FROM PlayerTechInventory WHERE SaveId=@s AND TechKey=@k LIMIT 1)", _conn);
        cmd.Parameters.AddWithValue("@s", saveId);
        cmd.Parameters.AddWithValue("@k", techKey);
        cmd.ExecuteNonQuery();
    }

    private static Technique ReadTechnique(SQLiteDataReader rdr)
    {
        var t = new Technique();
        t.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
        t.Name = rdr.GetString(rdr.GetOrdinal("Name"));
        t.Description = GetStringOrDefault(rdr, "Description");
        t.TerminalLevel = rdr.GetInt32(rdr.GetOrdinal("TerminalLevel"));

        string tl = GetStringOrDefault(rdr, "TechLevel", "Initiate");
        t.TechLevel = tl switch
        {
            "Adept" => TechniqueLevel.Adept,
            "Warrior" => TechniqueLevel.Warrior,
            "Veteran" => TechniqueLevel.Veteran,
            "Master" => TechniqueLevel.Master,
            "GrandMaster" => TechniqueLevel.GrandMaster,
            "Phantom" => TechniqueLevel.Phantom,
            "Legend" => TechniqueLevel.Legend,
            "Vessel" => TechniqueLevel.Vessel,
            "Apex" => TechniqueLevel.Apex,
            _ => TechniqueLevel.Initiate,
        };

        string tt = GetStringOrDefault(rdr, "TechType", "Energy");
        t.TechType = tt switch
        {
            "Physical" => TechniqueType.Physical,
            "Mental" => TechniqueType.Mental,
            _ => TechniqueType.Energy,
        };

        t.OPCost = GetDoubleOrDefault(rdr, "OPCost");
        t.EnergyCost = GetDoubleOrDefault(rdr, "EnergyCost");
        t.StaminaCost = GetDoubleOrDefault(rdr, "StaminaCost");
        t.HealAmount = GetDoubleOrDefault(rdr, "HealAmount");
        t.RequiredStats = DeserializeOrDefault<Dictionary<int, double>>(rdr, "RequiredStats") ?? new();
        t.Faction = GetStringOrDefault(rdr, "Faction");
        t.CatalogKey = GetStringOrDefault(rdr, "CatalogKey");

        // ⚡ КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: не пытаемся десериализовать битые данные
        try
        {
            string amJson = GetStringOrDefault(rdr, "ActivationModes", "[]");

            // Проверка на пустую строку или null
            if (string.IsNullOrWhiteSpace(amJson) || amJson == "null")
            {
                t.ActivationModes = new List<ActivationMode>();
            }
            else
            {
                // Пробуем десериализовать с обработкой ошибок
                var modes = JsonSerializer.Deserialize<List<ActivationMode>>(amJson, JsonOptsPolymorphic);
                t.ActivationModes = modes ?? new List<ActivationMode>();
            }
        }
        catch (Exception ex)
        {
            // Подавляем ошибку и не выводим тысячи сообщений
            // System.Diagnostics.Debug.WriteLine($"  Ошибка десериализации {t.Name}: {ex.Message}");
            t.ActivationModes = new List<ActivationMode>();
        }

        return t;
    }
    public List<Technique> GetAllTechniquesFast()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var list = new List<Technique>();

        using var cmd = new SQLiteCommand(
            "SELECT Id, Name, Description, TerminalLevel, TechLevel, TechType, OPCost, EnergyCost, StaminaCost, RequiredStats, HealAmount, Faction, CatalogKey FROM Techniques ORDER BY TerminalLevel, Id",
            _conn);
        using var rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            var t = new Technique();
            t.Id = rdr.GetInt32(rdr.GetOrdinal("Id"));
            t.Name = rdr.GetString(rdr.GetOrdinal("Name"));
            t.Description = GetStringOrDefault(rdr, "Description");
            t.TerminalLevel = rdr.GetInt32(rdr.GetOrdinal("TerminalLevel"));

            string tl = GetStringOrDefault(rdr, "TechLevel", "Initiate");
            t.TechLevel = tl switch
            {
                "Adept" => TechniqueLevel.Adept,
                "Warrior" => TechniqueLevel.Warrior,
                "Veteran" => TechniqueLevel.Veteran,
                "Master" => TechniqueLevel.Master,
                "GrandMaster" => TechniqueLevel.GrandMaster,
                "Phantom" => TechniqueLevel.Phantom,
                "Legend" => TechniqueLevel.Legend,
                "Vessel" => TechniqueLevel.Vessel,
                "Apex" => TechniqueLevel.Apex,
                _ => TechniqueLevel.Initiate,
            };

            string tt = GetStringOrDefault(rdr, "TechType", "Energy");
            t.TechType = tt switch
            {
                "Physical" => TechniqueType.Physical,
                "Mental" => TechniqueType.Mental,
                _ => TechniqueType.Energy,
            };

            t.OPCost = GetDoubleOrDefault(rdr, "OPCost");
            t.EnergyCost = GetDoubleOrDefault(rdr, "EnergyCost");
            t.StaminaCost = GetDoubleOrDefault(rdr, "StaminaCost");
            t.HealAmount = GetDoubleOrDefault(rdr, "HealAmount");
            t.RequiredStats = DeserializeOrDefault<Dictionary<int, double>>(rdr, "RequiredStats") ?? new();
            t.Faction = GetStringOrDefault(rdr, "Faction");
            t.CatalogKey = GetStringOrDefault(rdr, "CatalogKey");

            // Пропускаем ActivationModes при быстрой загрузке
            t.ActivationModes = new List<ActivationMode>();

            list.Add(t);
        }

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"GetAllTechniquesFast: {list.Count} техник за {sw.ElapsedMilliseconds} мс");
        return list;
    }
}
