using System.Data.SQLite;
using System.IO;
using ApocMinimal.Models;

namespace ApocMinimal.Database;

public class DatabaseManager
{
    private readonly string _connectionString;
    private readonly string _dbPath;

    public DatabaseManager()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apoc_minimal.db");
        _connectionString = $"Data Source={_dbPath};Version=3;";
        InitializeDatabase();
    }

    public bool SaveExists =>
        File.Exists(_dbPath) &&
        ExecuteScalarGlobal("SELECT COUNT(*) FROM Player") is long c && c > 0;

    // =========================================================
    // Инициализация схемы
    // =========================================================

    private void InitializeDatabase()
    {
        using var conn = OpenConnection();

        Execute(conn, @"
            CREATE TABLE IF NOT EXISTS Player (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Name         TEXT    NOT NULL,
                FaithPoints  REAL    DEFAULT 0,
                AltarLevel   INTEGER DEFAULT 1,
                CurrentDay   INTEGER DEFAULT 0
            )");

        Execute(conn, @"
            CREATE TABLE IF NOT EXISTS Npcs (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Name            TEXT    NOT NULL,
                Age             INTEGER DEFAULT 0,
                Profession      TEXT    DEFAULT '',
                Health          REAL    DEFAULT 100,
                Faith           REAL    DEFAULT 0,
                Hunger          REAL    DEFAULT 0,
                Thirst          REAL    DEFAULT 0,
                Trait           TEXT    DEFAULT 'None',
                ActiveTask      TEXT    DEFAULT '',
                TaskDaysLeft    INTEGER DEFAULT 0,
                TaskRewardResId INTEGER DEFAULT 0,
                TaskRewardAmt   REAL    DEFAULT 0
            )");

        Execute(conn, @"
            CREATE TABLE IF NOT EXISTS Resources (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Name     TEXT NOT NULL,
                Amount   REAL DEFAULT 0,
                Category TEXT DEFAULT ''
            )");

        // Миграция: добавляем колонки если их нет (для уже существующих БД)
        MigrateColumns(conn);

        SeedIfEmpty(conn);
    }

    private void MigrateColumns(SQLiteConnection conn)
    {
        var migrations = new[]
        {
            ("Player", "CurrentDay",   "INTEGER DEFAULT 0"),
            ("Npcs",   "Hunger",       "REAL DEFAULT 0"),
            ("Npcs",   "Thirst",       "REAL DEFAULT 0"),
            ("Npcs",   "Trait",        "TEXT DEFAULT 'None'"),
            ("Npcs",   "ActiveTask",   "TEXT DEFAULT ''"),
            ("Npcs",   "TaskDaysLeft", "INTEGER DEFAULT 0"),
            ("Npcs",   "TaskRewardResId", "INTEGER DEFAULT 0"),
            ("Npcs",   "TaskRewardAmt",   "REAL DEFAULT 0"),
        };
        foreach (var (table, col, def) in migrations)
        {
            try { Execute(conn, $"ALTER TABLE {table} ADD COLUMN {col} {def}"); }
            catch { /* колонка уже существует */ }
        }
    }

    private void SeedIfEmpty(SQLiteConnection conn)
    {
        var count = ExecuteScalar(conn, "SELECT COUNT(*) FROM Player") as long? ?? 0;
        if (count > 0) return;

        Execute(conn,
            "INSERT INTO Player (Name, FaithPoints, AltarLevel, CurrentDay) " +
            "VALUES ('Божество', 0, 1, 1)");

        var npcs = new[]
        {
            ("Алексей", 32, "Механик",  90.0, 40.0, "Leader"),
            ("Мария",   27, "Медик",    85.0, 60.0, "None"),
            ("Иван",    45, "Охотник",  95.0, 30.0, "Coward"),
            ("Анна",    23, "Инженер",  80.0, 55.0, "Loner"),
        };
        foreach (var (name, age, prof, hp, faith, trait) in npcs)
            Execute(conn,
                $"INSERT INTO Npcs (Name, Age, Profession, Health, Faith, Hunger, Thirst, Trait) " +
                $"VALUES ('{name}', {age}, '{prof}', {hp}, {faith}, 10, 10, '{trait}')");

        var resources = new[]
        {
            ("Еда",          50.0, "Базовый"),
            ("Вода",         40.0, "Базовый"),
            ("Медикаменты",  15.0, "Медицинский"),
            ("Дерево",       30.0, "Материал"),
            ("Инструменты",  10.0, "Материал"),
        };
        foreach (var (name, amount, cat) in resources)
            Execute(conn,
                $"INSERT INTO Resources (Name, Amount, Category) " +
                $"VALUES ('{name}', {amount}, '{cat}')");
    }

    // =========================================================
    // Сброс (Новая игра)
    // =========================================================

    public void ResetDatabase()
    {
        using var conn = OpenConnection();
        Execute(conn, "DELETE FROM Player");
        Execute(conn, "DELETE FROM Npcs");
        Execute(conn, "DELETE FROM Resources");
        Execute(conn, "DELETE FROM sqlite_sequence");   // сброс AUTOINCREMENT
        SeedIfEmpty(conn);
    }

    // =========================================================
    // Чтение
    // =========================================================

    public Player? GetPlayer()
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("SELECT * FROM Player LIMIT 1", conn);
        using var rdr  = cmd.ExecuteReader();
        if (!rdr.Read()) return null;
        return ReadPlayer(rdr);
    }

    public List<Npc> GetAllNpcs()
    {
        var list = new List<Npc>();
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("SELECT * FROM Npcs ORDER BY Id", conn);
        using var rdr  = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(ReadNpc(rdr));
        return list;
    }

    public List<Resource> GetAllResources()
    {
        var list = new List<Resource>();
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand("SELECT * FROM Resources ORDER BY Id", conn);
        using var rdr  = cmd.ExecuteReader();
        while (rdr.Read())
            list.Add(new Resource
            {
                Id       = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Name     = rdr.GetString(rdr.GetOrdinal("Name")),
                Amount   = rdr.GetDouble(rdr.GetOrdinal("Amount")),
                Category = rdr.GetString(rdr.GetOrdinal("Category")),
            });
        return list;
    }

    // =========================================================
    // Запись
    // =========================================================

    public void SavePlayer(Player p)
    {
        using var conn = OpenConnection();
        Execute(conn,
            $"UPDATE Player SET FaithPoints={p.FaithPoints:F2}, AltarLevel={p.AltarLevel}, " +
            $"CurrentDay={p.CurrentDay} WHERE Id={p.Id}");
    }

    public void SaveNpc(Npc n)
    {
        using var conn = OpenConnection();
        Execute(conn,
            $"UPDATE Npcs SET " +
            $"Health={n.Health:F2}, Faith={n.Faith:F2}, " +
            $"Hunger={n.Hunger:F2}, Thirst={n.Thirst:F2}, " +
            $"ActiveTask='{Esc(n.ActiveTask)}', TaskDaysLeft={n.TaskDaysLeft}, " +
            $"TaskRewardResId={n.TaskRewardResId}, TaskRewardAmt={n.TaskRewardAmt:F2} " +
            $"WHERE Id={n.Id}");
    }

    public void SaveResource(Resource r)
    {
        using var conn = OpenConnection();
        Execute(conn,
            $"UPDATE Resources SET Amount={r.Amount:F2} WHERE Id={r.Id}");
    }

    // =========================================================
    // Вспомогательные
    // =========================================================

    private static Player ReadPlayer(SQLiteDataReader rdr) => new Player
    {
        Id          = rdr.GetInt32(rdr.GetOrdinal("Id")),
        Name        = rdr.GetString(rdr.GetOrdinal("Name")),
        FaithPoints = rdr.GetDouble(rdr.GetOrdinal("FaithPoints")),
        AltarLevel  = rdr.GetInt32(rdr.GetOrdinal("AltarLevel")),
        CurrentDay  = rdr.GetInt32(rdr.GetOrdinal("CurrentDay")),
    };

    private static Npc ReadNpc(SQLiteDataReader rdr) => new Npc
    {
        Id              = rdr.GetInt32(rdr.GetOrdinal("Id")),
        Name            = rdr.GetString(rdr.GetOrdinal("Name")),
        Age             = rdr.GetInt32(rdr.GetOrdinal("Age")),
        Profession      = rdr.GetString(rdr.GetOrdinal("Profession")),
        Health          = rdr.GetDouble(rdr.GetOrdinal("Health")),
        Faith           = rdr.GetDouble(rdr.GetOrdinal("Faith")),
        Hunger          = rdr.GetDouble(rdr.GetOrdinal("Hunger")),
        Thirst          = rdr.GetDouble(rdr.GetOrdinal("Thirst")),
        Trait           = Enum.TryParse<NpcTrait>(rdr.GetString(rdr.GetOrdinal("Trait")), out var t) ? t : NpcTrait.None,
        ActiveTask      = rdr.GetString(rdr.GetOrdinal("ActiveTask")),
        TaskDaysLeft    = rdr.GetInt32(rdr.GetOrdinal("TaskDaysLeft")),
        TaskRewardResId = rdr.GetInt32(rdr.GetOrdinal("TaskRewardResId")),
        TaskRewardAmt   = rdr.GetDouble(rdr.GetOrdinal("TaskRewardAmt")),
    };

    private SQLiteConnection OpenConnection()
    {
        var conn = new SQLiteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static void Execute(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private static object? ExecuteScalar(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        return cmd.ExecuteScalar();
    }

    private object? ExecuteScalarGlobal(string sql)
    {
        try
        {
            using var conn = OpenConnection();
            return ExecuteScalar(conn, sql);
        }
        catch { return null; }
    }

    private static string Esc(string s) => s.Replace("'", "''");
}
