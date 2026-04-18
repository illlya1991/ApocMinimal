using ApocMinimal.Models.ExchangeData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.ResourceData;

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
                stat.BaseValue = Math.Max(1, (int)Math.Round(stat.BaseValue * eff.Multiplier));
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
