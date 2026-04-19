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
    public double FaithGained { get; set; }
    public int FollowerCount { get; set; }
}

public static class GameLoopService
{
    private static readonly string[] PositiveEmotionsArray =
    {
        "Радость", "Спокойствие", "Надежда", "Любовь",
        "Воодушевление", "Гордость", "Благодарность",
    };

    private static bool IsPositiveEmotion(string emotionName)
    {
        for (int i = 0; i < PositiveEmotionsArray.Length; i++)
            if (PositiveEmotionsArray[i] == emotionName) return true;
        return false;
    }

    public static DayResult ProcessDay(
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests,
        Random rnd,
        Dictionary<string, ResourceCatalogEntry>? catalog = null)
    {
        catalog ??= new Dictionary<string, ResourceCatalogEntry>();
        DayResult result = new DayResult();

        ProcessNpcActions(result, npcs, player, rnd, null);
        ProcessQuests(result, quests, npcs, resources, rnd);
        ProcessLeaderBonus(result, npcs);
        ProcessFaithGeneration(result, player, npcs);
        ProcessDailyNeeds(result, player, npcs, resources, catalog);
        ProcessInjuryHealing(result, npcs);

        return result;
    }

    /// <summary>NPC actions only — used at the start of each player turn.</summary>
    public static DayResult ProcessNpcActionsOnly(Player player, List<Npc> npcs, Random rnd, ActionContext? ctx = null)
    {
        var result = new DayResult();
        ProcessNpcActions(result, npcs, player, rnd, ctx);
        return result;
    }

    /// <summary>End-of-day processing (quests, needs, faith) — used when player clicks End Day.</summary>
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
        ProcessFaithGeneration(result, player, npcs);
        ProcessDailyNeeds(result, player, npcs, resources, catalog);
        ProcessInjuryHealing(result, npcs);
        return result;
    }

    // ── Sub-methods ──────────────────────────────────────────────────────────

    private static void ProcessNpcActions(DayResult result, List<Npc> npcs, Player player, Random rnd, ActionContext? ctx = null)
    {
        // Initialize social pairing context
        if (ctx != null)
        {
            ctx.Npcs = npcs;
            ctx.NpcLogs = npcs.ToDictionary(n => n.Id, _ => new List<ActionLogEntry>());
            ctx.SocialPairedToday = new HashSet<int>();
        }

        // Pass 1: build each NPC's own log (social actions inject into partner NpcLogs)
        var ownLogs = new Dictionary<int, List<ActionLogEntry>>(npcs.Count);
        var systemAlerts = new List<ActionLogEntry>();
        for (int i = 0; i < npcs.Count; i++)
        {
            if (!npcs[i].IsAlive) continue;
            ownLogs[npcs[i].Id] = ActionSystem.ProcessDayActions(npcs[i], rnd, player.CurrentDay, ctx, systemAlerts);
        }
        foreach (var alert in systemAlerts)
            result.Logs.Add((alert.Text, true));

        // Pass 2: merge externally-injected entries (all NPCs processed, injections complete)
        for (int i = 0; i < npcs.Count; i++)
        {
            if (!npcs[i].IsAlive) continue;
            var actions = ownLogs[npcs[i].Id];

            if (ctx != null && ctx.NpcLogs.TryGetValue(npcs[i].Id, out var external) && external.Count > 0)
            {
                var injectedTimes = new HashSet<string>(external.Select(e => e.Time));
                actions.RemoveAll(a => injectedTimes.Contains(a.Time));
                actions.AddRange(external);
                actions.Sort((a, b) => string.Compare(a.Time, b.Time, StringComparison.Ordinal));
            }

            result.NpcResults.Add(new NpcDayResult(npcs[i], actions));
        }
    }

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
            if (!leader.IsAlive || leader.Trait != NpcTrait.Leader) continue;

            int count = 0;
            for (int j = 0; j < npcs.Count; j++)
            {
                Npc t = npcs[j];
                if (!t.IsAlive || t.Id == leader.Id || t.Trait == NpcTrait.Loner) continue;
                t.Faith = Math.Min(100, t.Faith + 3);
                count++;
            }

            if (count > 0)
                result.Logs.Add(($"{leader.Name} (Лидер) поднял Веру {count} выжившим +3", false));
        }
    }

    private static void ProcessFaithGeneration(DayResult result, Player player, List<Npc> npcs)
    {
        double faithTotal = 0;
        int followerCount = 0;

        for (int i = 0; i < npcs.Count; i++)
        {
            Npc npc = npcs[i];
            if (!npc.IsAlive || npc.FollowerLevel <= 0) continue;

            followerCount++;
            double maxDay = npc.FollowerLevel * (Player.MaxFaithPerNpcPerDay / 5.0);

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

            faithTotal += Math.Min(maxDay, maxDay * avgSat * trustMod * emoMod);
        }

        player.FaithPoints += faithTotal;
        result.FaithGained = faithTotal;
        result.FollowerCount = followerCount;
    }

    private static void ProcessDailyNeeds(DayResult result, Player player, List<Npc> npcs,
        List<Resource> resources, Dictionary<string, ResourceCatalogEntry> catalog)
    {
        int alive = 0;
        for (int i = 0; i < npcs.Count; i++)
            if (npcs[i].IsAlive) alive++;

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
                    if (npcs[j].IsAlive)
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
                if (npcs[i].IsAlive)
                    npcs[i].Health = Math.Max(0, npcs[i].Health - healthPenalty);
            }
        }
    }
}
