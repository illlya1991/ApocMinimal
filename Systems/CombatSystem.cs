using ApocMinimal.Models.BattleData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Systems;

/// <summary>
/// Event-based combat system. Up to 50 rounds. Damage expressed as % of health.
/// Supports 1v1 and mass combat.
/// </summary>
public static class CombatSystem
{
    private const int MaxRounds = 50;

    /// <summary>
    /// Resolve a 1v1 combat between two NPCs.
    /// </summary>
    public static CombatEvent Resolve1v1(Npc attacker, Npc defender, Random rnd, int day)
    {
        var combat = new CombatEvent
        {
            Day          = day,
            AttackerName = attacker.Name,
            DefenderName = defender.Name,
        };

        double atkHp = attacker.Health;
        double defHp = defender.Health;

        for (int round = 1; round <= MaxRounds; round++)
        {
            if (atkHp <= 0 || defHp <= 0) break;

            // Attacker strikes
            double atkDamage = CalcDamage(attacker, defender, rnd);
            defHp = Math.Max(0, defHp - atkDamage);

            var roundA = new CombatRound
            {
                RoundNumber      = round,
                AttackerName     = attacker.Name,
                DefenderName     = defender.Name,
                DamageDealt      = atkDamage,
                AttackerHpAfter  = atkHp,
                DefenderHpAfter  = defHp,
                Description      = $"Раунд {round}: {attacker.Name} наносит {atkDamage:F1}% урона ({defender.Name}: {defHp:F1}%)",
            };
            combat.Rounds.Add(roundA);

            if (defHp <= 0) break;

            // Defender counter-strikes
            double defDamage = CalcDamage(defender, attacker, rnd);
            atkHp = Math.Max(0, atkHp - defDamage);

            var roundD = new CombatRound
            {
                RoundNumber      = round,
                AttackerName     = defender.Name,
                DefenderName     = attacker.Name,
                DamageDealt      = defDamage,
                AttackerHpAfter  = defHp,
                DefenderHpAfter  = atkHp,
                Description      = $"  Ответ: {defender.Name} наносит {defDamage:F1}% урона ({attacker.Name}: {atkHp:F1}%)",
            };
            combat.Rounds.Add(roundD);

            // Escape check (Coward or Fear > 70)
            if (attacker.Trait == NpcTrait.Coward && rnd.NextDouble() < 0.25)
            {
                combat.Result = CombatResult.Escaped;
                break;
            }
        }

        // Apply final HP
        attacker.Health = atkHp;
        defender.Health = defHp;

        if (combat.Result != CombatResult.Escaped)
        {
            combat.Result = atkHp <= 0 && defHp <= 0 ? CombatResult.Draw
                          : atkHp <= 0               ? CombatResult.DefenderWins
                          : defHp <= 0               ? CombatResult.AttackerWins
                          : atkHp > defHp            ? CombatResult.AttackerWins
                                                     : CombatResult.DefenderWins;
        }

        return combat;
    }

    /// <summary>
    /// Resolve a group vs group combat. Each side takes the NPC with highest Сила as champion.
    /// </summary>
    public static CombatEvent ResolveMass(
        List<Npc> attackers, List<Npc> defenders, Random rnd, int day)
    {
        var atkChamp = attackers.OrderByDescending(n => n.Stats.Strength).First();
        var defChamp = defenders.OrderByDescending(n => n.Stats.Strength).First();

        var combat = Resolve1v1(atkChamp, defChamp, rnd, day);
        combat.AttackerName = $"Группа ({atkChamp.Name})";
        combat.DefenderName = $"Группа ({defChamp.Name})";

        // Splash damage: losing side loses 5–15% HP for each member
        var losers = combat.Result == CombatResult.AttackerWins ? defenders : attackers;
        foreach (var npc in losers.Where(n => n != (combat.Result == CombatResult.AttackerWins ? defChamp : atkChamp)))
            npc.Health = Math.Max(0, npc.Health - rnd.Next(5, 16));

        return combat;
    }

    // ── Private ─────────────────────────────────────────────────────────────

    private static double CalcDamage(Npc attacker, Npc defender, Random rnd)
    {
        // attack = (Сила + Ловкость) / 2
        double attack = (attacker.Stats.Strength + attacker.Stats.Agility) / 2.0;
        // defense = (Стойкость + Выносливость) / 2
        double defense = (defender.Stats.Toughness + defender.Stats.Endurance) / 2.0;

        if (attacker.CharTraits.Contains(CharacterTrait.Brave))    attack *= 1.1;
        if (attacker.CharTraits.Contains(CharacterTrait.Cowardly)) attack *= 0.8;

        // Concept formula: damage = attack × (1 - defense / (defense + 100))
        double base_ = attack * (1.0 - defense / (defense + 100.0));

        double roll = rnd.Next(70, 131) / 100.0;  // ±30% variance
        double damage = base_ * roll * 0.15;       // scale to % of health

        // Рефлексы (stat 5) of defender: dodge chance
        double dodgeChance = defender.Stats.Reflexes / 500.0;
        if (rnd.NextDouble() < dodgeChance) damage *= 0.1;

        return Math.Round(Math.Clamp(damage, 0, 30), 1);  // cap at 30% per hit
    }
}
