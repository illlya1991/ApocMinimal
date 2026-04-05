using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using ApocMinimal.Models;

namespace ApocMinimal.Database;

public class DatabaseManager
{
    private readonly string _connectionString;
    private readonly string _dbPath;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public DatabaseManager()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apoc_minimal.db");
        _connectionString = $"Data Source={_dbPath};Version=3;";
        InitializeDatabase();
    }

    public bool SaveExists
    {
        get
        {
            try
            {
                using var conn = OpenConnection();
                return (long)(ExecuteScalar(conn, "SELECT COUNT(*) FROM Player") ?? 0L) > 0;
            }
            catch { return false; }
        }
    }

    // =========================================================
    // Инициализация схемы
    // =========================================================

    private void InitializeDatabase()
    {
        using var conn = OpenConnection();

        ExecuteNQ(conn, @"
            CREATE TABLE IF NOT EXISTS Player (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Name         TEXT    NOT NULL,
                FaithPoints  REAL    DEFAULT 0,
                AltarLevel   INTEGER DEFAULT 1,
                CurrentDay   INTEGER DEFAULT 0
            )");

        ExecuteNQ(conn, @"
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
                TaskRewardAmt   REAL    DEFAULT 0,
                Stats           TEXT    DEFAULT '{}'
            )");

        ExecuteNQ(conn, @"
            CREATE TABLE IF NOT EXISTS Resources (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Name     TEXT NOT NULL,
                Amount   REAL DEFAULT 0,
                Category TEXT DEFAULT ''
            )");

        MigrateColumns(conn);
        SeedIfEmpty(conn);
    }

    private void MigrateColumns(SQLiteConnection conn)
    {
        var cols = new[]
        {
            ("Player", "CurrentDay",      "INTEGER DEFAULT 0"),
            ("Npcs",   "Hunger",          "REAL DEFAULT 0"),
            ("Npcs",   "Thirst",          "REAL DEFAULT 0"),
            ("Npcs",   "Trait",           "TEXT DEFAULT 'None'"),
            ("Npcs",   "ActiveTask",      "TEXT DEFAULT ''"),
            ("Npcs",   "TaskDaysLeft",    "INTEGER DEFAULT 0"),
            ("Npcs",   "TaskRewardResId", "INTEGER DEFAULT 0"),
            ("Npcs",   "TaskRewardAmt",   "REAL DEFAULT 0"),
            ("Npcs",   "Stats",           "TEXT DEFAULT '{}'"),
        };
        foreach (var (table, col, def) in cols)
        {
            try { ExecuteNQ(conn, $"ALTER TABLE {table} ADD COLUMN {col} {def}"); }
            catch { }
        }
    }

    private void SeedIfEmpty(SQLiteConnection conn)
    {
        var count = (long)(ExecuteScalar(conn, "SELECT COUNT(*) FROM Player") ?? 0L);
        if (count > 0) return;

        using var cmd = new SQLiteCommand(
            "INSERT INTO Player (Name, FaithPoints, AltarLevel, CurrentDay) VALUES (@n,@fp,@al,@cd)", conn);
        cmd.Parameters.AddWithValue("@n",  "Божество");
        cmd.Parameters.AddWithValue("@fp", 0.0);
        cmd.Parameters.AddWithValue("@al", 1);
        cmd.Parameters.AddWithValue("@cd", 1);
        cmd.ExecuteNonQuery();

        var rnd = new Random(42);
        var npcs = new[]
        {
            ("Алексей", 32, "Механик",  90.0, 40.0, "Leader"),
            ("Мария",   27, "Медик",    85.0, 60.0, "None"),
            ("Иван",    45, "Охотник",  95.0, 30.0, "Coward"),
            ("Анна",    23, "Инженер",  80.0, 55.0, "Loner"),
        };
        foreach (var (name, age, prof, hp, faith, trait) in npcs)
        {
            var stats = GenerateStats(prof, rnd);
            var statsJson = JsonSerializer.Serialize(stats, JsonOpts);
            using var c2 = new SQLiteCommand(
                "INSERT INTO Npcs (Name,Age,Profession,Health,Faith,Hunger,Thirst,Trait,Stats) " +
                "VALUES (@nm,@ag,@pr,@hp,@fa,@hu,@th,@tr,@st)", conn);
            c2.Parameters.AddWithValue("@nm", name);
            c2.Parameters.AddWithValue("@ag", age);
            c2.Parameters.AddWithValue("@pr", prof);
            c2.Parameters.AddWithValue("@hp", hp);
            c2.Parameters.AddWithValue("@fa", faith);
            c2.Parameters.AddWithValue("@hu", 10.0);
            c2.Parameters.AddWithValue("@th", 10.0);
            c2.Parameters.AddWithValue("@tr", trait);
            c2.Parameters.AddWithValue("@st", statsJson);
            c2.ExecuteNonQuery();
        }

        var resources = new[]
        {
            ("Еда",          50.0, "Базовый"),
            ("Вода",         40.0, "Базовый"),
            ("Медикаменты",  15.0, "Медицинский"),
            ("Дерево",       30.0, "Материал"),
            ("Инструменты",  10.0, "Материал"),
        };
        foreach (var (name, amount, cat) in resources)
        {
            using var c3 = new SQLiteCommand(
                "INSERT INTO Resources (Name,Amount,Category) VALUES (@n,@a,@c)", conn);
            c3.Parameters.AddWithValue("@n", name);
            c3.Parameters.AddWithValue("@a", amount);
            c3.Parameters.AddWithValue("@c", cat);
            c3.ExecuteNonQuery();
        }
    }

    // Генерация 30 статов с профессиональными бонусами
    // ID 1-10: Физические | 11-22: Умственные | 23-30: Энергетические
    private static Dictionary<int, double> GenerateStats(string profession, Random rnd)
    {
        var stats = new Dictionary<int, double>();
        for (int i = 1; i <= 30; i++)
            stats[i] = rnd.Next(15, 55);

        // Профессиональные бонусы (+25..+40 к профильным статам)
        int[] bonuses = profession switch
        {
            "Механик" => new[]
            {
                1,   // Сила
                2,   // Ловкость
                3,   // Выносливость
                7,   // Рефлексы
                11,  // Пространств. интеллект
                12,  // Логика
                26,  // Контроль энергии
            },
            "Медик" => new[]
            {
                2,   // Ловкость
                4,   // Сенсорика
                5,   // Регенерация
                12,  // Логика
                13,  // Память
                14,  // Соц.-эмоц. интеллект
                17,  // Интуиция
                28,  // Тонкость манипуляций
            },
            "Охотник" => new[]
            {
                1,   // Сила
                2,   // Ловкость
                3,   // Выносливость
                7,   // Рефлексы
                9,   // Стойкость
                17,  // Интуиция
                19,  // Дедукция
                29,  // Энергетич. восприятие
            },
            "Инженер" => new[]
            {
                2,   // Ловкость
                6,   // Адаптация
                11,  // Пространств. интеллект
                12,  // Логика
                15,  // Когнитивная гибкость
                18,  // Творчество
                20,  // Математич. способности
                25,  // Концентрация энергии
            },
            _ => new[] { 1, 3, 6, 12, 17, 23 },
        };

        foreach (var id in bonuses)
            stats[id] = Math.Min(100, stats[id] + rnd.Next(28, 45));

        return stats;
    }

    // =========================================================
    // Сброс (Новая игра)
    // =========================================================

    public void ResetDatabase()
    {
        using var conn = OpenConnection();
        ExecuteNQ(conn, "DELETE FROM Player");
        ExecuteNQ(conn, "DELETE FROM Npcs");
        ExecuteNQ(conn, "DELETE FROM Resources");
        try { ExecuteNQ(conn, "DELETE FROM sqlite_sequence"); } catch { }
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
        return rdr.Read() ? ReadPlayer(rdr) : null;
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
    // Запись — параметризованные запросы (без зависимости от культуры)
    // =========================================================

    public void SavePlayer(Player p)
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand(
            "UPDATE Player SET FaithPoints=@fp, AltarLevel=@al, CurrentDay=@cd WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@fp", p.FaithPoints);
        cmd.Parameters.AddWithValue("@al", p.AltarLevel);
        cmd.Parameters.AddWithValue("@cd", p.CurrentDay);
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveNpc(Npc n)
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand(
            "UPDATE Npcs SET Health=@hp, Faith=@fa, Hunger=@hu, Thirst=@th, " +
            "ActiveTask=@at, TaskDaysLeft=@tdl, TaskRewardResId=@trr, TaskRewardAmt=@tra, " +
            "Stats=@st WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@hp",  n.Health);
        cmd.Parameters.AddWithValue("@fa",  n.Faith);
        cmd.Parameters.AddWithValue("@hu",  n.Hunger);
        cmd.Parameters.AddWithValue("@th",  n.Thirst);
        cmd.Parameters.AddWithValue("@at",  n.ActiveTask);
        cmd.Parameters.AddWithValue("@tdl", n.TaskDaysLeft);
        cmd.Parameters.AddWithValue("@trr", n.TaskRewardResId);
        cmd.Parameters.AddWithValue("@tra", n.TaskRewardAmt);
        cmd.Parameters.AddWithValue("@st",  JsonSerializer.Serialize(n.Stats, JsonOpts));
        cmd.Parameters.AddWithValue("@id",  n.Id);
        cmd.ExecuteNonQuery();
    }

    public void SaveResource(Resource r)
    {
        using var conn = OpenConnection();
        using var cmd  = new SQLiteCommand(
            "UPDATE Resources SET Amount=@a WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@a",  r.Amount);
        cmd.Parameters.AddWithValue("@id", r.Id);
        cmd.ExecuteNonQuery();
    }

    // =========================================================
    // Вспомогательные
    // =========================================================

    private static Player ReadPlayer(SQLiteDataReader rdr) => new()
    {
        Id          = rdr.GetInt32(rdr.GetOrdinal("Id")),
        Name        = rdr.GetString(rdr.GetOrdinal("Name")),
        FaithPoints = rdr.GetDouble(rdr.GetOrdinal("FaithPoints")),
        AltarLevel  = rdr.GetInt32(rdr.GetOrdinal("AltarLevel")),
        CurrentDay  = rdr.GetInt32(rdr.GetOrdinal("CurrentDay")),
    };

    private static Npc ReadNpc(SQLiteDataReader rdr)
    {
        var statsJson = rdr.IsDBNull(rdr.GetOrdinal("Stats"))
            ? "{}" : rdr.GetString(rdr.GetOrdinal("Stats"));
        Dictionary<int, double> stats;
        try { stats = JsonSerializer.Deserialize<Dictionary<int, double>>(statsJson) ?? new(); }
        catch  { stats = new(); }

        return new Npc
        {
            Id              = rdr.GetInt32(rdr.GetOrdinal("Id")),
            Name            = rdr.GetString(rdr.GetOrdinal("Name")),
            Age             = rdr.GetInt32(rdr.GetOrdinal("Age")),
            Profession      = rdr.GetString(rdr.GetOrdinal("Profession")),
            Health          = rdr.GetDouble(rdr.GetOrdinal("Health")),
            Faith           = rdr.GetDouble(rdr.GetOrdinal("Faith")),
            Hunger          = rdr.GetDouble(rdr.GetOrdinal("Hunger")),
            Thirst          = rdr.GetDouble(rdr.GetOrdinal("Thirst")),
            Trait           = Enum.TryParse<NpcTrait>(rdr.GetString(rdr.GetOrdinal("Trait")), out var t)
                              ? t : NpcTrait.None,
            ActiveTask      = rdr.GetString(rdr.GetOrdinal("ActiveTask")),
            TaskDaysLeft    = rdr.GetInt32(rdr.GetOrdinal("TaskDaysLeft")),
            TaskRewardResId = rdr.GetInt32(rdr.GetOrdinal("TaskRewardResId")),
            TaskRewardAmt   = rdr.GetDouble(rdr.GetOrdinal("TaskRewardAmt")),
            Stats           = stats,
        };
    }

    private SQLiteConnection OpenConnection()
    {
        var conn = new SQLiteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static void ExecuteNQ(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private static object? ExecuteScalar(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        return cmd.ExecuteScalar();
    }
}
