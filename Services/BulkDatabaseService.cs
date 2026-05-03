using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Text.Json;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.StatisticsData;

namespace ApocMinimal.Services;

/// <summary>
/// Сервис для массовых операций с БД (batch insert/update).
/// Использует SQLite batch operations и параметризованные запросы.
/// </summary>
public class BulkDatabaseService
{
    private readonly SQLiteConnection _connection;
    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = false };

    // Кэш для prepared statements
    private SQLiteCommand? _cachedInsertCommand;
    private SQLiteCommand? _cachedUpdateCommand;

    public BulkDatabaseService(SQLiteConnection connection)
    {
        _connection = connection;

        // Оптимизация соединения для массовых операций
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA synchronous = OFF;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "PRAGMA cache_size = -100000;";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Сохранить всех NPC одной транзакцией с использованием batch UPDATE.
    /// Для 50 000 NPC это в 100+ раз быстрее, чем по одному.
    /// </summary>
    public async Task SaveAllNpcsBatchAsync(List<Npc> npcs, IProgress<string>? progress = null)
    {
        if (npcs.Count == 0) return;

        const int batchSize = 2000; // Увеличенный размер батча для лучшей производительности
        var batches = npcs.Chunk(batchSize);

        int totalBatches = (int)Math.Ceiling((double)npcs.Count / batchSize);
        int batchNum = 0;

        foreach (var batch in batches)
        {
            batchNum++;
            progress?.Report($"Сохранение NPC: batch {batchNum}/{totalBatches} ({batch.Length} шт.)");

            using var transaction = _connection.BeginTransaction();
            try
            {
                // Используем INSERT OR REPLACE для массовой вставки/обновления
                await ExecuteBatchUpsertAsync(batch, transaction);

                // Сохраняем модификаторы отдельным batch'ом
                await SaveModifiersBatchAsync(batch, transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    private async Task ExecuteBatchUpsertAsync(Npc[] batch, SQLiteTransaction transaction)
    {
        var sql = BuildBatchUpsertSql(batch.Length);
        using var cmd = new SQLiteCommand(sql, _connection, transaction);

        for (int i = 0; i < batch.Length; i++)
        {
            var npc = batch[i];
            var parameters = CreateNpcParameters(cmd, npc, i);
            cmd.Parameters.AddRange(parameters);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    private string BuildBatchUpsertSql(int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("INSERT OR REPLACE INTO Npcs (");
        sb.AppendLine("Id, Name, Age, Gender, Profession, Description, Health, Devotion, Stamina, Energy, ");
        sb.AppendLine("Fear, Trust, Initiative, CombatInitiative, Trait, FollowerLevel, Goal, Dream, Desire, ");
        sb.AppendLine("ActiveTask, TaskDaysLeft, TaskRewardResId, TaskRewardAmt, Statistics, CharTraits, ");
        sb.AppendLine("Specializations, Emotions, Needs, Memory, LocationId, LearnedTechIds) VALUES ");

        for (int i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.AppendLine($"(@id_{i}, @name_{i}, @age_{i}, @gender_{i}, @profession_{i}, @desc_{i}, ");
            sb.AppendLine($"@health_{i}, @devotion_{i}, @stamina_{i}, @energy_{i}, @fear_{i}, @trust_{i}, ");
            sb.AppendLine($"@initiative_{i}, @combat_init_{i}, @trait_{i}, @follower_lvl_{i}, @goal_{i}, ");
            sb.AppendLine($"@dream_{i}, @desire_{i}, @active_task_{i}, @task_days_{i}, @task_res_{i}, ");
            sb.AppendLine($"@task_amt_{i}, @stats_{i}, @traits_{i}, @specs_{i}, @emotions_{i}, @needs_{i}, ");
            sb.AppendLine($"@memory_{i}, @loc_id_{i}, @techs_{i})");
        }

        return sb.ToString();
    }

    private SQLiteParameter[] CreateNpcParameters(SQLiteCommand cmd, Npc npc, int idx)
    {
        var traitStrings = npc.CharTraits.Select(t => t.ToString()).ToList();

        return new SQLiteParameter[]
        {
            new($"@id_{idx}", npc.Id),
            new($"@name_{idx}", npc.Name),
            new($"@age_{idx}", npc.Age),
            new($"@gender_{idx}", npc.Gender == Gender.Female ? "Female" : "Male"),
            new($"@profession_{idx}", npc.Profession),
            new($"@desc_{idx}", npc.Description ?? ""),
            new($"@health_{idx}", npc.Health),
            new($"@devotion_{idx}", npc.Devotion),
            new($"@stamina_{idx}", npc.Stamina),
            new($"@energy_{idx}", npc.Energy),
            new($"@fear_{idx}", npc.Fear),
            new($"@trust_{idx}", npc.Trust),
            new($"@initiative_{idx}", npc.Initiative),
            new($"@combat_init_{idx}", npc.CombatInitiative),
            new($"@trait_{idx}", npc.Trait.ToString()),
            new($"@follower_lvl_{idx}", npc.FollowerLevel),
            new($"@goal_{idx}", npc.Goal ?? ""),
            new($"@dream_{idx}", npc.Dream ?? ""),
            new($"@desire_{idx}", npc.Desire ?? ""),
            new($"@active_task_{idx}", npc.ActiveTask ?? ""),
            new($"@task_days_{idx}", npc.TaskDaysLeft),
            new($"@task_res_{idx}", npc.TaskRewardResId),
            new($"@task_amt_{idx}", npc.TaskRewardAmt),
            new($"@stats_{idx}", JsonSerializer.Serialize(npc.Stats, _jsonOpts)),
            new($"@traits_{idx}", JsonSerializer.Serialize(traitStrings, _jsonOpts)),
            new($"@specs_{idx}", JsonSerializer.Serialize(npc.Specializations, _jsonOpts)),
            new($"@emotions_{idx}", JsonSerializer.Serialize(npc.Emotions, _jsonOpts)),
            new($"@needs_{idx}", JsonSerializer.Serialize(npc.Needs, _jsonOpts)),
            new($"@memory_{idx}", JsonSerializer.Serialize(npc.Memory, _jsonOpts)),
            new($"@loc_id_{idx}", npc.LocationId),
            new($"@techs_{idx}", JsonSerializer.Serialize(npc.LearnedTechIds, _jsonOpts)),
        };
    }

    private async Task SaveModifiersBatchAsync(Npc[] npcs, SQLiteTransaction transaction)
    {
        // Сначала удаляем все старые модификаторы для этих NPC
        var ids = string.Join(",", npcs.Select(n => n.Id));
        using var deleteCmd = new SQLiteCommand($"DELETE FROM NpcModifiers WHERE NpcId IN ({ids})", _connection, transaction);
        await deleteCmd.ExecuteNonQueryAsync();

        // Собираем все модификаторы для массовой вставки
        var allModifiers = new List<(int NpcId, Characteristic Stat, Modifier Mod)>();

        foreach (var npc in npcs)
        {
            foreach (var stat in npc.Stats.AllStats)
            {
                foreach (var mod in stat.GetModifiersByType<PermanentModifier>())
                {
                    allModifiers.Add((npc.Id, stat, mod));
                }
                foreach (var mod in stat.GetModifiersByType<IndependentModifier>())
                {
                    allModifiers.Add((npc.Id, stat, mod));
                }
            }
        }

        if (allModifiers.Count == 0) return;

        // Массовая вставка модификаторов
        const int modBatchSize = 10000;
        var modBatches = allModifiers.Chunk(modBatchSize);

        foreach (var modBatch in modBatches)
        {
            var sql = BuildModifiersInsertSql(modBatch.Length);
            using var insertCmd = new SQLiteCommand(sql, _connection, transaction);

            for (int i = 0; i < modBatch.Length; i++)
            {
                var (npcId, stat, mod) = modBatch[i];
                insertCmd.Parameters.AddWithValue($"@nid_{i}", npcId);
                insertCmd.Parameters.AddWithValue($"@sid_{i}", stat.Id);
                insertCmd.Parameters.AddWithValue($"@mid_{i}", mod.Id);
                insertCmd.Parameters.AddWithValue($"@nm_{i}", mod.Name);
                insertCmd.Parameters.AddWithValue($"@src_{i}", mod.Source);
                insertCmd.Parameters.AddWithValue($"@mty_{i}", (int)mod.Type);
                insertCmd.Parameters.AddWithValue($"@val_{i}", mod.Value);

                if (mod is PermanentModifier perm)
                {
                    insertCmd.Parameters.AddWithValue($"@class_{i}", "Permanent");
                    insertCmd.Parameters.AddWithValue($"@act_{i}", perm.IsActiveFlag ? 1 : 0);
                    insertCmd.Parameters.AddWithValue($"@tu_{i}", DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@dur_{i}", DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@rem_{i}", DBNull.Value);
                }
                else if (mod is IndependentModifier ind)
                {
                    insertCmd.Parameters.AddWithValue($"@class_{i}", "Independent");
                    insertCmd.Parameters.AddWithValue($"@act_{i}", 1);
                    insertCmd.Parameters.AddWithValue($"@tu_{i}", (int)ind.TimeUnit);
                    insertCmd.Parameters.AddWithValue($"@dur_{i}", ind.Duration);
                    insertCmd.Parameters.AddWithValue($"@rem_{i}", ind.Remaining);
                }
            }

            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    private string BuildModifiersInsertSql(int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("INSERT INTO NpcModifiers (NpcId, StatId, ModifierId, Name, Source, ModifierType, Value, ");
        sb.AppendLine("ModifierClass, IsActive, TimeUnit, Duration, Remaining) VALUES ");

        for (int i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.AppendLine($"(@nid_{i}, @sid_{i}, @mid_{i}, @nm_{i}, @src_{i}, @mty_{i}, @val_{i}, ");
            sb.AppendLine($"@class_{i}, @act_{i}, @tu_{i}, @dur_{i}, @rem_{i})");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Вспомогательный метод для чанков
    /// </summary>
    private static IEnumerable<T[]> Chunk<T>(IEnumerable<T> source, int chunkSize)
    {
        var list = source.ToList();
        for (int i = 0; i < list.Count; i += chunkSize)
        {
            int size = Math.Min(chunkSize, list.Count - i);
            var chunk = new T[size];
            for (int j = 0; j < size; j++)
                chunk[j] = list[i + j];
            yield return chunk;
        }
    }
}