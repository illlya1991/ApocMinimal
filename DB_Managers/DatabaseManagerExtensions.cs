using System.Data.SQLite;

namespace ApocMinimal.Database;

/// <summary>
/// Расширения для DatabaseManager для оптимизированного доступа к соединению
/// </summary>
public static class DatabaseManagerExtensions
{
    /// <summary>
    /// Получение SQLite соединения из DatabaseManager
    /// </summary>
    public static SQLiteConnection GetConnection(this DatabaseManager db)
    {
        // Используем рефлексию для доступа к приватному полю _conn
        var field = typeof(DatabaseManager).GetField("_conn",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (field != null)
        {
            return field.GetValue(null) as SQLiteConnection;
        }

        throw new InvalidOperationException("Cannot access SQLite connection");
    }

    /// <summary>
    /// Создание команды SQL с оптимизированными параметрами
    /// </summary>
    public static SQLiteCommand CreateCommand(this DatabaseManager db)
    {
        var conn = db.GetConnection();
        var cmd = conn.CreateCommand();

        // Оптимизация команды для массовых операций
        cmd.CommandTimeout = 300; // 5 минут таймаут для массовых операций

        return cmd;
    }
}