using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

/// <summary>
/// Manages quest generation, assignment, and resolution.
/// </summary>
public static class QuestSystem
{
    private static int _nextId = 1;

    /// <summary>
    /// Generate a pool of AI quests for the current day (1–3 new quests).
    /// </summary>
    public static List<Quest> GenerateDailyQuests(List<Resource> resources, Random rnd)
    {
        var result = new List<Quest>();
        int count  = rnd.Next(1, 4);

        var templates = QuestTemplates.All
            .OrderBy(_ => rnd.Next())
            .Take(count);

        foreach (var t in templates)
        {
            // Pick a reward resource based on what's in supply
            int resId = t.ResId > 0 && t.ResId <= resources.Count
                ? resources[t.ResId - 1].Id
                : (resources.Count > 0 ? resources[rnd.Next(resources.Count)].Id : 0);

            result.Add(new Quest
            {
                Id              = _nextId++,
                Title           = t.Title,
                Description     = t.Desc,
                Source          = QuestSource.AI,
                Status          = QuestStatus.Available,
                DaysRequired    = t.Days,
                DaysRemaining   = t.Days,
                RewardResourceId= resId,
                RewardAmount    = t.Reward,
                FaithCost       = t.FaithCost,
            });
        }
        return result;
    }

    /// <summary>
    /// Auto-assign available quests to idle NPCs.
    /// NPCs with high Initiative are more likely to self-assign.
    /// </summary>
    public static List<string> AutoAssign(List<Quest> quests, List<Npc> npcs, Random rnd)
    {
        var log     = new List<string>();
        var idle    = npcs.Where(n => n.IsAlive && !n.HasTask).ToList();
        var avail   = quests.Where(q => q.Status == QuestStatus.Available && q.AssignedNpcId == 0).ToList();

        foreach (var quest in avail)
        {
            var candidate = idle
                .Where(n => n.Initiative >= 40 && rnd.NextDouble() < n.Initiative / 100.0)
                .OrderByDescending(n => n.Initiative)
                .FirstOrDefault();

            if (candidate == null) continue;

            AssignQuest(quest, candidate);
            idle.Remove(candidate);
            log.Add($"  {candidate.Name} взял задание: «{quest.Title}»");
        }
        return log;
    }

    public static void AssignQuest(Quest quest, Npc npc)
    {
        quest.Status          = QuestStatus.Active;
        quest.AssignedNpcId   = npc.Id;
        quest.DaysRemaining   = quest.DaysRequired;

        npc.ActiveTask        = quest.Title;
        npc.TaskDaysLeft      = quest.DaysRequired;
        npc.TaskRewardResId   = quest.RewardResourceId;
        npc.TaskRewardAmt     = quest.RewardAmount;
    }

    /// <summary>
    /// Advance all active quests by one day. Returns reward events.
    /// </summary>
    public static List<(Npc Npc, Quest Quest)> AdvanceDay(
        List<Quest> quests, List<Npc> npcs, Random rnd)
    {
        var rewards = new List<(Npc, Quest)>();

        foreach (var q in quests.Where(q => q.Status == QuestStatus.Active))
        {
            var npc = npcs.FirstOrDefault(n => n.Id == q.AssignedNpcId);
            if (npc == null || !npc.IsAlive)
            {
                q.Status = QuestStatus.Failed;
                continue;
            }

            // Coward trait: 30% chance to fail each day
            if (npc.Trait == NpcTrait.Coward && rnd.NextDouble() < 0.30)
            {
                q.Status     = QuestStatus.Failed;
                npc.ActiveTask    = "";
                npc.TaskDaysLeft  = 0;
                npc.Remember(new MemoryEntry(0, MemoryType.Quest, $"Провалил задание: «{q.Title}»"));
                continue;
            }

            q.DaysRemaining--;
            npc.TaskDaysLeft--;

            if (q.DaysRemaining <= 0)
            {
                q.Status     = QuestStatus.Completed;
                npc.ActiveTask    = "";
                npc.TaskDaysLeft  = 0;
                npc.Remember(new MemoryEntry(0, MemoryType.Quest, $"Выполнил задание: «{q.Title}»"));
                rewards.Add((npc, q));
            }
        }
        return rewards;
    }
}
