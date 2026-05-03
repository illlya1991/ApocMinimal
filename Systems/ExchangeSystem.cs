using ApocMinimal.Models.ExchangeData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.StatisticsData;

namespace ApocMinimal.Systems;

public static class ExchangeSystem
{
    public static void Apply(PresidentialExchangeEntry ex, List<Npc> npcs, List<Resource> resources)
    {
        foreach (var npc in npcs)
        {
            if (!npc.IsAlive) continue;

            foreach (var eff in ex.StatEffects)
            {
                var stat = npc.Stats.GetByNumber(eff.StatNumber);
                if (stat == null) continue;
                // Permanent multiplicative buff — survives save/load via NpcModifiers table
                stat.AddModifier(new PermanentModifier(
                    $"exchange_{ex.Id}_{stat.Id}",
                    ex.Name,
                    "Обмен",
                    ModifierType.Multiplicative,
                    eff.Multiplier));
            }

            foreach (var eff in ex.NeedEffects)
            {
                var need = npc.Needs.FirstOrDefault(n => n.Id == eff.NeedId);
                if (need != null)
                    need.Level = Math.Clamp(need.Level + eff.LevelDelta, 1, 5);
            }
        }

        foreach (var eff in ex.ResourceEffects)
        {
            var res = resources.FirstOrDefault(r => r.Name == eff.ResourceName);
            if (res != null)
                res.Amount = Math.Round(res.Amount + eff.Amount, 1);
        }
    }
    public static void ApplyFast(PresidentialExchangeEntry ex, List<Npc> npcs, List<Resource> resources)
    {
        // Применяем эффекты без сохранения (сохраним позже одной транзакцией)
        foreach (var npc in npcs)
        {
            if (!npc.IsAlive) continue;

            foreach (var eff in ex.StatEffects)
            {
                var stat = npc.Stats.GetByNumber(eff.StatNumber);
                if (stat == null) continue;

                stat.AddModifier(new PermanentModifier(
                    $"exchange_{ex.Id}_{stat.Id}",
                    ex.Name,
                    "Обмен",
                    ModifierType.Multiplicative,
                    eff.Multiplier));
            }

            foreach (var eff in ex.NeedEffects)
            {
                var need = npc.Needs.FirstOrDefault(n => n.Id == eff.NeedId);
                if (need != null)
                    need.Level = Math.Clamp(need.Level + eff.LevelDelta, 1, 5);
            }
        }

        foreach (var eff in ex.ResourceEffects)
        {
            var res = resources.FirstOrDefault(r => r.Name == eff.ResourceName);
            if (res != null)
                res.Amount = Math.Round(res.Amount + eff.Amount, 1);
        }
    }
}
