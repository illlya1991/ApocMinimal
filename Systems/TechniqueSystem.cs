using ApocMinimal.Models.StatisticsData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.LocationData;

namespace ApocMinimal.Systems;

/// <summary>
/// Handles applying techniques to NPCs and location clearance rewards.
/// Stat distribution: primary 60%, secondary 30%, tertiary 10%.
/// </summary>
public static class TechniqueSystem
{
    // Stat IDs by category (priority order from concept)
    private static readonly int[] EnergyStats   = { 23, 24, 25, 26, 27, 28, 29, 30 };
    private static readonly int[] PhysicalStats  = { 3, 9, 1, 5, 7, 2, 6, 4, 8, 10 };
    private static readonly int[] MentalStats    = { 16, 13, 12, 19, 11, 21, 22, 15, 17, 14, 18, 20 };

    /// <summary>
    /// Try to apply a technique to an NPC.
    /// Returns true on success; log contains a result message.
    /// </summary>
    public static bool Apply(Technique tech, Npc npc, out string log)
    {
        if (npc.Energy < tech.EnergyCost)
        { log = $"Недостаточно энергии: {npc.Energy:F0}/{tech.EnergyCost:F0}"; return false; }

        if (npc.Stamina < tech.StaminaCost)
        { log = $"Недостаточно выносливости: {npc.Stamina:F0}/{tech.StaminaCost:F0}"; return false; }

        foreach (var (statId, minVal) in tech.RequiredStats)
        {
            int current = npc.Stats.GetStatValue(statId);
            if (current < minVal)
            {
                string sname = npc.Stats.GetByNumber(statId)?.Name ?? $"Stat_{statId}";
                log = $"Требуется {sname} ≥ {minVal} (есть {current})";
                return false;
            }
        }

        npc.Energy  = Math.Max(0, npc.Energy  - tech.EnergyCost);
        npc.Stamina = Math.Max(0, npc.Stamina - tech.StaminaCost);

        if (tech.HealAmount > 0)
        {
            double actual = Math.Min(tech.HealAmount, 100 - npc.Health);
            npc.Health = Math.Min(100, npc.Health + tech.HealAmount);
            log = $"{tech.Name} применена → {npc.Name}: здоровье +{actual:F0} → {npc.Health:F0}";
            return true;
        }

        if (tech.TrustBoost > 0 || tech.FearClear > 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"{tech.Name} применена → {npc.Name}:");
            if (tech.TrustBoost > 0)
            {
                int prev = (int)npc.Trust;
                npc.Trust = (int)Math.Min(100, npc.Trust + tech.TrustBoost);
                sb.Append($" Доверие +{npc.Trust - prev}");
            }
            if (tech.FearClear > 0)
            {
                double prev = npc.Fear;
                npc.Fear = Math.Max(0, npc.Fear - tech.FearClear);
                sb.Append($" Страх −{prev - npc.Fear:F0}");
            }
            log = sb.ToString();
            return true;
        }

        if (tech.StaminaBoost > 0)
        {
            double prev = npc.Stamina;
            npc.Stamina = Math.Min(npc.MaxStamina, npc.Stamina + tech.StaminaBoost);
            log = $"{tech.Name} применена → {npc.Name}: Выносливость +{npc.Stamina - prev:F0} → {npc.Stamina:F0}";
            return true;
        }

        double mult  = tech.TechLevel.GetMultiplier();
        double bonus = mult * 3.0;

        var (primary, secondary, tertiary) = tech.TechType switch
        {
            TechniqueType.Physical => (PhysicalStats, EnergyStats, MentalStats),
            TechniqueType.Mental   => (MentalStats,   EnergyStats, PhysicalStats),
            _                      => (EnergyStats,   PhysicalStats, MentalStats),
        };

        ApplyGroup(npc.Stats, primary,   bonus * 0.60);
        ApplyGroup(npc.Stats, secondary, bonus * 0.30);
        ApplyGroup(npc.Stats, tertiary,  bonus * 0.10);

        log = $"{tech.Name} [{tech.TechLevel.ToLabel()} ×{mult}] применена → {npc.Name}";
        return true;
    }

    /// <summary>ОР reward for clearing a location: Floor=5, Building=50.</summary>
    public static double ClearanceReward(LocationType type) => type switch
    {
        LocationType.Floor    => 5.0,
        LocationType.Building => 50.0,
        _                     => 0.0,
    };

    private static void ApplyGroup(Statistics stats, int[] ids, double total)
    {
        if (ids.Length == 0) return;
        double perStat = total / ids.Length;
        foreach (var id in ids)
        {
            int delta = (int)Math.Round(perStat);
            if (delta != 0)
                stats.GetByNumber(id)?.AddDeviation(delta);
        }
    }
}
