using ApocMinimal.Models;

namespace ApocMinimal.Systems;

/// <summary>
/// Handles applying techniques to NPCs and location clearance rewards.
/// Stat distribution: primary 60%, secondary 30%, tertiary 10%.
/// </summary>
public static class TechniqueSystem
{
    // Stat IDs by category (priority order from concept)
    private static readonly int[] EnergyStats   = { 23, 24, 25, 26, 27, 28, 29, 30 };
    private static readonly int[] PhysicalStats = { 3, 9, 1, 5, 7, 2, 6, 4, 8, 10 };
    private static readonly int[] MentalStats   = { 16, 13, 12, 19, 11, 21, 22, 15, 17, 14, 18, 20 };

    /// <summary>
    /// Try to apply a technique to an NPC.
    /// Returns true on success; log contains a result message.
    /// </summary>
    public static bool Apply(Technique tech, Npc npc, out string log)
    {
        if (npc.Chakra < tech.ChakraCost)
        { log = $"Недостаточно чакры: {npc.Chakra:F0}/{tech.ChakraCost:F0}"; return false; }

        if (npc.Stamina < tech.StaminaCost)
        { log = $"Недостаточно выносливости: {npc.Stamina:F0}/{tech.StaminaCost:F0}"; return false; }

        foreach (var (statId, minVal) in tech.RequiredStats)
        {
            if (!npc.Stats.TryGetValue(statId, out var val) || val < minVal)
            {
                string sname = StatDefs.Names.GetValueOrDefault(statId, statId.ToString());
                log = $"Требуется {sname} ≥ {minVal} (есть {npc.Stats.GetValueOrDefault(statId):F0})";
                return false;
            }
        }

        npc.Chakra  = Math.Max(0, npc.Chakra  - tech.ChakraCost);
        npc.Stamina = Math.Max(0, npc.Stamina - tech.StaminaCost);

        double mult  = tech.TechLevel.GetMultiplier();
        double bonus = mult * 3.0;

        var (primary, secondary, tertiary) = tech.TechType switch
        {
            TechniqueType.Physical => (PhysicalStats, EnergyStats,   MentalStats),
            TechniqueType.Mental   => (MentalStats,   EnergyStats,   PhysicalStats),
            _                      => (EnergyStats,   PhysicalStats, MentalStats),
        };

        ApplyGroup(npc, primary,   bonus * 0.60);
        ApplyGroup(npc, secondary, bonus * 0.30);
        ApplyGroup(npc, tertiary,  bonus * 0.10);

        log = $"{tech.Name} [{tech.TechLevel.ToLabel()} ×{mult}] применена → {npc.Name}";
        return true;
    }

    /// <summary>ОВ reward for clearing a location: Floor=5, Building=50.</summary>
    public static double ClearanceReward(LocationType type) => type switch
    {
        LocationType.Floor    => 5.0,
        LocationType.Building => 50.0,
        _                     => 0.0,
    };

    private static void ApplyGroup(Npc npc, int[] ids, double total)
    {
        if (ids.Length == 0) return;
        double perStat = total / ids.Length;
        foreach (var id in ids)
            npc.Stats[id] = Math.Min(100,
                npc.Stats.TryGetValue(id, out var cur) ? cur + perStat : perStat);
    }
}
