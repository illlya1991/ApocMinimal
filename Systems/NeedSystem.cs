using ApocMinimal.Models;

namespace ApocMinimal.Systems;

/// <summary>
/// Manages daily need decay and satisfaction for NPCs.
/// All internal logic uses BasicNeedId (int) — no raw string keys.
/// </summary>
public static class NeedSystem
{
    // Daily decay rates keyed by stable BasicNeedId
    private static readonly Dictionary<int, double> BasicDecayRates = new()
    {
        [(int)BasicNeedId.Food]    = 8,
        [(int)BasicNeedId.Water]   = 10,
        [(int)BasicNeedId.Sleep]   = 7,
        [(int)BasicNeedId.Heat]    = 5,
        [(int)BasicNeedId.Hygiene] = 4,
        [(int)BasicNeedId.Toilet]  = 12,
        [(int)BasicNeedId.Safety]  = 3,
        [(int)BasicNeedId.Rest]    = 6,
        [(int)BasicNeedId.Health]  = 2,
        [(int)BasicNeedId.Social]  = 4,
    };

    /// <summary>
    /// Initialise default needs for a newly created NPC.
    /// Picks all 10 basic needs + 1–10 random special needs.
    /// Basic needs get Id 1–10 matching BasicNeedId enum values.
    /// </summary>
    public static List<Need> InitialiseNeeds(Npc npc, Random rnd)
    {
        var needs = new List<Need>();

        // Basic needs — Id matches (int)BasicNeedId
        for (int i = 0; i < BasicNeeds.Names.Length; i++)
        {
            needs.Add(new Need
            {
                Id           = i + 1,   // 1-based = BasicNeedId value
                Name         = BasicNeeds.Names[i],
                Category     = NeedCategory.Basic,
                Level        = rnd.Next(1, 4),
                Value        = rnd.Next(5, 25),
                Satisfaction = rnd.Next(75, 100),
            });
        }

        // Special needs — Id starts at 11+
        int specialId = 11;
        int specialCount = rnd.Next(1, 11);
        foreach (var name in SpecialNeeds.All.OrderBy(_ => rnd.Next()).Take(specialCount))
        {
            needs.Add(new Need
            {
                Id           = specialId++,
                Name         = name,
                Category     = NeedCategory.Special,
                Level        = rnd.Next(1, 6),
                Value        = rnd.Next(0, 40),
                Satisfaction = rnd.Next(60, 100),
            });
        }

        return needs;
    }

    /// <summary>Apply one day of decay to all NPC needs.</summary>
    public static void ApplyDailyDecay(Npc npc)
    {
        foreach (var need in npc.Needs)
        {
            double rate = need.Category == NeedCategory.Basic
                ? (BasicDecayRates.TryGetValue(need.Id, out var r) ? r : 5)
                : 3;
            need.Decay(rate);
        }
    }

    /// <summary>Apply health/stamina penalties for critical unmet basic needs.</summary>
    public static void ApplyPenalties(Npc npc)
    {
        foreach (var need in npc.Needs.Where(n => n.IsCritical && n.Id is >= 1 and <= 10))
        {
            double penalty = need.Level * 2.0;
            switch ((BasicNeedId)need.Id)
            {
                case BasicNeedId.Food:
                case BasicNeedId.Water:
                    npc.Health  = Math.Max(0, npc.Health  - penalty);
                    npc.Stamina = Math.Max(0, npc.Stamina - penalty * 0.5);
                    break;
                case BasicNeedId.Sleep:
                case BasicNeedId.Rest:
                    npc.Stamina = Math.Max(0, npc.Stamina - penalty);
                    break;
                case BasicNeedId.Safety:
                    npc.Fear = Math.Min(100, npc.Fear + penalty);
                    break;
                case BasicNeedId.Health:
                    npc.Health = Math.Max(0, npc.Health - penalty * 0.5);
                    break;
            }
        }
    }

    // ── Satisfy overloads ─────────────────────────────────────────────────────

    /// <summary>Primary API: satisfy by stable need ID.</summary>
    public static bool SatisfyNeed(Npc npc, int needId, double amount)
    {
        var need = npc.Needs.FirstOrDefault(n => n.Id == needId);
        if (need == null) return false;
        need.Satisfy(amount);
        return true;
    }

    /// <summary>Satisfy a basic need using the enum.</summary>
    public static bool SatisfyNeed(Npc npc, BasicNeedId needId, double amount) =>
        SatisfyNeed(npc, (int)needId, amount);

    /// <summary>Satisfy by display name — used by ActionCatalog and legacy callers.</summary>
    public static bool SatisfyNeed(Npc npc, string needName, double amount)
    {
        var need = npc.Needs.FirstOrDefault(n => n.Name == needName);
        if (need == null) return false;
        need.Satisfy(amount);
        return true;
    }

    /// <summary>
    /// Returns the most urgent unsatisfied need (highest Value×Level), or null if all satisfied.
    /// </summary>
    public static Need? GetMostUrgentNeed(Npc npc) =>
        npc.Needs.OrderByDescending(n => n.Value * n.Level).FirstOrDefault(n => !n.IsSatisfied);

    /// <summary>Restore stamina at start of day.</summary>
    public static void RestoreStamina(Npc npc)
    {
        double restBonus = 100;
        var sleep = npc.Needs.FirstOrDefault(n => n.Id == (int)BasicNeedId.Sleep);
        if (sleep != null && sleep.Value > 50)
            restBonus -= (sleep.Value - 50) * 0.8;
        npc.Stamina = Math.Clamp(npc.Stamina + restBonus * 0.5, 0, 100);
    }
}
