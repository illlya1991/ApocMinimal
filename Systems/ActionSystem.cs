using ApocMinimal.Models;

namespace ApocMinimal.Systems;

/// <summary>
/// AI action selection: each NPC performs up to 23 actions per day
/// (max 3 special, rest basic). Actions are chosen based on needs and stats.
/// </summary>
public static class ActionSystem
{
    private const int MaxActionsPerDay = 23;
    private const int MaxSpecialPerDay = 3;

    public static List<string> ProcessDayActions(Npc npc, Random rnd, int day)
    {
        var log = new List<string>();

        if (!npc.IsAlive) return log;

        NeedSystem.RestoreStamina(npc);

        int totalActions  = 0;
        int specialActions = 0;
        double staminaLeft = npc.Stamina;

        // Coward trait: 50% chance of skipping the day
        if (npc.Trait == NpcTrait.Coward && rnd.NextDouble() < 0.50)
        {
            log.Add($"  {npc.Name} (Трус) отказался действовать сегодня.");
            return log;
        }

        // Lazy character trait: –10 max actions
        int maxActions = MaxActionsPerDay;
        if (npc.CharTraits.Contains(CharacterTrait.Lazy)) maxActions -= 10;

        while (totalActions < maxActions && staminaLeft > 2)
        {
            // Pick the most urgent need
            var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);
            GameAction? action = null;

            if (urgentNeed != null)
            {
                // Try to find a basic action that satisfies this need
                action = ActionCatalog.Basic
                    .Where(a => a.SatisfiedNeeds.ContainsKey(urgentNeed.Name)
                             && MeetsStatRequirements(npc, a)
                             && staminaLeft >= a.StaminaCost)
                    .OrderByDescending(a => a.SatisfiedNeeds[urgentNeed.Name])
                    .FirstOrDefault();
            }

            // If no matching basic action, try a special (if quota allows)
            if (action == null && specialActions < MaxSpecialPerDay && urgentNeed != null)
            {
                action = ActionCatalog.Special
                    .Where(a => a.SatisfiedNeeds.ContainsKey(urgentNeed.Name)
                             && MeetsStatRequirements(npc, a)
                             && staminaLeft >= a.StaminaCost)
                    .OrderByDescending(a => a.SatisfiedNeeds[urgentNeed.Name])
                    .FirstOrDefault();
                if (action != null) specialActions++;
            }

            // Fallback: pick any affordable basic action at random
            if (action == null)
            {
                var candidates = ActionCatalog.Basic
                    .Where(a => MeetsStatRequirements(npc, a) && staminaLeft >= a.StaminaCost)
                    .ToList();
                if (candidates.Count == 0) break;
                action = candidates[rnd.Next(candidates.Count)];
            }

            // Apply the action
            staminaLeft -= action.StaminaCost;
            npc.Stamina  = Math.Clamp(npc.Stamina - action.StaminaCost, 0, 100);

            foreach (var (needName, amount) in action.SatisfiedNeeds)
                NeedSystem.SatisfyNeed(npc, needName, amount);

            npc.Remember(new MemoryEntry(day, MemoryType.Action, $"Выполнил: {action.Name}"));
            log.Add($"  {npc.Name}: {action.Name}");
            totalActions++;
        }

        // Apply end-of-day penalties
        NeedSystem.ApplyDailyDecay(npc);
        NeedSystem.ApplyPenalties(npc);

        return log;
    }

    private static bool MeetsStatRequirements(Npc npc, GameAction action)
    {
        foreach (var (statId, minVal) in action.RequiredStats)
        {
            if (!npc.Stats.TryGetValue(statId, out var val) || val < minVal)
                return false;
        }
        return true;
    }
}
