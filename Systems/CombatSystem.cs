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
    /// Resolve a 1v1 combat between two NPCs. Up to 50 rounds.
    /// Includes special moves: critical hits, dodges, stamina blows, energy bursts.
    /// </summary>
    public static CombatEvent Resolve1v1(Npc attacker, Npc defender, Random rnd, int day)
    {
        var combat = new CombatEvent
        {
            Day          = day,
            AttackerName = attacker.Name,
            DefenderName = defender.Name,
        };

        double atkHp  = attacker.Health;
        double defHp  = defender.Health;
        double damageMod = 1.0; // modified by special moves

        for (int round = 1; round <= MaxRounds; round++)
        {
            if (atkHp <= 0 || defHp <= 0) break;

            // ── Attacker strikes ──────────────────────────────────────────
            var (atkDamage, atkSpecial, atkDesc) = ResolveStrike(attacker, defender, rnd, damageMod);
            defHp      = Math.Max(0, defHp - atkDamage);
            damageMod  = ApplySpecialEffect(atkSpecial, attacker, defender, ref atkHp);

            combat.Rounds.Add(new CombatRound
            {
                RoundNumber     = round,
                AttackerName    = attacker.Name,
                DefenderName    = defender.Name,
                DamageDealt     = atkDamage,
                AttackerHpAfter = atkHp,
                DefenderHpAfter = defHp,
                Special         = atkSpecial,
                Description     = $"Раунд {round}: {attacker.Name} {atkDesc} {atkDamage:F1}% ({defender.Name}: {defHp:F1}%)",
            });

            if (defHp <= 0) break;

            // ── Defender counter-strikes ──────────────────────────────────
            var (defDamage, defSpecial, defDesc) = ResolveStrike(defender, attacker, rnd, 1.0);
            atkHp = Math.Max(0, atkHp - defDamage);
            ApplySpecialEffect(defSpecial, defender, attacker, ref defHp);

            combat.Rounds.Add(new CombatRound
            {
                RoundNumber     = round,
                AttackerName    = defender.Name,
                DefenderName    = attacker.Name,
                DamageDealt     = defDamage,
                AttackerHpAfter = defHp,
                DefenderHpAfter = atkHp,
                Special         = defSpecial,
                Description     = $"  Ответ: {defender.Name} {defDesc} {defDamage:F1}% ({attacker.Name}: {atkHp:F1}%)",
            });

            // ── Morale check: escape ──────────────────────────────────────
            if (attacker.Trait == NpcTrait.Coward && atkHp < 30 && rnd.NextDouble() < 0.35)
            {
                combat.Result = CombatResult.Escaped;
                combat.Rounds.Add(new CombatRound
                {
                    RoundNumber = round, AttackerName = attacker.Name,
                    Description = $"  {attacker.Name} (Трус) бежит с поля боя!",
                });
                break;
            }
        }

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

    private static (double damage, SpecialMoveType special, string desc) ResolveStrike(
        Npc attacker, Npc defender, Random rnd, double mod)
    {
        double damage = CalcDamage(attacker, defender, rnd) * mod;
        var special = SpecialMoveType.None;
        string desc = "наносит";

        double roll = rnd.NextDouble();

        // Critical hit: 10% + Reflexes bonus
        double critChance = 0.10 + attacker.Stats.Reflexes / 1000.0;
        if (roll < critChance)
        {
            damage  *= 2;
            special  = SpecialMoveType.CriticalHit;
            desc     = "критически бьёт на";
        }
        // Dodge: 8% + Agility bonus
        else if (roll < critChance + 0.08 + defender.Stats.Agility / 1000.0)
        {
            damage  = 0;
            special = SpecialMoveType.Dodge;
            desc     = "промахивается —";
        }
        // Energy burst: 5% if has energy
        else if (roll < critChance + 0.13 && attacker.Energy > 20)
        {
            damage  *= 1.5;
            special  = SpecialMoveType.EnergyBurst;
            desc     = "выбрасывает энергию на";
        }
        // Stamina blow: 5%
        else if (roll < critChance + 0.18)
        {
            special = SpecialMoveType.StaminaBlow;
            desc    = "бьёт по выносливости на";
        }

        return (Math.Round(Math.Clamp(damage, 0, 30), 1), special, desc);
    }

    /// <summary>Apply side-effects of special moves. Returns new damageMod for next round.</summary>
    private static double ApplySpecialEffect(
        SpecialMoveType special, Npc attacker, Npc defender, ref double attackerHp)
    {
        switch (special)
        {
            case SpecialMoveType.EnergyBurst:
                attacker.Energy = Math.Max(0, attacker.Energy - 15);
                break;
            case SpecialMoveType.StaminaBlow:
                defender.Stamina = Math.Max(0, defender.Stamina - 20);
                break;
            case SpecialMoveType.Taunt:
                return 1.3; // next round both deal more damage
        }
        return 1.0;
    }

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
