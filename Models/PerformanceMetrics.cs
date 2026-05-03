namespace ApocMinimal.Models;

/// <summary>
/// Метрики производительности для мониторинга.
/// </summary>
public class PerformanceMetrics
{
    public DateTime StartTime { get; set; }
    public int TotalDays { get; set; }
    public int TotalNpcs { get; set; }
    public int AliveNpcs { get; set; }
    public double AverageNpcActionsPerSecond { get; set; }
    public double DbSaveTimeMs { get; set; }
    public double CacheHitRate { get; set; }
    public long MemoryUsageMb { get; set; }

    public string GetSummary()
    {
        return $"Дней: {TotalDays}, НПС: {TotalNpcs} (живых: {AliveNpcs}), " +
               $"Действий/сек: {AverageNpcActionsPerSecond:F1}, " +
               $"Сохранение БД: {DbSaveTimeMs:F0}мс, " +
               $"Кэш: {CacheHitRate:P1}, " +
               $"Память: {MemoryUsageMb}МБ";
    }
}