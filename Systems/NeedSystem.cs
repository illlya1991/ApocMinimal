using ApocMinimal.Models;

namespace ApocMinimal.Systems;

/// <summary>
/// Manages daily need decay and satisfaction for NPCs.
/// </summary>
public static class NeedSystem
{
    // Daily decay per need category (basic needs decay faster)
    private static readonly Dictionary<string, double> BasicDecayRates = new()
    {
        ["Еда"]          = 8,
        ["Вода"]         = 10,
        ["Сон"]          = 7,
        ["Тепло"]        = 5,
        ["Гигиена"]      = 4,
        ["Туалет"]       = 12,
        ["Безопасность"] = 3,
        ["Отдых"]        = 6,
        ["Здоровье"]     = 2,
        ["Социальность"] = 4,
    };

    /// <summary>
    /// Initialise default needs for a newly created NPC.
    /// Picks all 10 basic needs + 1–10 random special needs based on NPC personality.
    /// </summary>
    public static List<Need> InitialiseNeeds(Npc npc, Random rnd)
    {
        var needs = new List<Need>();
        int id = 1;

        // All 10 basic needs
        foreach (var name in BasicNeeds.Names)
        {
            needs.Add(new Need
            {
                Id           = id++,
                Name         = name,
                Category     = NeedCategory.Basic,
                Level        = rnd.Next(1, 4),
                Value        = rnd.Next(5, 25),
                Satisfaction = rnd.Next(75, 100),
            });
        }

        // 1–10 random special needs
        int specialCount = rnd.Next(1, 11);
        var specialNames = SpecialNeeds.All.OrderBy(_ => rnd.Next()).Take(specialCount);
        foreach (var name in specialNames)
        {
            needs.Add(new Need
            {
                Id           = id++,
                Name         = name,
                Category     = NeedCategory.Special,
                Level        = rnd.Next(1, 6),
                Value        = rnd.Next(0, 40),
                Satisfaction = rnd.Next(60, 100),
            });
        }

        return needs;
    }

    /// <summary>
    /// Apply one day of decay to all NPC needs.
    /// </summary>
    public static void ApplyDailyDecay(Npc npc)
    {
        foreach (var need in npc.Needs)
        {
            double rate = need.Category == NeedCategory.Basic
                ? (BasicDecayRates.TryGetValue(need.Name, out var r) ? r : 5)
                : 3;  // special needs decay slowly
            need.Decay(rate);
        }
    }

    /// <summary>
    /// Apply health/stamina penalties for critical unmet needs.
    /// </summary>
    public static void ApplyPenalties(Npc npc)
    {
        foreach (var need in npc.Needs.Where(n => n.IsCritical))
        {
            double penalty = need.Level * 2.0;
            switch (need.Name)
            {
                case "Еда":
                case "Вода":
                    npc.Health  = Math.Max(0, npc.Health  - penalty);
                    npc.Stamina = Math.Max(0, npc.Stamina - penalty * 0.5);
                    break;
                case "Сон":
                case "Отдых":
                    npc.Stamina = Math.Max(0, npc.Stamina - penalty);
                    break;
                case "Безопасность":
                    npc.Fear = Math.Min(100, npc.Fear + penalty);
                    break;
                case "Здоровье":
                    npc.Health = Math.Max(0, npc.Health - penalty * 0.5);
                    break;
            }
        }
    }

    /// <summary>
    /// Satisfy a specific need by name.
    /// </summary>
    public static bool SatisfyNeed(Npc npc, string needName, double amount)
    {
        var need = npc.Needs.FirstOrDefault(n => n.Name == needName);
        if (need == null) return false;
        need.Satisfy(amount);
        return true;
    }

    /// <summary>
    /// Returns the most urgent unsatisfied need (highest Value), or null if all satisfied.
    /// </summary>
    public static Need? GetMostUrgentNeed(Npc npc) =>
        npc.Needs.OrderByDescending(n => n.Value * n.Level).FirstOrDefault(n => !n.IsSatisfied);

    /// <summary>
    /// Restore stamina at start of day (sleep/rest needs reduce regeneration if unmet).
    /// </summary>
    public static void RestoreStamina(Npc npc)
    {
        double restBonus = 100;
        var sleep = npc.Needs.FirstOrDefault(n => n.Name == "Сон");
        if (sleep != null && sleep.Value > 50)
            restBonus -= (sleep.Value - 50) * 0.8;
        npc.Stamina = Math.Clamp(npc.Stamina + restBonus * 0.5, 0, 100);
    }
}
