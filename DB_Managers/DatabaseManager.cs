using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.Systems;

namespace ApocMinimal.Database;

public partial class DatabaseManager
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
            _ListSaves.Add(new OneSave(Path.Combine(savesPath, $"apocSave_{i}.db")));

        _thisSave = _ListSaves[0];
        InitializeDatabase();
    }

    public List<OneSave> ListSaves => _ListSaves;
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
                    if (scalarResult != null) currentDay = (long)scalarResult;
                    item._active = currentDay > 1;
                    item._currentDay = (int)currentDay;

                    object? altarResult = ExecuteScalar("SELECT TerminalLevel FROM Player Limit 1");
                    if (altarResult != null) item._terminalLevel = (int)(long)altarResult;

                    object? faithResult = ExecuteScalar("SELECT DevPoints FROM Player Limit 1");
                    if (faithResult != null) item._devPoints = Convert.ToDouble(faithResult);
                }
            }
            catch { }
        }
        OpenConnection("");
    }

    public bool HasAnyActiveSave()
    {
        if (_ListSaves == null) return false;
        for (int i = 0; i < _ListSaves.Count; i++)
            if (_ListSaves[i]._active) return true;
        return false;
    }

    public void OpenCurrentSave()
    {
        if (!string.IsNullOrEmpty(_thisSave._connectionString))
            OpenConnection(_thisSave._connectionString);
    }

    public void DeleteSave(OneSave value)
    {
        CloseDatabaseConnections();
        if (File.Exists(value._fileName)) File.Delete(value._fileName);
        InitializeDatabase();
    }

    public void ResetDatabase(Action<int, string, string>? progressCallback = null)
    {
        void Report(int p, string s, string d = "") => progressCallback?.Invoke(p, s, d);

        Report(0, "Проверка шаблона...");
        if (!File.Exists(_templateSave._fileName))
            throw new FileNotFoundException($"Файл шаблону не знайдено: {_templateSave._fileName}");

        Report(5, "Закрытие соединений...");
        CloseDatabaseConnections();

        Report(10, "Создание файла сохранения...");
        if (File.Exists(_thisSave._fileName)) File.Delete(_thisSave._fileName);
        File.Copy(_templateSave._fileName, _thisSave._fileName);

        Report(15, "Подключение к базе...");
        OpenConnection(_thisSave._connectionString);

        Report(20, "Проверка схемы БД...");
        EnsureNpcLocationColumn();
        EnsureNpcModifiersSchema();
        EnsurePlayerSchema();

        Report(30, "Загрузка локаций...");
        var locations = GetAllLocations();
        var rnd = new Random();

        Report(40, "Генерация ресурсов...", $"Обработано 0 из {locations.Count} локаций");
        MapInitializer.InitialiseMapResources(locations, rnd, (current, total) =>
        {
            if (current % 100 == 0 || current == total)
            {
                int percent = 40 + (int)(current * 30.0 / total);
                Report(percent, "Распределение ресурсов...", $"Обработано {current} из {total} локаций");
            }
        });

        Report(75, "Сохранение локаций...");
        SaveLocationsBatch(locations);

        Report(80, "Сброс флагов изменений...");
        foreach (var loc in locations) loc.ClearDirty();

        Report(85, "Размещение NPC...");
        object? startIdObj = ExecuteScalar("SELECT MIN(Id) FROM Locations WHERE Type='Floor' AND IsExplored=1");
        int startLocId = startIdObj is long sl ? (int)sl : 1;
        ExecuteNQ($"UPDATE Npcs SET LocationId={startLocId}");

        Report(90, "Начальные ресурсы...");
        SetInitialResources(CurrentSaveId);

        Report(95, "Начальные квесты...");
        SetInitialQuests(CurrentSaveId);

        Report(100, "Готово!");
    }

    // ── Schema migrations ────────────────────────────────────────────────────

    private void EnsureNpcLocationColumn()
    {
        try { ExecuteNQ("ALTER TABLE Npcs ADD COLUMN LocationId INTEGER NOT NULL DEFAULT 0"); } catch { }
    }

    public void EnsureNpcModifiersSchema()
    {
        string[] alters =
        {
            "ALTER TABLE NpcModifiers ADD COLUMN ModifierClass TEXT NOT NULL DEFAULT 'Permanent'",
            "ALTER TABLE NpcModifiers ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1",
            "ALTER TABLE NpcModifiers ADD COLUMN TimeUnit INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE NpcModifiers ADD COLUMN Duration INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE NpcModifiers ADD COLUMN Remaining INTEGER NOT NULL DEFAULT 0",
        };
        foreach (var sql in alters) { try { ExecuteNQ(sql); } catch { } }
    }

    public void EnsurePlayerSchema()
    {
        string[] alters =
        {
            "ALTER TABLE Player ADD COLUMN BarrierLevel INTEGER NOT NULL DEFAULT 1",
            "ALTER TABLE Player ADD COLUMN ControlledZoneIds TEXT NOT NULL DEFAULT '[]'",
            "ALTER TABLE Player ADD COLUMN FactionCoeffs TEXT NOT NULL DEFAULT '{}'",
        };
        foreach (var sql in alters) { try { ExecuteNQ(sql); } catch { } }
        EnsureFactionCoeffsInGameConfig();
    }

    public void EnsureFactionCoeffsInGameConfig()
    {
        if (!IsTableExistsSafe("GameConfig")) return;
        var rows = new (string Key, string Value)[]
        {
            ("faction_ElementMages_dev_per_npc",      "1.15"),
            ("faction_ElementMages_dev_per_location", "0"),
            ("faction_ElementMages_donation",         "1.15"),
            ("faction_ElementMages_terminal_cost",    "1"),
            ("faction_ElementMages_stat_growth",      "1"),
            ("faction_ElementMages_shop_cost",        "1"),
            ("faction_ElementMages_max_dev_per_npc",  "1"),
            ("faction_ElementMages_barrier_units",    "1"),
            ("faction_PathBlades_dev_per_npc",        "1"),
            ("faction_PathBlades_dev_per_location",   "0"),
            ("faction_PathBlades_donation",           "1"),
            ("faction_PathBlades_terminal_cost",      "1.25"),
            ("faction_PathBlades_stat_growth",        "1"),
            ("faction_PathBlades_shop_cost",          "1"),
            ("faction_PathBlades_max_dev_per_npc",    "1"),
            ("faction_PathBlades_barrier_units",      "1"),
            ("faction_MirrorHealers_dev_per_npc",     "1"),
            ("faction_MirrorHealers_dev_per_location","0"),
            ("faction_MirrorHealers_donation",        "1"),
            ("faction_MirrorHealers_terminal_cost",   "1"),
            ("faction_MirrorHealers_stat_growth",     "1"),
            ("faction_MirrorHealers_shop_cost",       "0.8"),
            ("faction_MirrorHealers_max_dev_per_npc", "1.1"),
            ("faction_MirrorHealers_barrier_units",   "1"),
            ("faction_DeepSmiths_dev_per_npc",        "1"),
            ("faction_DeepSmiths_dev_per_location",   "0"),
            ("faction_DeepSmiths_donation",           "1"),
            ("faction_DeepSmiths_terminal_cost",      "1"),
            ("faction_DeepSmiths_stat_growth",        "1"),
            ("faction_DeepSmiths_shop_cost",          "0.75"),
            ("faction_DeepSmiths_max_dev_per_npc",    "1"),
            ("faction_DeepSmiths_barrier_units",      "1.25"),
            ("faction_GuardHeralds_dev_per_npc",      "0.9"),
            ("faction_GuardHeralds_dev_per_location", "2"),
            ("faction_GuardHeralds_donation",         "1"),
            ("faction_GuardHeralds_terminal_cost",    "1"),
            ("faction_GuardHeralds_stat_growth",      "1"),
            ("faction_GuardHeralds_shop_cost",        "1"),
            ("faction_GuardHeralds_max_dev_per_npc",  "1"),
            ("faction_GuardHeralds_barrier_units",    "1"),
        };
        using var tx = _conn.BeginTransaction();
        foreach (var (key, val) in rows)
        {
            using var cmd = new SQLiteCommand("INSERT OR IGNORE INTO GameConfig (Key, Value) VALUES (@k, @v)", _conn, tx);
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", val);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public void ApplyFactionCoefficients(Player player)
    {
        var config = GetGameConfig();
        player.FactionCoeffs = FactionCoefficients.ForFaction(player.Faction, config);
    }

    public void EnsureNpcTechSchema()
    {
        string[] alters =
        {
            "ALTER TABLE Npcs ADD COLUMN LearnedTechIds TEXT NOT NULL DEFAULT '[]'",
            "ALTER TABLE Techniques ADD COLUMN ActivationModes TEXT NOT NULL DEFAULT '[]'",
        };
        foreach (var sql in alters) { try { ExecuteNQ(sql); } catch { } }
        ExecuteNQ(@"
            CREATE TABLE IF NOT EXISTS PlayerTechInventory (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                SaveId   TEXT    NOT NULL,
                TechKey  TEXT    NOT NULL
            )");
    }

    // ── GameLog ──────────────────────────────────────────────────────────────

    public void EnsureGameLogTable()
    {
        ExecuteNQ(@"
            CREATE TABLE IF NOT EXISTS GameLog (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                SaveId    TEXT    NOT NULL,
                DayNumber INTEGER NOT NULL,
                Section   TEXT    NOT NULL,
                Text      TEXT    NOT NULL,
                Color     TEXT    NOT NULL DEFAULT '#c9d1d9',
                IsAction  INTEGER NOT NULL DEFAULT 0
            )");
        try { ExecuteNQ("CREATE INDEX IF NOT EXISTS idx_gamelog_save_day ON GameLog(SaveId, DayNumber)"); } catch { }
    }

    public void SaveDayLog(string saveId, int dayNumber, IEnumerable<(string section, string text, string color, bool isAction)> entries)
    {
        ExecuteNQ($"DELETE FROM GameLog WHERE SaveId='{saveId}' AND DayNumber={dayNumber}");
        using var tx = _conn.BeginTransaction();
        using var cmd = new SQLiteCommand(
            "INSERT INTO GameLog (SaveId,DayNumber,Section,Text,Color,IsAction) VALUES (@s,@d,@sec,@t,@c,@a)", _conn, tx);
        foreach (var (section, text, color, isAction) in entries)
        {
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@s",   saveId);
            cmd.Parameters.AddWithValue("@d",   dayNumber);
            cmd.Parameters.AddWithValue("@sec", section);
            cmd.Parameters.AddWithValue("@t",   text);
            cmd.Parameters.AddWithValue("@c",   color);
            cmd.Parameters.AddWithValue("@a",   isAction ? 1 : 0);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public List<(int DayNumber, string Section, string Text, string Color, bool IsAction)> GetAllLogs(string saveId)
    {
        var result = new List<(int, string, string, string, bool)>();
        using var cmd = new SQLiteCommand(
            "SELECT DayNumber,Section,Text,Color,IsAction FROM GameLog WHERE SaveId=@s ORDER BY DayNumber,Id", _conn);
        cmd.Parameters.AddWithValue("@s", saveId);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
            result.Add((rdr.GetInt32(0), rdr.GetString(1), rdr.GetString(2), rdr.GetString(3), rdr.GetInt32(4) == 1));
        return result;
    }

    // ── Infrastructure helpers ───────────────────────────────────────────────

    public bool IsTableExistsSafe(string tableName = "Player")
    {
        using var command = new SQLiteCommand(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name", _conn);
        command.Parameters.AddWithValue("@name", tableName);
        return (long)command.ExecuteScalar() > 0;
    }

    private void OpenConnection(string ThisConn)
    {
        if (ThisConn == "")
        {
            if (_conn != null)
            {
                if (_conn.State == ConnectionState.Open) _conn.Close();
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
                if (_conn.State == ConnectionState.Open) _conn.Close();
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

    private void CloseDatabaseConnections()
    {
        if (_conn != null)
        {
            if (_conn.State == ConnectionState.Open) _conn.Close();
            _conn.Dispose();
        }
        _conn = new SQLiteConnection();
        GC.Collect();
        GC.WaitForPendingFinalizers();
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
}

public class OneSave
{
    public string _connectionString = "";
    public string _fileName = "";
    public bool _active = false;
    public int _currentDay = 0;
    public int _terminalLevel = 0;
    public double _devPoints = 0;

    public OneSave() { }
    public OneSave(string fileName) { _fileName = fileName; LoadConnectionString(); }
    public OneSave(string fileName, bool active) { _fileName = fileName; _active = active; LoadConnectionString(); }

    public void LoadConnectionString() => _connectionString = $"Data Source={_fileName};Version=3;";
}
