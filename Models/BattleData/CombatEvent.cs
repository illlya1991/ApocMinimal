namespace ApocMinimal.Models.BattleData;

public enum CombatResult { AttackerWins, DefenderWins, Draw, Escaped }

public enum SpecialMoveType
{
    None,
    CriticalHit,    // ×2 damage
    Dodge,          // defender takes no damage
    StaminaBlow,    // drains defender stamina
    ChakraBurst,    // chakra-powered attack ×1.5 + chakra drain
    Disarm,         // attacker loses 20% damage next round
    Taunt,          // +5 damage both sides next round
}

public class CombatRound
{
    public int RoundNumber { get; set; }
    public string AttackerName { get; set; } = "";
    public string DefenderName { get; set; } = "";
    public double DamageDealt { get; set; }
    public double AttackerHpAfter { get; set; }
    public double DefenderHpAfter { get; set; }
    public SpecialMoveType Special { get; set; } = SpecialMoveType.None;
    public string Description { get; set; } = "";
}

/// <summary>
/// A combat event between two NPCs (or NPC groups). Up to 50 rounds.
/// Damage is expressed as % of max health.
/// </summary>
public class CombatEvent
{
    public int Day { get; set; }
    public string AttackerName { get; set; } = "";
    public string DefenderName { get; set; } = "";
    public CombatResult Result { get; set; }

    public List<CombatRound> Rounds { get; set; } = new();

    public int TotalRounds => Rounds.Count;
    public string ResultLabel => Result switch
    {
        CombatResult.AttackerWins => $"{AttackerName} победил",
        CombatResult.DefenderWins => $"{DefenderName} победил",
        CombatResult.Draw => "Ничья",
        CombatResult.Escaped => "Побег",
        _ => "Неизвестно",
    };
}
