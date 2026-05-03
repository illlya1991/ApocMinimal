using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.Systems;

namespace ApocMinimal.Database;

public partial class DatabaseManager : IDisposable
{
    private const int MaxSavesCount = 3;
    private OneSave _thisSave;
    private OneSave _templateSave;
    private List<OneSave> _ListSaves;
    private static SQLiteConnection _conn = new SQLiteConnection();
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    // Статические кэши
    private static Dictionary<int, Npc> _npcCache = new();
    private static Dictionary<int, Location> _locationCache = new();
    private static Dictionary<string, Technique> _techniqueCache = new();
    private static readonly object _cacheLock = new object();
    private static DateTime _lastCacheClear = DateTime.Now;
    private static readonly object _locationCacheLock = new();
    private static readonly object _npcCacheLock = new();
    private static DateTime _lastNpcLoad = DateTime.MinValue;
    private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

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

                    object? TerminalResult = ExecuteScalar("SELECT TerminalLevel FROM Player Limit 1");
                    if (TerminalResult != null) item._terminalLevel = (int)(long)TerminalResult;

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

        // ВРЕМЕННАЯ ЗАПЛАТКА: оставляем только первые 10 000 НПС
        Report(18, "Обрезка НПС до 10 000...");
        long totalNpcs = (long)(ExecuteScalar("SELECT COUNT(*) FROM Npcs") ?? 0L);
        if (totalNpcs > 10000)
        {
            ExecuteNQ(@"DELETE FROM NpcModifiers WHERE NpcId NOT IN (SELECT Id FROM Npcs ORDER BY Id LIMIT 10000)");
            ExecuteNQ(@"DELETE FROM Npcs WHERE Id NOT IN (SELECT Id FROM Npcs ORDER BY Id LIMIT 10000)");
            ExecuteNQ("VACUUM");
        }

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
    public void ApplyFactionCoefficients(Player player)
    {
        var config = GetGameConfig();
        player.FactionCoeffs = FactionCoefficients.ForFaction(player.Faction, config);
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
    /// <summary>
    /// Полная очистка всех кэшей
    /// </summary>
    public static void ClearAllCaches()
    {
        lock (_cacheLock)
        {
            _npcCache.Clear();
            _locationCache.Clear();
            _techniqueCache.Clear();
            _lastCacheClear = DateTime.Now;

            System.Diagnostics.Debug.WriteLine($"[Cache] Все кэши очищены в {_lastCacheClear:HH:mm:ss.fff}");

            // Принудительный сбор мусора после очистки
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    /// <summary>
    /// Очистка кэша NPC (при изменении данных)
    /// </summary>
    public static void InvalidateNpcCache()
    {
        lock (_cacheLock)
        {
            int count = _npcCache.Count;
            _npcCache.Clear();
            System.Diagnostics.Debug.WriteLine($"[Cache] Очищен кэш NPC ({count} записей)");
        }
    }
    public SQLiteConnection GetConnection()
    {
        return _conn;
    }
    /// <summary>
    /// Очистка кэша локаций (при изменении данных)
    /// </summary>
    public static void InvalidateLocationCache()
    {
        lock (_cacheLock)
        {
            int count = _locationCache.Count;
            _locationCache.Clear();
            System.Diagnostics.Debug.WriteLine($"[Cache] Очищен кэш локаций ({count} записей)");
        }
    }

    /// <summary>
    /// Очистка старых кэшей (вызывать при переключении сохранений)
    /// </summary>
    public void ResetForNewSave()
    {
        // Закрываем текущее соединение
        CloseConnection();

        // Очищаем все кэши
        ClearAllCaches();

        // Сбрасываем статические поля
        _conn?.Dispose();
        _conn = new SQLiteConnection();

        System.Diagnostics.Debug.WriteLine("[Database] Сброшено для нового сохранения");
    }

    /// <summary>
    /// Закрытие соединения с БД
    /// </summary>
    public void CloseConnection()
    {
        try
        {
            if (_conn != null && _conn.State == ConnectionState.Open)
            {
                _conn.Close();
                System.Diagnostics.Debug.WriteLine("[Database] Соединение закрыто");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Database] Ошибка закрытия соединения: {ex.Message}");
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

    private bool _disposed = false;

    public void Dispose()
    {
        if (!_disposed)
        {
            CloseConnection();
            _conn?.Dispose();
            ClearAllCaches();
            _disposed = true;

            System.Diagnostics.Debug.WriteLine("[Database] DatabaseManager освобождён");
        }
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
