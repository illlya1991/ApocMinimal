using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

/// <summary>
/// Per-NPC result of processing one day.
/// </summary>
public class NpcDayResult
{
    public Npc                     Npc     { get; }
    public IReadOnlyList<ActionLogEntry> Actions { get; }

    public NpcDayResult(Npc npc, List<ActionLogEntry> actions)
    {
        Npc     = npc;
        Actions = actions;
    }
}

/// <summary>
/// Aggregate result returned by GameLoopService.ProcessDay.
/// Contains all structured data needed to render the log — no WPF types.
/// </summary>
public class DayResult
{
    public List<NpcDayResult>       NpcResults    { get; } = new();
    public List<(Npc Npc, Quest Quest)> QuestRewards  { get; } = new();
    public List<Quest>              NewQuests     { get; } = new();
    /// <summary>General text log lines (resource consumption, leader bonus, etc.).</summary>
    public List<(string Text, bool IsAlert)> Logs { get; } = new();
    public double FaithGained    { get; set; }
    public int    FollowerCount  { get; set; }
}

/// <summary>
/// Pure game-logic service: drives the end-of-day pipeline.
/// Has no dependency on WPF — can be unit-tested and reused.
/// </summary>
public static class GameLoopService
{
    private static readonly HashSet<string> PositiveEmotions = new()
    {
        "Радость", "Спокойствие", "Надежда", "Любовь",
        "Воодушевление", "Гордость", "Благодарность",
    };

    /// <summary>
    /// Process one full game day.
    /// Mutates player, npcs, resources, quests in-place (same as before);
    /// returns a <see cref="DayResult"/> that the UI can render.
    /// </summary>
    public static DayResult ProcessDay(
        Player       player,
        List<Npc>    npcs,
        List<Resource> resources,
        List<Quest>  quests,
        Random       rnd)
    {
        var result = new DayResult();

        // ── 1. NPC autonomous actions ─────────────────────────────────────────
        foreach (var npc in npcs.Where(n => n.IsAlive))
        {
            var actions = ActionSystem.ProcessDayActions(npc, rnd, player.CurrentDay);
            result.NpcResults.Add(new NpcDayResult(npc, actions));
        }

        // ── 2. Quest advancement ──────────────────────────────────────────────
        result.QuestRewards.AddRange(QuestSystem.AdvanceDay(quests, npcs, rnd));

        // ── 3. Auto-assign quests to idle NPCs ────────────────────────────────
        foreach (var line in QuestSystem.AutoAssign(quests, npcs, rnd))
            result.Logs.Add((line, false));

        // ── 4. Generate new daily quests ──────────────────────────────────────
        var newQuests = QuestSystem.GenerateDailyQuests(resources, rnd);
        result.NewQuests.AddRange(newQuests);
        quests.AddRange(newQuests);

        // ── 5. Leader bonus ───────────────────────────────────────────────────
        foreach (var leader in npcs.Where(n => n.IsAlive && n.Trait == NpcTrait.Leader))
        {
            var targets = npcs
                .Where(n => n.IsAlive && n.Id != leader.Id && n.Trait != NpcTrait.Loner)
                .ToList();
            foreach (var t in targets)
                t.Faith = Math.Min(100, t.Faith + 3);
            if (targets.Count > 0)
                result.Logs.Add(($"{leader.Name} (Лидер) поднял Веру {targets.Count} выжившим +3", false));
        }

        // ── 6. Faith (ОВ) generation ──────────────────────────────────────────
        double faithTotal = 0;
        foreach (var npc in npcs.Where(n => n.IsAlive && n.FollowerLevel > 0))
        {
            double maxDay  = npc.FollowerLevel * (Player.MaxFaithPerNpcPerDay / 5.0);
            double avgSat  = npc.Needs.Count > 0
                ? npc.Needs.Average(n => n.Satisfaction) / 100.0
                : 0.5;
            double trustMod = 0.3 + npc.Trust / 100.0 * 0.7;
            double posSum   = npc.Emotions
                .Where(em => PositiveEmotions.Contains(em.Name))
                .Sum(em => em.Percentage);
            double emoMod  = 0.5 + posSum / 200.0;
            faithTotal    += Math.Min(maxDay, maxDay * avgSat * trustMod * emoMod);
        }
        player.FaithPoints  += faithTotal;
        result.FaithGained   = faithTotal;
        result.FollowerCount = npcs.Count(n => n.IsAlive && n.FollowerLevel > 0);

        // ── 7. Auto-consume resources ─────────────────────────────────────────
        int alive = npcs.Count(n => n.IsAlive);
        if (alive > 0)
        {
            var food  = resources.FirstOrDefault(r => r.Name == ResourceNames.Food);
            var water = resources.FirstOrDefault(r => r.Name == ResourceNames.Water);

            if (food != null)
            {
                double eat = Math.Min(food.Amount, alive);
                food.Amount -= eat;
                foreach (var n in npcs.Where(n => n.IsAlive).Take((int)eat))
                    NeedSystem.SatisfyNeed(n, BasicNeedId.Food, 30);
                result.Logs.Add(($"{ResourceNames.Food}: -{eat:F0} ед.  Осталось: {food.Amount:F0}", false));
            }

            if (water != null)
            {
                double drink = Math.Min(water.Amount, alive);
                water.Amount -= drink;
                foreach (var n in npcs.Where(n => n.IsAlive).Take((int)drink))
                    NeedSystem.SatisfyNeed(n, BasicNeedId.Water, 35);
                result.Logs.Add(($"{ResourceNames.Water}: -{drink:F0} ед.  Осталось: {water.Amount:F0}", false));
            }
        }

        return result;
    }
}
