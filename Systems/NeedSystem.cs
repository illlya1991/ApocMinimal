using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Systems;

/// <summary>
/// Manages daily need decay and satisfaction for NPCs.
/// 10 basic needs (IDs 1–10) + up to 10 special needs (IDs 11–20).
/// </summary>
public static class NeedSystem
{
    // ── Decay rates (base, multiplied by Need.Level in Decay()) ──────────────

    private static readonly Dictionary<int, double> BasicDecayRates = new()
    {
        [(int)BasicNeedId.Food]            = 5,   // level3 → 15/day; critical in ~5 days
        [(int)BasicNeedId.Water]           = 7,   // level3 → 21/day; critical in ~4 days
        [(int)BasicNeedId.Sleep]           = 8,   // level3 → 24/day; critical in ~3 days
        [(int)BasicNeedId.Heat]            = 4,   // level3 → 12/day; environment-dependent
        [(int)BasicNeedId.Hygiene]         = 3,   // level3 → 9/day
        [(int)BasicNeedId.Safety]          = 2,   // level3 → 6/day; auto-restored by barrier
        [(int)BasicNeedId.RestHealth]      = 4,   // level3 → 12/day
        [(int)BasicNeedId.Social]          = 3,   // level3 → 9/day
        [(int)BasicNeedId.SexFamily]       = 2,   // level3 → 6/day; critical in ~13 days
        [(int)BasicNeedId.SelfImprovement] = 2,   // level3 → 6/day
    };

    private static readonly Dictionary<int, double> SpecialDecayRates = new()
    {
        [(int)SpecialNeedId.Gourmet]       = 6,   // level3 → 18/day; needs quality food daily
        [(int)SpecialNeedId.Sommelier]     = 5,   // level3 → 15/day; alcohol every ~5 days
        [(int)SpecialNeedId.LightSleeper]  = 4,   // level3 → 12/day; needs proper bed
        [(int)SpecialNeedId.Softie]        = 5,   // level3 → 15/day; needs comfort warmth
        [(int)SpecialNeedId.Aesthete]      = 5,   // level3 → 15/day; premium hygiene
        [(int)SpecialNeedId.Paranoid]      = 4,   // level3 → 12/day; personal safety check
        [(int)SpecialNeedId.Hedonist]      = 5,   // level3 → 15/day; luxury rest
        [(int)SpecialNeedId.SocialLion]    = 5,   // level3 → 15/day; status interaction
        [(int)SpecialNeedId.Romantic]      = 3,   // level3 → 9/day; romance every ~9 days
        [(int)SpecialNeedId.Perfectionist] = 4,   // level3 → 12/day; top-quality learning
    };

    // ── Initialise needs for new NPC ─────────────────────────────────────────

    /// <summary>
    /// Creates 10 basic needs with levels summing to 32.
    /// Guarantees: at least one need at level 1, at least one at level 5.
    /// Plus 1–5 random special needs (from the 10 defined).
    /// </summary>
    public static List<Need> InitialiseNeeds(Npc npc, Random rnd)
    {
        var needs = new List<Need>();

        // ── Basic needs: sum of levels = 32, min one L1 and one L5 ──────────
        int[] levels = GenerateNeedLevels(rnd);

        for (int i = 0; i < BasicNeeds.Names.Length; i++)
        {
            needs.Add(new Need
            {
                Id           = i + 1,
                Name         = BasicNeeds.Names[i],
                Category     = NeedCategory.Basic,
                Level        = levels[i],
                Value        = rnd.Next(5, 25),
                Satisfaction = rnd.Next(75, 100),
            });
        }

        // ── Special needs: random subset of the 10 defined ───────────────────
        int specialCount = rnd.Next(1, 6); // 1–5 special needs per NPC
        foreach (var (id, name, _) in SpecialNeedsData.All.OrderBy(_ => rnd.Next()).Take(specialCount))
        {
            needs.Add(new Need
            {
                Id           = id,
                Name         = name,
                Category     = NeedCategory.Special,
                Level        = rnd.Next(1, 6),
                Value        = rnd.Next(0, 30),
                Satisfaction = rnd.Next(70, 100),
            });
        }

        return needs;
    }

    /// <summary>
    /// Generates level array for 10 basic needs with constraints:
    /// - Sum = 32
    /// - At least one level = 1
    /// - At least one level = 5
    /// - Each level in [1, 5]
    /// </summary>
    private static int[] GenerateNeedLevels(Random rnd)
    {
        int[] levels = new int[10];

        // Pick guaranteed L1 and L5 positions
        int l1 = rnd.Next(10);
        int l5;
        do { l5 = rnd.Next(10); } while (l5 == l1);

        levels[l1] = 1;
        levels[l5] = 5;

        // Remaining 8 needs start at 1 → current sum = 1+5+8 = 14, need 32 → distribute 18
        for (int i = 0; i < 10; i++)
            if (i != l1 && i != l5) levels[i] = 1;

        int remaining = 32 - 14; // = 18
        int maxAttempts = remaining * 20;
        int attempts = 0;

        while (remaining > 0 && attempts < maxAttempts)
        {
            int idx = rnd.Next(10);
            if (idx == l1 || levels[idx] >= 5) { attempts++; continue; }
            levels[idx]++;
            remaining--;
            attempts = 0;
        }

        return levels;
    }

    // ── Daily decay ───────────────────────────────────────────────────────────

    public static void ApplyDailyDecay(Npc npc)
    {
        foreach (var need in npc.Needs)
        {
            double rate = need.Category == NeedCategory.Basic
                ? (BasicDecayRates.TryGetValue(need.Id, out var r) ? r : 4)
                : (SpecialDecayRates.TryGetValue(need.Id, out var sr) ? sr : 5);
            need.Decay(rate);
        }
    }

    // ── Penalties for critical unmet needs ────────────────────────────────────

    public static void ApplyPenalties(Npc npc)
    {
        foreach (var need in npc.Needs.Where(n => n.IsCritical && n.Category == NeedCategory.Basic))
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
                case BasicNeedId.RestHealth:
                    npc.Stamina = Math.Max(0, npc.Stamina - penalty);
                    break;
                case BasicNeedId.Safety:
                    npc.Fear = Math.Min(100, npc.Fear + penalty);
                    break;
                case BasicNeedId.Social:
                case BasicNeedId.SexFamily:
                    npc.Trust = Math.Max(0, npc.Trust - penalty * 0.5);
                    break;
                case BasicNeedId.SelfImprovement:
                    // Morale penalty — reflected via Initiative
                    npc.Initiative = Math.Max(0, npc.Initiative - penalty * 0.3);
                    break;
            }
        }
    }

    // ── Satisfy overloads ─────────────────────────────────────────────────────

    public static bool SatisfyNeed(Npc npc, int needId, double amount)
    {
        var need = npc.Needs.FirstOrDefault(n => n.Id == needId);
        if (need == null) return false;
        need.Satisfy(amount);
        return true;
    }

    public static bool SatisfyNeed(Npc npc, BasicNeedId needId, double amount) =>
        SatisfyNeed(npc, (int)needId, amount);

    public static bool SatisfyNeed(Npc npc, string needName, double amount)
    {
        var need = npc.Needs.FirstOrDefault(n => n.Name == needName);
        if (need == null) return false;
        need.Satisfy(amount);
        return true;
    }

    public static Need? GetMostUrgentNeed(Npc npc) =>
        npc.Needs.OrderByDescending(n => n.Value * n.Level).FirstOrDefault(n => !n.IsSatisfied);

    // ── Night sleep stamina restore ───────────────────────────────────────────

    /// <summary>
    /// Ночной сон 8ч: восстановление = 8 × 12.5 × RecoveryPhys × MaxStamina / 10000.
    /// При RecoveryPhys=100, MaxStamina=100 → полное восстановление.
    /// </summary>
    public static void RestoreStamina(Npc npc)
    {
        double nightRestore = 8 * 12.5 * npc.Stats.RecoveryPhys * npc.MaxStamina / 10000.0;
        npc.Stamina = Math.Clamp(npc.Stamina + nightRestore, 0, npc.MaxStamina);
        // Ночной сон закрывает потребность во сне
        SatisfyNeed(npc, BasicNeedId.Sleep, 70);
    }
}
