// ActionSystem.cs - обновленный импорт и использование

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
/// AI action selection: each NPC performs up to 23 actions per day
/// (max 3 special). Actions chosen based on needs and stats.
/// </summary>
public static class ActionSystem
{
    private const int MaxActionsPerDay = 23;
    private const int MaxSpecialPerDay = 3;

    public static List<ActionLogEntry> ProcessDayActions(Npc npc, Random rnd, int day)
    {
        var log = new List<ActionLogEntry>();
        if (!npc.IsAlive) return log;

        NeedSystem.RestoreStamina(npc);

        int totalActions = 0;
        int specialActions = 0;
        double staminaLeft = npc.Stamina;
        int currentHour = 0;

        // Трус: 50% шанс пропустить день
        if (npc.Trait == NpcTrait.Coward && rnd.NextDouble() < 0.50)
        {
            log.Add(new ActionLogEntry
            {
                Time = "00:00",
                Text = $"{npc.Name} (Трус) отказался действовать.",
                Color = "#fbbf24",
            });
            NeedSystem.ApplyDailyDecay(npc);
            NeedSystem.ApplyPenalties(npc);
            return log;
        }

        int maxActions = MaxActionsPerDay;
        if (npc.CharTraits.Contains(CharacterTrait.Lazy)) maxActions -= 10;

        while (totalActions < maxActions && staminaLeft > 2)
        {
            var urgentNeed = NeedSystem.GetMostUrgentNeed(npc);
            NpcAction? action = null;

            if (urgentNeed != null)
            {
                // Поиск в базовых действиях
                for (int i = 0; i < NpcActionCatalog.Basic.Length; i++)
                {
                    NpcAction a = NpcActionCatalog.Basic[i];
                    if (a.SatisfiedNeeds.ContainsKey(urgentNeed.Name) &&
                        MeetsStatRequirements(npc, a) &&
                        staminaLeft >= a.StaminaCost)
                    {
                        if (action == null || a.SatisfiedNeeds[urgentNeed.Name] > action.SatisfiedNeeds[urgentNeed.Name])
                        {
                            action = a;
                        }
                    }
                }
            }

            if (action == null && specialActions < MaxSpecialPerDay && urgentNeed != null)
            {
                // Поиск в специальных действиях
                for (int i = 0; i < NpcActionCatalog.Special.Length; i++)
                {
                    NpcAction a = NpcActionCatalog.Special[i];
                    if (a.SatisfiedNeeds.ContainsKey(urgentNeed.Name) &&
                        MeetsStatRequirements(npc, a) &&
                        staminaLeft >= a.StaminaCost)
                    {
                        if (action == null || a.SatisfiedNeeds[urgentNeed.Name] > action.SatisfiedNeeds[urgentNeed.Name])
                        {
                            action = a;
                            specialActions++;
                        }
                    }
                }
            }

            if (action == null)
            {
                // Выбираем случайное базовое действие
                List<NpcAction> candidates = new List<NpcAction>();
                for (int i = 0; i < NpcActionCatalog.Basic.Length; i++)
                {
                    NpcAction a = NpcActionCatalog.Basic[i];
                    if (MeetsStatRequirements(npc, a) && staminaLeft >= a.StaminaCost)
                    {
                        candidates.Add(a);
                    }
                }
                if (candidates.Count == 0) break;
                action = candidates[rnd.Next(candidates.Count)];
            }

            staminaLeft -= action.StaminaCost;
            npc.Stamina = Math.Clamp(npc.Stamina - action.StaminaCost, 0, 100);

            // Удовлетворение потребностей
            foreach (var kvp in action.SatisfiedNeeds)
            {
                NeedSystem.SatisfyNeed(npc, kvp.Key, kvp.Value);
            }

            npc.Remember(new MemoryEntry(day, MemoryType.Action, $"[{currentHour:00}:00] {action.Name}"));

            log.Add(new ActionLogEntry
            {
                Time = $"{currentHour:00}:00",
                Text = action.Name,
                Color = action.Category == ActionCategory.Special ? "#e879f9" : "#c9d1d9",
            });

            totalActions++;
            currentHour = Math.Min(currentHour + 1, 22);
        }

        // Предупреждения о критических потребностях
        NeedSystem.ApplyDailyDecay(npc);
        NeedSystem.ApplyPenalties(npc);

        for (int i = 0; i < npc.Needs.Count; i++)
        {
            Need need = npc.Needs[i];
            if (need.IsCritical)
            {
                log.Add(new ActionLogEntry
                {
                    Time = "23:00",
                    Text = $"[!] Критическая нужда: {need.Name} ({need.Value:F0}%)",
                    Color = "#f87171",
                    IsAlert = true,
                });
            }
        }

        return log;
    }

    private static bool MeetsStatRequirements(Npc npc, NpcAction action)
    {
        foreach (var kvp in action.RequiredStats)
        {
            int currentValue = npc.Stats.GetStatValue(kvp.Key);
            if (currentValue < kvp.Value)
                return false;
        }
        return true;
    }
}