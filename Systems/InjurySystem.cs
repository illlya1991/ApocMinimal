using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Systems;

/// <summary>
/// Manages NPC injuries: application, daily healing, and stat modifier sync.
/// </summary>
public static class InjurySystem
{
    /// <summary>
    /// Inflict an injury on an NPC after taking significant damage.
    /// Only applied if damage > threshold (avoids trivial scratches).
    /// </summary>
    public static Injury? TryInflict(Npc npc, double damage, Random rnd, double threshold = 15.0)
    {
        if (damage < threshold) return null;
        if (rnd.NextDouble() > 0.40) return null; // 40% chance of injury per hit

        var (name, statId, penalty, days) = InjuryTypes.FromCombatDamage(damage, rnd);
        var injury = new Injury
        {
            NpcId         = npc.Id,
            Name          = name,
            AffectedStatId = statId,
            Penalty       = penalty,
            HealDaysLeft  = days,
        };

        npc.Injuries.Add(injury);
        ApplyPenalty(npc, injury);
        return injury;
    }

    /// <summary>Advance all injuries by one day; remove healed ones and restore stats.</summary>
    public static List<string> AdvanceDay(Npc npc)
    {
        var log = new List<string>();
        var healed = new List<Injury>();

        for (int i = 0; i < npc.Injuries.Count; i++)
        {
            var inj = npc.Injuries[i];
            inj.HealDaysLeft--;

            if (inj.HealDaysLeft <= 0)
            {
                RemovePenalty(npc, inj);
                healed.Add(inj);
                log.Add($"{npc.Name}: вылечился от «{inj.Name}»");
            }
        }

        for (int i = 0; i < healed.Count; i++)
            npc.Injuries.Remove(healed[i]);

        return log;
    }

    /// <summary>Get total stat penalty for a given stat number across all active injuries.</summary>
    public static int GetTotalPenalty(Npc npc, int statId)
    {
        int total = 0;
        for (int i = 0; i < npc.Injuries.Count; i++)
            if (npc.Injuries[i].AffectedStatId == statId)
                total += npc.Injuries[i].Penalty;
        return total;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ApplyPenalty(Npc npc, Injury injury)
    {
        var characteristic = npc.Stats.GetByNumber(injury.AffectedStatId);
        characteristic?.AddDeviation(-injury.Penalty);
    }

    private static void RemovePenalty(Npc npc, Injury injury)
    {
        var characteristic = npc.Stats.GetByNumber(injury.AffectedStatId);
        characteristic?.AddDeviation(injury.Penalty);
    }
}
