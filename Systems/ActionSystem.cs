using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Systems;

public class ActionLogEntry
{
    public string Text { get; set; } = "";
    public string Color { get; set; } = "#c9d1d9";
    public string Time { get; set; } = "";  // "HH:MM"
    public bool IsAlert { get; set; }
}

/// <summary>
/// NPC action selection for the 17-hour daily cycle (06:00–22:00).
/// Priority: critical need → active quest → weighted random.
/// Max 3 special actions per day. Stat growth applied after each action.
/// </summary>
public static class ActionSystem
{
    private const int MaxHoursPerDay  = 23; // 00:00–22:00 включительно
    private const int MaxNightHours   = 12; // максимум ночного сна
    private const int MaxSpecialPerDay = 3;

    public static List<ActionLogEntry> ProcessDayActions(Npc npc, Random rnd, int day)
    {
        var log = new List<ActionLogEntry>();
        if (!npc.IsAlive) return log;

        int maxHours = npc.CharTraits.Contains(CharacterTrait.Lazy) ? 20 : MaxHoursPerDay;
        int specialActions = 0;
        int hoursUsed = 0;

        // ── Фаза 1: Ночной сон (00:00 до восстановления стамины и потребности сна) ──
        var sleepAct = Array.Find(NpcActionCatalog.Basic, a => a.ActionType == NpcActionType.Sleep);
        var sleepNeed = npc.Needs.FirstOrDefault(n => n.Id == (int)BasicNeedId.Sleep);

        while (hoursUsed < MaxNightHours && sleepAct != null)
        {
            bool staminaOk = npc.Stamina >= npc.MaxStamina * 0.85;
            bool sleepOk   = sleepNeed == null || sleepNeed.Value <= 20;
            if (staminaOk && sleepOk) break;

            double delta = 12.5 * npc.Stats.RecoveryPhys * npc.MaxStamina / 10000.0;
            npc.Stamina = Math.Clamp(npc.Stamina + delta, 0, npc.MaxStamina);
            foreach (var kvp in sleepAct.SatisfiedNeeds)
                NeedSystem.SatisfyNeed(npc, kvp.Key, kvp.Value);

            npc.Remember(new MemoryEntry(day, MemoryType.Action, $"[{hoursUsed:00}:00] {sleepAct.Name}"));
            log.Add(new ActionLogEntry { Time = $"{hoursUsed:00}:00", Text = sleepAct.Name, Color = "#c9d1d9" });
            hoursUsed++;
        }

        // ── Фаза 2: Дневные действия ─────────────────────────────────────────────
        var restAct = Array.Find(NpcActionCatalog.Basic, a => a.ActionType == NpcActionType.Rest);

        while (hoursUsed < maxHours)
        {
            int hour = hoursUsed;

            NpcAction? action;
            if (npc.Stamina < 10 && restAct != null)
                action = restAct;
            else
                action = SelectAction(npc, rnd, ref specialActions);

            if (action == null) break;

            double staminaDelta = action.ActionType switch
            {
                NpcActionType.Sleep => 12.5 * npc.Stats.RecoveryPhys * npc.MaxStamina / 10000.0,
                NpcActionType.Rest  => 7.5  * npc.Stats.RecoveryPhys * npc.MaxStamina / 10000.0,
                NpcActionType.Idle  => 5.0  * npc.Stats.RecoveryPhys * npc.MaxStamina / 10000.0,
                _                   => -(action.StaminaCost) + 1.0 * npc.Stats.RecoveryPhys * npc.MaxStamina / 10000.0
            };
            npc.Stamina = Math.Clamp(npc.Stamina + staminaDelta, 0, npc.MaxStamina);

            foreach (var kvp in action.SatisfiedNeeds)
                NeedSystem.SatisfyNeed(npc, kvp.Key, kvp.Value);

            var growth = StatGrowthSystem.Apply(npc, action, rnd);
            string growthSuffix = growth.Count > 0
                ? $" [{string.Join(", ", growth.Select(g => $"+{g.Gain} {g.StatName}"))}]"
                : "";

            npc.Remember(new MemoryEntry(day, MemoryType.Action, $"[{hour:00}:00] {action.Name}"));
            log.Add(new ActionLogEntry
            {
                Time  = $"{hour:00}:00",
                Text  = action.Name + growthSuffix,
                Color = action.Category == ActionCategory.Special ? "#e879f9" : "#c9d1d9",
            });

            hoursUsed++;
        }

        // ── Конец дня: деградация и штрафы ───────────────────────────────────────
        NeedSystem.ApplyDailyDecay(npc);
        NeedSystem.ApplyPenalties(npc);

        for (int i = 0; i < npc.Needs.Count; i++)
        {
            Need need = npc.Needs[i];
            if (need.IsCritical)
            {
                log.Add(new ActionLogEntry
                {
                    Time    = "23:00",
                    Text    = $"[!] Критическая нужда: {need.Name} ({need.Value:F0}%)",
                    Color   = "#f87171",
                    IsAlert = true,
                });
            }
        }

        return log;
    }

    // ── Selection logic ──────────────────────────────────────────────────────

    private static NpcAction? SelectAction(Npc npc, Random rnd, ref int specialCount)
    {
        // 0. Трус: 50% шанс — пассивное действие вместо работы
        if (npc.Trait == NpcTrait.Coward && rnd.NextDouble() < 0.50)
        {
            var idle = Array.Find(NpcActionCatalog.Special, a => a.ActionType == NpcActionType.Idle);
            if (idle != null) { specialCount++; return idle; }
            var rest = Array.Find(NpcActionCatalog.Basic, a => a.ActionType == NpcActionType.Rest);
            if (rest != null) return rest;
        }

        var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);

        // 1. Critical need (Value >= 80) → pick best matching basic action
        if (urgentNeed != null && urgentNeed.Value >= 80)
        {
            NpcAction? needAction = FindBestForNeed(NpcActionCatalog.Basic, npc, urgentNeed.Name);
            if (needAction != null) return needAction;
        }

        // 2. Active quest → prefer actions related to it (simple stub: do random basic)
        if (npc.HasTask)
        {
            NpcAction? questAction = FindTaskAction(npc, rnd);
            if (questAction != null) return questAction;
        }

        // 3. Urgent need (any unsatisfied) → try to address it
        if (urgentNeed != null)
        {
            NpcAction? needAction = FindBestForNeed(NpcActionCatalog.Basic, npc, urgentNeed.Name);
            if (needAction != null) return needAction;

            // Try special actions for the need
            if (specialCount < MaxSpecialPerDay)
            {
                NpcAction? special = FindBestForNeed(NpcActionCatalog.Special, npc, urgentNeed.Name);
                if (special != null) { specialCount++; return special; }
            }
        }

        // 4. Weighted random from all available basic actions
        return WeightedRandom(NpcActionCatalog.Basic, npc, rnd);
    }

    private static NpcAction? FindBestForNeed(NpcAction[] catalog, Npc npc, string needName)
    {
        NpcAction? best = null;
        double bestScore = -1;
        for (int i = 0; i < catalog.Length; i++)
        {
            NpcAction a = catalog[i];
            if (!a.SatisfiedNeeds.ContainsKey(needName)) continue;
            if (!MeetsStatRequirements(npc, a)) continue;
            if (npc.Stamina < a.StaminaCost) continue;
            double score = a.SatisfiedNeeds[needName];
            if (score > bestScore) { bestScore = score; best = a; }
        }
        return best;
    }

    private static NpcAction? FindTaskAction(Npc npc, Random rnd)
    {
        // Prefer actions that grow physical stats (reflects doing assigned work)
        var candidates = new List<NpcAction>();
        for (int i = 0; i < NpcActionCatalog.Basic.Length; i++)
        {
            NpcAction a = NpcActionCatalog.Basic[i];
            if (MeetsStatRequirements(npc, a) && npc.Stamina >= a.StaminaCost && a.StaminaCost > 8)
                candidates.Add(a);
        }
        return candidates.Count > 0 ? candidates[rnd.Next(candidates.Count)] : null;
    }

    private static NpcAction? WeightedRandom(NpcAction[] catalog, Npc npc, Random rnd)
    {
        // Build candidate list with weights; prefer actions NPC can do and that address mild needs
        var pool = new List<(NpcAction action, double weight)>();
        for (int i = 0; i < catalog.Length; i++)
        {
            NpcAction a = catalog[i];
            if (!MeetsStatRequirements(npc, a)) continue;
            if (npc.Stamina < a.StaminaCost) continue;

            double w = 1.0;
            foreach (var kvp in a.SatisfiedNeeds)
            {
                var need = npc.Needs.FirstOrDefault(n => n.Name == kvp.Key);
                if (need != null && !need.IsSatisfied) w += kvp.Value / 20.0;
            }
            pool.Add((a, w));
        }

        if (pool.Count == 0) return null;

        double total = pool.Sum(p => p.weight);
        double pick  = rnd.NextDouble() * total;
        double acc   = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += pool[i].weight;
            if (pick <= acc) return pool[i].action;
        }
        return pool[pool.Count - 1].action;
    }

    private static bool MeetsStatRequirements(Npc npc, NpcAction action)
    {
        foreach (var kvp in action.RequiredStats)
        {
            if (npc.Stats.GetStatValue(kvp.Key) < kvp.Value) return false;
        }
        return true;
    }
}