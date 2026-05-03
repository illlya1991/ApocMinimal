using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.ResourceData;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ApocMinimal.Systems;

/// <summary>
/// Оптимизированная версия игрового цикла для 50 000 NPC.
/// Использует параллельную обработку и минимизирует блокировки.
/// </summary>
public class OptimizedGameLoopService
{
    private readonly Random _globalRng = new();
    private readonly ThreadLocal<Random> _threadLocalRng = new(() => new Random(Guid.NewGuid().GetHashCode()));

    /// <summary>
    /// Параллельная обработка всех NPC с использованием пула потоков.
    /// </summary>
    public async Task<DayResult> ProcessNpcDaysParallelAsync(
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Location> locations,
        int day,
        double statGrowthCoeff = 1.0,
        IProgress<(int, int)>? progress = null,
        bool alertsOnly = false)
    {
        var result = new DayResult();
        var aliveNpcs = npcs.Where(n => n.IsAlive).ToList();
        int total = aliveNpcs.Count;

        Debug.WriteLine($"      ProcessNpcDaysParallelAsync: начало, всего NPC: {total}, alertsOnly={alertsOnly}");

        var npcResults = new ConcurrentBag<NpcDayResult>();
        var systemAlerts = new ConcurrentBag<(string Text, bool IsAlert)>();

        int chunkSize = Math.Max(50, Environment.ProcessorCount * 50);
        var batches = aliveNpcs.Chunk(chunkSize).ToArray();

        int processed = 0;
        int lastReported = 0;

        foreach (var batch in batches)
        {
            var batchTasks = batch.Select(npc => Task.Run(() =>
            {
                var rng = _threadLocalRng.Value!;
                var ctx = new ActionContext
                {
                    Resources = resources,
                    Locations = locations,
                    Npcs = npcs
                };

                var npcLogs = ActionSystem.ProcessDayActionsOptimized(
                    npc, rng, day, ctx, null, statGrowthCoeff);

                // В режиме alertsOnly оставляем только критические записи
                if (alertsOnly)
                {
                    npcLogs = npcLogs.Where(l => l.IsAlert).ToList();
                    if (npcLogs.Count == 0) return;
                }

                npcResults.Add(new NpcDayResult(npc, npcLogs));
            })).ToArray();

            await Task.WhenAll(batchTasks);

            processed += batch.Length;

            if (progress != null && (processed - lastReported >= 100 || processed == total))
            {
                lastReported = processed;
                progress.Report((processed, total));
                Debug.WriteLine($"        Прогресс: {processed}/{total} ({processed * 100 / total}%)");
            }
        }

        Debug.WriteLine($"      ProcessNpcDaysParallelAsync: собрано результатов {npcResults.Count}");

        result.NpcResults.AddRange(npcResults);

        var criticalNeeds = new List<(Npc Npc, Need Need)>();
        var npcsList = aliveNpcs;

        Parallel.ForEach(npcsList, npc =>
        {
            for (int i = 0; i < npc.Needs.Count; i++)
            {
                var need = npc.Needs[i];
                if (need.IsCritical)
                {
                    lock (criticalNeeds)
                    {
                        criticalNeeds.Add((npc, need));
                    }
                }
            }
        });

        foreach (var (npc, need) in criticalNeeds)
        {
            systemAlerts.Add(($"[!] {npc.Name}: критическая нужда «{need.Name}» ({need.Value:F0}%)", true));
        }

        foreach (var alert in systemAlerts)
        {
            result.Logs.Add(alert);
        }

        Debug.WriteLine($"      ProcessNpcDaysParallelAsync: завершено, логов {result.Logs.Count}");

        return result;
    }

    /// <summary>
    /// Оптимизированная генерация ОР от последователей.
    /// </summary>
    public double CalculateTotalDevPointsGeneration(List<Npc> npcs, Player player)
    {
        double total = 0;
        double maxDevPerNpc = Player.MaxDevPointsPerNpcPerDay * player.FactionCoeffs.CoeffMaxDevPerNpc;

        var lockObj = new object();

        Parallel.ForEach(npcs, npc =>
        {
            if (!npc.IsAlive || npc.FollowerLevel <= 0) return;

            double maxDay = npc.FollowerLevel * (maxDevPerNpc / 5.0);

            double avgSat = 0.5;
            if (npc.Needs.Count > 0)
            {
                double sum = 0;
                for (int j = 0; j < npc.Needs.Count; j++)
                    sum += npc.Needs[j].Satisfaction;
                avgSat = (sum / npc.Needs.Count) / 100.0;
            }

            double trustMod = 0.3 + (npc.Trust / 100.0) * 0.7;

            double posSum = 0;
            for (int j = 0; j < npc.Emotions.Count; j++)
            {
                var emotion = npc.Emotions[j];
                if (emotion.Name is "Радость" or "Спокойствие" or "Надежда" or "Любовь"
                        or "Воодушевление" or "Гордость" or "Благодарность")
                {
                    posSum += emotion.Percentage;
                }
            }
            double emoMod = 0.5 + posSum / 200.0;

            double contribution = Math.Min(maxDay, maxDay * avgSat * trustMod * emoMod);

            lock (lockObj)
            {
                total += contribution;
            }
        });

        return total * player.FactionCoeffs.CoeffDevPerNpc;
    }
}