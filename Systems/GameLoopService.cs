// Systems/GameLoopService.cs
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

public class NpcDayResult
{
    public Npc Npc { get; }
    public IReadOnlyList<ActionLogEntry> Actions { get; }

    public NpcDayResult(Npc npc, List<ActionLogEntry> actions)
    {
        Npc = npc;
        Actions = actions;
    }
}

public class DayResult
{
    public List<NpcDayResult> NpcResults { get; } = new List<NpcDayResult>();
    public List<(Npc Npc, Quest Quest)> QuestRewards { get; } = new List<(Npc, Quest)>();
    public List<Quest> NewQuests { get; } = new List<Quest>();
    public List<(string Text, bool IsAlert)> Logs { get; } = new List<(string, bool)>();
    public double DevPointsGained { get; set; }
    public int FollowerCount { get; set; }
}

/// <summary>
/// Оптимизированный игровой цикл для 10 000+ NPC.
/// Использует параллельную обработку с прогресс-отчётами.
/// </summary>
public static class GameLoopService
{
    private static readonly string[] PositiveEmotionsArray =
    {
        "Радость", "Спокойствие", "Надежда", "Любовь",
        "Воодушевление", "Гордость", "Благодарность",
    };

    // ThreadLocal для изоляции Random в параллельных потоках
    private static readonly ThreadLocal<Random> _threadLocalRng =
        new(() => new Random(Guid.NewGuid().GetHashCode()));

    private static bool IsPositiveEmotion(string emotionName)
    {
        for (int i = 0; i < PositiveEmotionsArray.Length; i++)
            if (PositiveEmotionsArray[i] == emotionName) return true;
        return false;
    }

    /// <summary>
    /// Полный день: NPC действия + конец дня
    /// </summary>
    public static async Task<DayResult> ProcessDayAsync(
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests,
        Random rnd,
        IProgress<(int current, int total)>? progress = null,
        Dictionary<string, ResourceCatalogEntry>? catalog = null)
    {
        catalog ??= new Dictionary<string, ResourceCatalogEntry>();
        DayResult result = new DayResult();

        await ProcessNpcActionsAsync(result, npcs, player, rnd, progress);
        ProcessQuests(result, quests, npcs, resources, rnd);
        ProcessLeaderBonus(result, npcs);
        ProcessDevPointsGeneration(result, player, npcs);
        ProcessDailyNeeds(result, player, npcs, resources, catalog);
        ProcessInjuryHealing(result, npcs);

        return result;
    }

    /// <summary>
    /// Только NPC действия — используется в начале дня игрока
    /// </summary>
    public static async Task<DayResult> ProcessNpcActionsOnlyAsync(
        Player player,
        List<Npc> npcs,
        Random rnd,
        IProgress<(int current, int total)>? progress = null,
        ActionContext? ctx = null)
    {
        var result = new DayResult();
        await ProcessNpcActionsAsync(result, npcs, player, rnd, progress, ctx);
        return result;
    }

    /// <summary>
    /// Только конец дня (квесты, нужды, вера) — синхронный, быстрый
    /// </summary>
    public static DayResult ProcessDayEnd(
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests,
        Random rnd,
        Dictionary<string, ResourceCatalogEntry>? catalog = null)
    {
        catalog ??= new Dictionary<string, ResourceCatalogEntry>();
        var result = new DayResult();
        ProcessQuests(result, quests, npcs, resources, rnd);
        ProcessLeaderBonus(result, npcs);
        ProcessDevPointsGeneration(result, player, npcs);
        ProcessDailyNeeds(result, player, npcs, resources, catalog);
        ProcessInjuryHealing(result, npcs);
        return result;
    }

    // =========================================================
    // Параллельная обработка NPC действий
    // =========================================================

    private static async Task ProcessNpcActionsAsync(
        DayResult result,
        List<Npc> npcs,
        Player player,
        Random rnd,
        IProgress<(int current, int total)>? progress = null,
        ActionContext? ctx = null)
    {
        var aliveNpcs = npcs.Where(n => n.IsAlive).ToList();
        if (aliveNpcs.Count == 0) return;

        // Инициализация контекста для социальных действий
        if (ctx != null)
        {
            ctx.Npcs = npcs;
            ctx.NpcLogs = npcs.ToDictionary(n => n.Id, _ => new List<ActionLogEntry>());
            ctx.SocialPairedToday = new HashSet<int>();
        }

        int total = aliveNpcs.Count;
        int processed = 0;
        int batchSize = Environment.ProcessorCount * 4;
        double statGrowthCoeff = player.FactionCoeffs.CoeffStatGrowth;

        var npcResults = new List<NpcDayResult>(total);
        var systemAlerts = new List<ActionLogEntry>();

        // Разбиваем на батчи для лучшей загрузки CPU
        for (int i = 0; i < aliveNpcs.Count; i += batchSize)
        {
            var batch = aliveNpcs.Skip(i).Take(batchSize).ToList();

            var batchTasks = batch.Select(npc => Task.Run(() =>
            {
                var localRng = _threadLocalRng.Value!;
                var actions = ActionSystem.ProcessDayActionsOptimized(
                    npc, localRng, player.CurrentDay, ctx, null, statGrowthCoeff);
                return (npc, actions);
            })).ToArray();

            var batchResults = await Task.WhenAll(batchTasks);

            foreach (var (npc, actions) in batchResults)
            {
                npcResults.Add(new NpcDayResult(npc, actions));

                // Сбор критических нужд для алертов
                for (int j = 0; j < npc.Needs.Count; j++)
                {
                    var need = npc.Needs[j];
                    if (need.IsCritical)
                    {
                        lock (systemAlerts)
                        {
                            systemAlerts.Add(new ActionLogEntry
                            {
                                Time = "23:00",
                                Text = $"[!] {npc.Name}: критическая нужда «{need.Name}» ({need.Value:F0}%)",
                                Color = "#f87171",
                                IsAlert = true,
                            });
                        }
                        break; // один критический алерт на NPC достаточно
                    }
                }
            }

            processed += batch.Count;
            progress?.Report((processed, total));
        }

        // Мердж внешних логов (социальные действия)
        for (int i = 0; i < npcResults.Count; i++)
        {
            var npcResult = npcResults[i];
            var actions = npcResult.Actions.ToList();

            if (ctx != null && ctx.NpcLogs.TryGetValue(npcResult.Npc.Id, out var external) && external.Count > 0)
            {
                var injectedTimes = new HashSet<string>(external.Select(e => e.Time));
                actions.RemoveAll(a => injectedTimes.Contains(a.Time));
                actions.AddRange(external);
                actions.Sort((a, b) => string.Compare(a.Time, b.Time, StringComparison.Ordinal));

                // Обновляем результат
                npcResults[i] = new NpcDayResult(npcResult.Npc, actions);
            }
        }

        // Добавляем все результаты и алерты
        result.NpcResults.AddRange(npcResults);
        foreach (var alert in systemAlerts)
            result.Logs.Add((alert.Text, true));
    }

    // =========================================================
    // Синхронные методы (быстрые операции)
    // =========================================================

    private static void ProcessQuests(DayResult result, List<Quest> quests, List<Npc> npcs,
        List<Resource> resources, Random rnd)
    {
        List<(Npc, Quest)> rewards = QuestSystem.AdvanceDay(quests, npcs, rnd);
        for (int i = 0; i < rewards.Count; i++)
            result.QuestRewards.Add(rewards[i]);

        List<string> autoLogs = QuestSystem.AutoAssign(quests, npcs, rnd);
        for (int i = 0; i < autoLogs.Count; i++)
            result.Logs.Add((autoLogs[i], false));

        List<Quest> newQuests = QuestSystem.GenerateDailyQuests(resources, rnd);
        for (int i = 0; i < newQuests.Count; i++)
        {
            result.NewQuests.Add(newQuests[i]);
            quests.Add(newQuests[i]);
        }
    }

    private static void ProcessLeaderBonus(DayResult result, List<Npc> npcs)
    {
        for (int i = 0; i < npcs.Count; i++)
        {
            Npc leader = npcs[i];
            // Лидерский бонус только если лидер уже стал последователем (FollowerLevel > 0)
            if (!leader.IsAlive || leader.Trait != NpcTrait.Leader || leader.FollowerLevel == 0) continue;

            int count = 0;
            for (int j = 0; j < npcs.Count; j++)
            {
                Npc t = npcs[j];
                if (!t.IsAlive || t.Id == leader.Id || t.Trait == NpcTrait.Loner) continue;
                if (t.PlayerId != 1) continue; // бонус только для своих НПС
                t.Devotion = Math.Min(100, t.Devotion + 3);
                count++;
            }

            if (count > 0)
                result.Logs.Add(($"{leader.Name} (Лидер) поднял Веру {count} последователям +3", false));
        }
    }

    private static void ProcessDevPointsGeneration(DayResult result, Player player, List<Npc> npcs)
    {
        double devTotal = 0;
        int followerCount = 0;
        double maxDevPerNpc = Player.MaxDevPointsPerNpcPerDay * player.FactionCoeffs.CoeffMaxDevPerNpc;

        for (int i = 0; i < npcs.Count; i++)
        {
            Npc npc = npcs[i];
            if (!npc.IsAlive || npc.FollowerLevel <= 0) continue;

            followerCount++;
            double maxDay = npc.FollowerLevel * (maxDevPerNpc / 5.0);

            double avgSat = 0.5;
            if (npc.Needs.Count > 0)
            {
                double sum = 0;
                for (int j = 0; j < npc.Needs.Count; j++) sum += npc.Needs[j].Satisfaction;
                avgSat = (sum / npc.Needs.Count) / 100.0;
            }

            double trustMod = 0.3 + (npc.Trust / 100.0) * 0.7;

            double posSum = 0;
            for (int j = 0; j < npc.Emotions.Count; j++)
                if (IsPositiveEmotion(npc.Emotions[j].Name)) posSum += npc.Emotions[j].Percentage;
            double emoMod = 0.5 + posSum / 200.0;

            devTotal += Math.Min(maxDay, maxDay * avgSat * trustMod * emoMod);
        }

        double npcContrib = devTotal * player.FactionCoeffs.CoeffDevPerNpc;
        double locationBonus = player.ControlledZoneIds.Count * player.FactionCoeffs.CoeffDevPerLocation;
        player.DevPoints += npcContrib + locationBonus;
        result.DevPointsGained = npcContrib + locationBonus;
        result.FollowerCount = followerCount;
    }

    private static void ProcessDailyNeeds(DayResult result, Player player, List<Npc> npcs,
        List<Resource> resources, Dictionary<string, ResourceCatalogEntry> catalog)
    {
        // Только свои НПС (PlayerId == 1) едят из инвентаря игрока
        int alive = 0;
        for (int i = 0; i < npcs.Count; i++)
            if (npcs[i].IsAlive && npcs[i].PlayerId == 1) alive++;

        if (alive == 0) return;

        // Collect food and water resources from inventory using catalog metadata
        var foodResources = new List<(Resource res, double restore)>();
        var waterResources = new List<(Resource res, double restore)>();

        for (int i = 0; i < resources.Count; i++)
        {
            Resource res = resources[i];
            if (catalog.TryGetValue(res.Name, out var entry))
            {
                if (entry.FoodRestore > 0) foodResources.Add((res, entry.FoodRestore));
                if (entry.WaterRestore > 0) waterResources.Add((res, entry.WaterRestore));
            }
            else
            {
                // Fallback for resources not in catalog: match by name constants
                if (res.Name == ResourceNames.Food) foodResources.Add((res, 20));
                else if (res.Name == ResourceNames.Water) waterResources.Add((res, 30));
            }
        }

        ConsumeResourceGroup(result, npcs, alive, foodResources, BasicNeedId.Food,
            "Голод наносит урон!", 5);
        ConsumeResourceGroup(result, npcs, alive, waterResources, BasicNeedId.Water,
            "Обезвоживание наносит урон!", 8);
    }

    private static void ProcessInjuryHealing(DayResult result, List<Npc> npcs)
    {
        for (int i = 0; i < npcs.Count; i++)
        {
            if (!npcs[i].IsAlive || npcs[i].Injuries.Count == 0) continue;
            var logs = InjurySystem.AdvanceDay(npcs[i]);
            for (int j = 0; j < logs.Count; j++)
                result.Logs.Add((logs[j], false));
        }
    }

    private static void ConsumeResourceGroup(
        DayResult result, List<Npc> npcs, int alive,
        List<(Resource res, double restore)> entries,
        BasicNeedId needId, string emptyAlert, double healthPenalty)
    {
        if (entries.Count == 0) return;

        bool anyAvailable = false;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].res.Amount > 0) { anyAvailable = true; break; }

        if (anyAvailable)
        {
            int remaining = alive;
            for (int i = 0; i < entries.Count && remaining > 0; i++)
            {
                Resource res = entries[i].res;
                if (res.Amount <= 0) continue;

                double consume = Math.Min(res.Amount, remaining);
                res.Amount -= consume;
                remaining -= (int)consume;

                int fed = (int)consume;
                int fedSoFar = 0;
                for (int j = 0; j < npcs.Count && fedSoFar < fed; j++)
                {
                    if (npcs[j].IsAlive && npcs[j].PlayerId == 1)
                    {
                        NeedSystem.SatisfyNeed(npcs[j], needId, entries[i].restore);
                        fedSoFar++;
                    }
                }

                result.Logs.Add(($"{res.Name}: -{consume:F0} ед.  Осталось: {res.Amount:F0}", false));
            }
        }
        else
        {
            string resName = entries.Count > 0 ? entries[0].res.Name : needId.ToString();
            result.Logs.Add(($"{resName}: {emptyAlert}", true));
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i].IsAlive && npcs[i].PlayerId == 1)
                    npcs[i].Health = Math.Max(0, npcs[i].Health - healthPenalty);
            }
        }
    }
}