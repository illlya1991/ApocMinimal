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
    /// <summary>
    /// Generate a pool of AI quests for the current day (1–3 new quests).
    /// Uses QuestGenerator for procedural template-substituted titles.
    /// </summary>
    public static List<Quest> GenerateDailyQuests(List<Resource> resources, Random rnd)
        => QuestGenerator.GenerateDailyQuests(resources, rnd);

    public static List<string> AutoAssign(List<Quest> quests, List<Npc> npcs, Random rnd)
        => SmartAssign(quests, npcs, rnd);

    public static List<string> SmartAssign(List<Quest> publishedQuests, List<Npc> npcs, Random rnd)
    {
        var log = new List<string>();
        var avail = publishedQuests.Where(q => q.Status == QuestStatus.Available && q.AssignedNpcId == 0).ToList();

        foreach (var quest in avail)
        {
            Npc? best = null;
            double bestScore = double.MinValue;

            for (int i = 0; i < npcs.Count; i++)
            {
                var n = npcs[i];
                if (!n.IsAlive || n.HasTask) continue;
                if (n.ActiveTask != "" || n.TaskDaysLeft > 0) continue;
                if (n.PlayerId == 0) continue; // только НПС под управлением ЦС

                int activeCount = publishedQuests.Count(q => q.Status == QuestStatus.Active && q.AssignedNpcId == n.Id);
                if (activeCount >= 3) continue;

                double score = n.Initiative;

                if (n.Devotion > 50) score += 10;
                if (n.Trust > 60) score += 15;
                if (n.Fear > 70) score -= 20;

                for (int j = 0; j < n.CharTraits.Count; j++)
                {
                    if (n.CharTraits[j] == CharacterTrait.Lazy) score -= 15;
                    if (n.CharTraits[j] == CharacterTrait.Loyal) score += 20;
                    if (n.CharTraits[j] == CharacterTrait.Cowardly) score -= 30;
                }

                if (n.Trait == NpcTrait.Coward) score -= 30;

                if (n.FollowerLevel > 0) score += n.FollowerLevel * 10;

                for (int j = 0; j < n.Specializations.Count; j++)
                {
                    if (quest.Title.Contains(n.Specializations[j], StringComparison.OrdinalIgnoreCase) ||
                        quest.Description.Contains(n.Specializations[j], StringComparison.OrdinalIgnoreCase))
                    {
                        score += 25;
                        break;
                    }
                }

                score += rnd.NextDouble() * 5;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = n;
                }
            }

            if (best == null) continue;

            AssignQuest(quest, best);
            log.Add($"  {best.Name} взял задание: «{quest.Title}»");
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
