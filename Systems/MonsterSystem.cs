using ApocMinimal.Models.PersonData;

namespace ApocMinimal.Systems;

public class MonsterCombatResult
{
    public bool NpcWon { get; set; }
    public bool MonsterKilled { get; set; }
    public double OvReward { get; set; }
    public bool HeartDropped { get; set; }
    public double NpcHealthLost { get; set; }
    public List<string> Log { get; set; } = new();
}

/// <summary>
/// NPC vs Monster combat using formula: damage = attack × (1 - defense / (defense + 100)).
/// Simpler than the full CombatSystem — used for location clearance.
/// </summary>
public static class MonsterSystem
{
    private const int MaxRounds = 20;

    /// <summary>Resolve an NPC fighting a single monster.</summary>
    public static MonsterCombatResult Resolve(Npc npc, Monster monster, Random rnd)
    {
        var result = new MonsterCombatResult();
        double npcHp  = npc.Health;
        double monHp  = monster.Health;

        for (int round = 1; round <= MaxRounds && npcHp > 0 && monHp > 0; round++)
        {
            // NPC attacks monster
            double npcAtk = NpcAttack(npc);
            double monDef = MonsterStat(monster, 9); // Toughness equivalent
            double npcDmg = CalcDamage(npcAtk, monDef) * (0.8 + rnd.NextDouble() * 0.4);
            monHp = Math.Max(0, monHp - npcDmg);

            result.Log.Add($"Раунд {round}: {npc.Name} наносит {npcDmg:F1} урона монстру ({monHp:F1} ОЗ)");

            if (monHp <= 0) break;

            // Monster attacks NPC
            double monAtk = MonsterStat(monster, 3); // Strength equivalent
            double npcDef = npc.Stats.Toughness + npc.Stats.Endurance / 2.0;
            double monDmg = CalcDamage(monAtk, npcDef) * (0.8 + rnd.NextDouble() * 0.4);
            npcHp = Math.Max(0, npcHp - monDmg);

            result.Log.Add($"  Монстр: {monDmg:F1} урона {npc.Name} ({npcHp:F1} ОЗ)");
        }

        result.NpcHealthLost  = npc.Health - npcHp;
        npc.Health            = npcHp;
        result.MonsterKilled  = monHp <= 0;
        result.NpcWon         = npcHp > 0;

        if (result.MonsterKilled)
        {
            result.OvReward    = monster.OvReward;
            result.HeartDropped = rnd.NextDouble() < monster.HeartChance;
            if (result.HeartDropped)
            {
                result.OvReward += 10;
                result.Log.Add($"  Сердце монстра подобрано! +10 ОР");
            }
            result.Log.Add($"  {npc.Name} победил {monster.Name}! Награда: {result.OvReward:F0} ОР");
        }
        else
        {
            result.Log.Add($"  {npc.Name} отступил (HP: {npcHp:F1})");
        }

        return result;
    }

    /// <summary>
    /// A group of NPCs clears a location monster.
    /// Each living NPC fights sequentially until monster is dead or all NPCs retreated.
    /// </summary>
    public static MonsterCombatResult ResolveGroup(List<Npc> npcs, Monster monster, Random rnd)
    {
        var combined = new MonsterCombatResult();
        double monHp = monster.Health;

        foreach (var npc in npcs.Where(n => n.IsAlive && n.Stamina > 10))
        {
            var r = Resolve(npc, new Monster
            {
                Name        = monster.Name,
                Type        = monster.Type,
                Health      = monHp,
                DangerLevel = monster.DangerLevel,
                Stats       = monster.Stats,
                OvReward    = monster.OvReward,
                HeartChance = monster.HeartChance,
            }, rnd);

            monHp -= r.NpcHealthLost; // proxy: each hit reduces monster health pool
            monHp  = Math.Max(0, r.MonsterKilled ? 0 : monHp);

            combined.Log.AddRange(r.Log);
            combined.NpcHealthLost += r.NpcHealthLost;
            combined.OvReward      += r.OvReward;
            if (r.HeartDropped) combined.HeartDropped = true;

            if (r.MonsterKilled) { combined.MonsterKilled = true; combined.NpcWon = true; break; }
        }

        return combined;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>damage = attack × (1 - defense / (defense + 100))</summary>
    private static double CalcDamage(double attack, double defense)
        => attack * (1.0 - defense / (defense + 100.0));

    private static double NpcAttack(Npc npc)
        => (npc.Stats.Strength + npc.Stats.Agility) / 2.0;

    private static double MonsterStat(Monster monster, int statId)
        => monster.Stats.TryGetValue(statId, out var v) ? v : 50;
}
