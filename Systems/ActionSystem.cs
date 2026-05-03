using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.ResourceData;

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
    private const int MaxHoursPerDay  = 23;
    private const int MaxNightHours   = 12;
    private const int MaxSpecialPerDay = 3;

    // Social action IDs that require a partner
    private static readonly HashSet<int> SocialActionIds = new() { 10, 18, 29, 103, 130 };
    private static bool IsSocialAction(NpcAction action) => SocialActionIds.Contains(action.Id);

    public static List<ActionLogEntry> ProcessDayActions(Npc npc, Random rnd, int day, ActionContext? ctx = null,
        List<ActionLogEntry>? systemAlerts = null, double statGrowthCoeff = 1.0)
    {
        var log = new List<ActionLogEntry>();
        if (!npc.IsAlive) return log;

        int maxHours = npc.CharTraits.Contains(CharacterTrait.Lazy) ? 20 : MaxHoursPerDay;
        int specialActions = 0;
        int hoursUsed = 0;

        // ── Фаза 1: Ночной сон ──────────────────────────────────────────────────
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

        // ── Фаза 1.5: Путешествие ───────────────────────────────────────────────
        Location? todayLocation = null;
        if (ctx != null && ctx.Locations.Count > 0)
        {
            todayLocation = PickTravelLocation(npc, ctx.Locations, rnd);
            if (todayLocation != null && todayLocation.Id != npc.LocationId)
            {
                var currentLoc = ctx.Locations.FirstOrDefault(l => l.Id == npc.LocationId);
                bool sameBuilding = currentLoc != null
                    && GetBuildingAncestor(currentLoc, ctx.Locations) is int cb && cb != 0
                    && GetBuildingAncestor(todayLocation, ctx.Locations) == cb;

                npc.LocationId = todayLocation.Id;
                npc.Remember(new MemoryEntry(day, MemoryType.Discovery,
                    $"[{hoursUsed:00}:00] {(sameBuilding ? "Перешёл" : "Путешествие")} → {todayLocation.Name}"));
                log.Add(new ActionLogEntry
                {
                    Time  = $"{hoursUsed:00}:00",
                    Text  = $"{(sameBuilding ? "Перешёл" : "Путешествие")} → {todayLocation.Name}",
                    Color = sameBuilding ? "#6b7280" : "#94a3b8",
                });

                if (!sameBuilding)
                {
                    // External travel: costs 1 hour + stamina
                    npc.Stamina = Math.Clamp(npc.Stamina - 8, 0, npc.MaxStamina);
                    hoursUsed++;
                }
                else
                {
                    // Internal (same building): no stamina cost, but still uses the hour slot to avoid duplicate
                    hoursUsed++;
                }
            }
        }

        // ── Фаза 2: Дневные действия ────────────────────────────────────────────
        var restAct = Array.Find(NpcActionCatalog.Basic, a => a.ActionType == NpcActionType.Rest);

        while (hoursUsed < maxHours)
        {
            int hour = hoursUsed;
            string? socialPartnerName = null;

            NpcAction? action;
            if (npc.Stamina < 10 && restAct != null)
            {
                action = restAct;
            }
            else
            {
                action = SelectAction(npc, rnd, ref specialActions);

                // Handle social actions: find partner or re-select
                if (action != null && IsSocialAction(action) && ctx != null)
                {
                    var partner = FindSocialPartner(npc, ctx, rnd);
                    if (partner != null)
                    {
                        // Mark only the partner so they aren't chosen again;
                        // the initiator is free to start more social actions.
                        ctx.SocialPairedToday.Add(partner.Id);
                        socialPartnerName = partner.Name;

                        // Add matching entry to partner's log
                        if (!ctx.NpcLogs.ContainsKey(partner.Id))
                            ctx.NpcLogs[partner.Id] = new List<ActionLogEntry>();
                        ctx.NpcLogs[partner.Id].Add(new ActionLogEntry
                        {
                            Time  = $"{hour:00}:00",
                            Text  = $"{action.Name} с {npc.Name}",
                            Color = action.Category == ActionCategory.Special ? "#e879f9" : "#c9d1d9",
                        });
                    }
                    else
                    {
                        // No partner available — pick non-social action
                        action = SelectNonSocialAction(npc, rnd);
                    }
                }
            }

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

            var growth = StatGrowthSystem.Apply(npc, action, rnd, statGrowthCoeff);
            string growthSuffix = growth.Count > 0
                ? $" [{string.Join(", ", growth.Select(g => $"+{g.Gain} {g.StatName}"))}]"
                : "";

            string resourceSuffix = "";
            if (ctx != null)
            {
                var sbRes = new System.Text.StringBuilder();
                foreach (var kvp in action.ResourceConsumes)
                {
                    var res = ctx.Resources.FirstOrDefault(r => r.Name == kvp.Key);
                    if (res != null && res.Amount >= kvp.Value)
                    {
                        res.Amount -= kvp.Value;
                        sbRes.Append($" -{kvp.Value:G} {kvp.Key}");
                    }
                    else
                    {
                        sbRes.Append($" ({kvp.Key}: нет)");
                    }
                }
                foreach (var kvp in action.ResourceFinds)
                {
                    Location? findLoc = todayLocation
                        ?? ctx.Locations.FirstOrDefault(l => l.Id == npc.LocationId);
                    bool nodeAvailable = findLoc != null
                        && findLoc.ResourceNodes.TryGetValue(kvp.Key, out double nodeAmt)
                        && nodeAmt > 0;

                    if (nodeAvailable && findLoc != null)
                    {
                        double found = Math.Round(kvp.Value * (0.5 + rnd.NextDouble()), 1);
                        double nodeMax = findLoc.ResourceNodes[kvp.Key];
                        found = Math.Min(found, nodeMax);
                        found = Math.Round(found, 1);
                        if (found > 0)
                        {
                            MapInitializer.DeductFromNode(findLoc, kvp.Key, found);
                            var res = ctx.Resources.FirstOrDefault(r => r.Name == kvp.Key);
                            if (res != null) res.Amount += found;
                            sbRes.Append($" +{found} {kvp.Key}");
                        }
                    }
                    else if (findLoc == null)
                    {
                        sbRes.Append($" ({kvp.Key}: нет локации)");
                    }
                    else
                    {
                        sbRes.Append($" ({kvp.Key}: исчерпано)");
                    }
                }
                resourceSuffix = sbRes.ToString();
            }

            string socialSuffix = socialPartnerName != null ? $" с {socialPartnerName}" : "";

            npc.Remember(new MemoryEntry(day, MemoryType.Action, $"[{hour:00}:00] {action.Name}{socialSuffix}"));
            log.Add(new ActionLogEntry
            {
                Time  = $"{hour:00}:00",
                Text  = action.Name + socialSuffix + resourceSuffix + growthSuffix,
                Color = action.Category == ActionCategory.Special ? "#e879f9" : "#c9d1d9",
            });

            hoursUsed++;
        }

        // ── Конец дня: деградация и штрафы ──────────────────────────────────────
        NeedSystem.ApplyDailyDecay(npc);
        NeedSystem.ApplyPenalties(npc);

        for (int i = 0; i < npc.Needs.Count; i++)
        {
            Need need = npc.Needs[i];
            if (need.IsCritical)
            {
                var alert = new ActionLogEntry
                {
                    Time    = "23:00",
                    Text    = $"[!] {npc.Name}: критическая нужда «{need.Name}» ({need.Value:F0}%)",
                    Color   = "#f87171",
                    IsAlert = true,
                };
                if (systemAlerts != null)
                    systemAlerts.Add(alert);
                else
                    log.Add(alert);
            }
        }

        return log;
    }

    // ── Selection logic ──────────────────────────────────────────────────────

    private static NpcAction? SelectAction(Npc npc, Random rnd, ref int specialCount)
    {
        if (npc.Trait == NpcTrait.Coward && rnd.NextDouble() < 0.50)
        {
            var idle = Array.Find(NpcActionCatalog.Special, a => a.ActionType == NpcActionType.Idle);
            if (idle != null) { specialCount++; return idle; }
            var rest = Array.Find(NpcActionCatalog.Basic, a => a.ActionType == NpcActionType.Rest);
            if (rest != null) return rest;
        }

        var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);

        if (urgentNeed != null && urgentNeed.Value >= 80)
        {
            NpcAction? needAction = FindBestForNeed(NpcActionCatalog.Basic, npc, urgentNeed.Name);
            if (needAction != null) return needAction;
        }

        if (npc.HasTask)
        {
            NpcAction? questAction = FindTaskAction(npc, rnd);
            if (questAction != null) return questAction;
        }

        if (urgentNeed != null)
        {
            NpcAction? needAction = FindBestForNeed(NpcActionCatalog.Basic, npc, urgentNeed.Name);
            if (needAction != null) return needAction;

            if (specialCount < MaxSpecialPerDay)
            {
                NpcAction? special = FindBestForNeed(NpcActionCatalog.Special, npc, urgentNeed.Name);
                if (special != null) { specialCount++; return special; }
            }
        }

        return WeightedRandom(NpcActionCatalog.Basic, npc, rnd);
    }

    /// <summary>Select a non-social action as fallback.</summary>
    private static NpcAction? SelectNonSocialAction(Npc npc, Random rnd)
    {
        var pool = new List<(NpcAction action, double weight)>();
        for (int i = 0; i < NpcActionCatalog.Basic.Length; i++)
        {
            NpcAction a = NpcActionCatalog.Basic[i];
            if (IsSocialAction(a)) continue;
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

    /// <summary>Find an alive NPC at the same location that hasn't been paired today.</summary>
    private static Npc? FindSocialPartner(Npc npc, ActionContext ctx, Random rnd)
    {
        var candidates = new List<Npc>();
        for (int i = 0; i < ctx.Npcs.Count; i++)
        {
            Npc n = ctx.Npcs[i];
            if (!n.IsAlive) continue;
            if (n.Id == npc.Id) continue;
            if (ctx.SocialPairedToday.Contains(n.Id)) continue;
            if (n.LocationId != npc.LocationId) continue;
            candidates.Add(n);
        }
        return candidates.Count > 0 ? candidates[rnd.Next(candidates.Count)] : null;
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

    /// <summary>Returns the Id of the nearest Building ancestor, or 0 if none.</summary>
    private static int GetBuildingAncestor(Location loc, List<Location> locations)
    {
        var cur = loc;
        for (int depth = 0; depth < 6 && cur != null; depth++)
        {
            if (cur.Type == LocationType.Building) return cur.Id;
            cur = locations.FirstOrDefault(l => l.Id == cur.ParentId);
        }
        return 0;
    }

    private static Location? PickTravelLocation(Npc npc, List<Location> locations, Random rnd)
    {
        if (rnd.NextDouble() < 0.20) return null;

        var explored = locations.Where(l =>
            l.IsExplored &&
            l.MapState == MapState.Current &&
            l.Type is LocationType.Building
                   or LocationType.Floor
                   or LocationType.Apartment
                   or LocationType.Street
        ).ToList();

        if (explored.Count == 0) return null;

        var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);
        if (urgentNeed != null)
        {
            string? resourceNeeded = urgentNeed.Name switch
            {
                "Еда"              => "Еда",
                "Вода"             => "Вода",
                "Отдых и здоровье" => "Медикаменты",
                _                  => null,
            };
            if (resourceNeeded != null)
            {
                var withResource = explored
                    .Where(l => l.ResourceNodes.TryGetValue(resourceNeeded, out double v) && v > 0)
                    .ToList();
                if (withResource.Count > 0)
                    return withResource[rnd.Next(withResource.Count)];
            }
        }

        return explored[rnd.Next(explored.Count)];
    }

    public static List<ActionLogEntry> ProcessDayActionsOptimized(
    Npc npc,
    Random rnd,
    int day,
    ActionContext? ctx = null,
    List<ActionLogEntry>? systemAlerts = null,
    double statGrowthCoeff = 1.0)
    {
        var log = new List<ActionLogEntry>();
        if (!npc.IsAlive) return log;

        int maxHours = npc.CharTraits.Contains(CharacterTrait.Lazy) ? 20 : MaxHoursPerDay;
        int specialActions = 0;
        int hoursUsed = 0;

        // Оптимизация: кэшируем MaxStamina один раз
        double maxStamina = npc.MaxStamina;
        double recoveryPhys = npc.Stats.RecoveryPhys;

        // ── Фаза 1: Ночной сон (оптимизировано) ───────────────────────────────
        var sleepAct = Array.Find(NpcActionCatalog.Basic, a => a.ActionType == NpcActionType.Sleep);
        var sleepNeed = npc.Needs.FirstOrDefault(n => n.Id == (int)BasicNeedId.Sleep);
        bool sleepNeedMet = sleepNeed?.Value <= 20;

        while (hoursUsed < MaxNightHours && sleepAct != null &&
               (npc.Stamina < maxStamina * 0.85 || !sleepNeedMet))
        {
            double delta = 12.5 * recoveryPhys * maxStamina / 10000.0;
            npc.Stamina = Math.Clamp(npc.Stamina + delta, 0, maxStamina);

            // Оптимизация: используем прямой доступ к словарю без LINQ
            if (sleepAct.SatisfiedNeeds.TryGetValue("Сон", out var sleepValue))
                NeedSystem.SatisfyNeed(npc, "Сон", sleepValue);
            if (sleepAct.SatisfiedNeeds.TryGetValue("Отдых и здоровье", out var restValue))
                NeedSystem.SatisfyNeed(npc, "Отдых и здоровье", restValue);

            npc.Remember(new MemoryEntry(day, MemoryType.Action, $"[{hoursUsed:00}:00] {sleepAct.Name}"));
            log.Add(new ActionLogEntry { Time = $"{hoursUsed:00}:00", Text = sleepAct.Name, Color = "#c9d1d9" });
            hoursUsed++;

            sleepNeedMet = sleepNeed?.Value <= 20;
        }

        // ── Фаза 2: Путешествие (оптимизировано) ──────────────────────────────
        Location? todayLocation = null;
        if (ctx != null && ctx.Locations.Count > 0)
        {
            todayLocation = PickTravelLocationOptimized(npc, ctx.Locations, rnd);
            if (todayLocation != null && todayLocation.Id != npc.LocationId)
            {
                npc.LocationId = todayLocation.Id;
                npc.Remember(new MemoryEntry(day, MemoryType.Discovery, $"[{hoursUsed:00}:00] Путешествие → {todayLocation.Name}"));
                log.Add(new ActionLogEntry { Time = $"{hoursUsed:00}:00", Text = $"Путешествие → {todayLocation.Name}", Color = "#94a3b8" });
                npc.Stamina = Math.Clamp(npc.Stamina - 8, 0, maxStamina);
                hoursUsed++;
            }
        }

        // ── Фаза 3: Дневные действия (оптимизировано) ─────────────────────────
        var restAct = Array.Find(NpcActionCatalog.Basic, a => a.ActionType == NpcActionType.Rest);
        var basicActions = NpcActionCatalog.Basic;
        var specialActionsList = NpcActionCatalog.Special;

        while (hoursUsed < maxHours)
        {
            int hour = hoursUsed;
            NpcAction? action = null;

            if (npc.Stamina < 10 && restAct != null)
            {
                action = restAct;
            }
            else
            {
                action = SelectActionOptimized(npc, rnd, ref specialActions, basicActions, specialActionsList);

                // Обработка социальных действий
                if (action != null && IsSocialAction(action) && ctx != null)
                {
                    var partner = FindSocialPartnerOptimized(npc, ctx, rnd);
                    if (partner != null)
                    {
                        ctx.SocialPairedToday.Add(partner.Id);
                        if (!ctx.NpcLogs.ContainsKey(partner.Id))
                            ctx.NpcLogs[partner.Id] = new List<ActionLogEntry>();
                        ctx.NpcLogs[partner.Id].Add(new ActionLogEntry
                        {
                            Time = $"{hour:00}:00",
                            Text = $"{action.Name} с {npc.Name}",
                            Color = action.Category == ActionCategory.Special ? "#e879f9" : "#c9d1d9",
                        });
                    }
                    else if (action != null && IsSocialAction(action))
                    {
                        action = SelectNonSocialActionOptimized(npc, rnd, basicActions);
                    }
                }
            }

            if (action == null) break;

            // Обновление стамины
            double staminaDelta = action.ActionType switch
            {
                NpcActionType.Sleep => 12.5 * recoveryPhys * maxStamina / 10000.0,
                NpcActionType.Rest => 7.5 * recoveryPhys * maxStamina / 10000.0,
                NpcActionType.Idle => 5.0 * recoveryPhys * maxStamina / 10000.0,
                _ => -action.StaminaCost + 1.0 * recoveryPhys * maxStamina / 10000.0
            };
            npc.Stamina = Math.Clamp(npc.Stamina + staminaDelta, 0, maxStamina);

            // Удовлетворение потребностей (прямой доступ вместо LINQ)
            foreach (var kvp in action.SatisfiedNeeds)
            {
                NeedSystem.SatisfyNeed(npc, kvp.Key, kvp.Value);
            }

            // Рост статов
            var growth = StatGrowthSystem.Apply(npc, action, rnd, statGrowthCoeff);
            string growthSuffix = growth.Count > 0
                ? $" [{string.Join(", ", growth.Select(g => $"+{g.Gain} {g.StatName}"))}]"
                : "";

            npc.Remember(new MemoryEntry(day, MemoryType.Action, $"[{hour:00}:00] {action.Name}"));
            log.Add(new ActionLogEntry
            {
                Time = $"{hour:00}:00",
                Text = action.Name + growthSuffix,
                Color = action.Category == ActionCategory.Special ? "#e879f9" : "#c9d1d9",
            });

            hoursUsed++;
        }

        // ── Конец дня ─────────────────────────────────────────────────────────
        NeedSystem.ApplyDailyDecay(npc);
        NeedSystem.ApplyPenalties(npc);

        return log;
    }

    // Вспомогательные оптимизированные методы
    private static NpcAction? SelectActionOptimized(Npc npc, Random rnd, ref int specialCount,
        NpcAction[] basicActions, NpcAction[] specialActionsList)
    {
        var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);

        if (urgentNeed != null && urgentNeed.Value >= 80)
        {
            NpcAction? needAction = FindBestForNeedOptimized(basicActions, npc, urgentNeed.Name);
            if (needAction != null) return needAction;
        }

        if (npc.HasTask)
        {
            NpcAction? questAction = FindTaskActionOptimized(npc, rnd, basicActions);
            if (questAction != null) return questAction;
        }

        if (urgentNeed != null)
        {
            NpcAction? needAction = FindBestForNeedOptimized(basicActions, npc, urgentNeed.Name);
            if (needAction != null) return needAction;

            if (specialCount < MaxSpecialPerDay)
            {
                NpcAction? special = FindBestForNeedOptimized(specialActionsList, npc, urgentNeed.Name);
                if (special != null) { specialCount++; return special; }
            }
        }

        return WeightedRandomOptimized(basicActions, npc, rnd);
    }

    private static NpcAction? FindBestForNeedOptimized(NpcAction[] catalog, Npc npc, string needName)
    {
        NpcAction? best = null;
        double bestScore = -1;
        double stamina = npc.Stamina;

        for (int i = 0; i < catalog.Length; i++)
        {
            var a = catalog[i];
            if (!a.SatisfiedNeeds.ContainsKey(needName)) continue;
            if (!MeetsStatRequirements(npc, a)) continue;
            if (stamina < a.StaminaCost) continue;

            // Используем TryGetValue вместо прямого доступа, чтобы избежать возможного null
            if (a.SatisfiedNeeds.TryGetValue(needName, out double score))
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    best = a;
                }
            }
        }
        return best;
    }

    private static NpcAction? WeightedRandomOptimized(NpcAction[] catalog, Npc npc, Random rnd)
    {
        var pool = new List<(NpcAction action, double weight)>();
        double stamina = npc.Stamina;
        var needs = npc.Needs;

        for (int i = 0; i < catalog.Length; i++)
        {
            var a = catalog[i];
            if (!MeetsStatRequirements(npc, a)) continue;
            if (stamina < a.StaminaCost) continue;

            double w = 1.0;
            foreach (var kvp in a.SatisfiedNeeds)
            {
                // Используем цикл для поиска потребности вместо LINQ FirstOrDefault
                for (int j = 0; j < needs.Count; j++)
                {
                    if (needs[j].Name == kvp.Key && !needs[j].IsSatisfied)
                    {
                        w += kvp.Value / 20.0;
                        break;
                    }
                }
            }
            pool.Add((a, w));
        }

        if (pool.Count == 0) return null;

        double total = 0;
        for (int i = 0; i < pool.Count; i++)
            total += pool[i].weight;

        double pick = rnd.NextDouble() * total;
        double acc = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += pool[i].weight;
            if (pick <= acc)
                return pool[i].action;
        }
        return pool.Count > 0 ? pool[pool.Count - 1].action : null;
    }

    private static NpcAction? FindTaskActionOptimized(Npc npc, Random rnd, NpcAction[] basicActions)
    {
        var candidates = new List<NpcAction>();
        double stamina = npc.Stamina;

        for (int i = 0; i < basicActions.Length; i++)
        {
            var a = basicActions[i];
            if (MeetsStatRequirements(npc, a) && stamina >= a.StaminaCost && a.StaminaCost > 8)
                candidates.Add(a);
        }
        return candidates.Count > 0 ? candidates[rnd.Next(candidates.Count)] : null;
    }

    private static NpcAction? SelectNonSocialActionOptimized(Npc npc, Random rnd, NpcAction[] basicActions)
    {
        // SocialActionIds должно быть доступно в классе
        var pool = new List<(NpcAction action, double weight)>();
        double stamina = npc.Stamina;
        var needs = npc.Needs;

        for (int i = 0; i < basicActions.Length; i++)
        {
            var a = basicActions[i];
            // Проверка на социальное действие
            if (SocialActionIds.Contains(a.Id)) continue;
            if (!MeetsStatRequirements(npc, a)) continue;
            if (stamina < a.StaminaCost) continue;

            double w = 1.0;
            foreach (var kvp in a.SatisfiedNeeds)
            {
                for (int j = 0; j < needs.Count; j++)
                {
                    if (needs[j].Name == kvp.Key && !needs[j].IsSatisfied)
                    {
                        w += kvp.Value / 20.0;
                        break;
                    }
                }
            }
            pool.Add((a, w));
        }

        if (pool.Count == 0) return null;

        double total = 0;
        for (int i = 0; i < pool.Count; i++)
            total += pool[i].weight;

        double pick = rnd.NextDouble() * total;
        double acc = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += pool[i].weight;
            if (pick <= acc)
                return pool[i].action;
        }
        return pool.Count > 0 ? pool[pool.Count - 1].action : null;
    }

    private static Npc? FindSocialPartnerOptimized(Npc npc, ActionContext ctx, Random rnd)
    {
        var candidates = new List<Npc>();
        int npcLocationId = npc.LocationId;

        for (int i = 0; i < ctx.Npcs.Count; i++)
        {
            var n = ctx.Npcs[i];
            if (!n.IsAlive) continue;
            if (n.Id == npc.Id) continue;
            if (ctx.SocialPairedToday.Contains(n.Id)) continue;
            if (n.LocationId != npcLocationId) continue;
            candidates.Add(n);
        }
        return candidates.Count > 0 ? candidates[rnd.Next(candidates.Count)] : null;
    }

    private static Location? PickTravelLocationOptimized(Npc npc, List<Location> locations, Random rnd)
    {
        if (rnd.NextDouble() < 0.20) return null;

        var explored = new List<Location>();
        for (int i = 0; i < locations.Count; i++)
        {
            var l = locations[i];
            if (l.IsExplored && l.MapState == MapState.Current &&
                (l.Type == LocationType.Building || l.Type == LocationType.Floor ||
                 l.Type == LocationType.Apartment || l.Type == LocationType.Street))
            {
                explored.Add(l);
            }
        }

        if (explored.Count == 0) return null;

        var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);
        if (urgentNeed != null)
        {
            string? resourceNeeded = urgentNeed.Name switch
            {
                "Еда" => "Еда",
                "Вода" => "Вода",
                "Отдых и здоровье" => "Медикаменты",
                _ => null,
            };

            if (resourceNeeded != null)
            {
                var withResource = new List<Location>();
                for (int i = 0; i < explored.Count; i++)
                {
                    var l = explored[i];
                    if (l.ResourceNodes.TryGetValue(resourceNeeded, out double v) && v > 0)
                        withResource.Add(l);
                }
                if (withResource.Count > 0)
                    return withResource[rnd.Next(withResource.Count)];
            }
        }

        return explored[rnd.Next(explored.Count)];
    }


}
