namespace ApocMinimal.Models.BattleData;

public enum CombatResult { AttackerWins, DefenderWins, Draw, Escaped }

public class CombatRound
{
    public int RoundNumber { get; set; }
    public string AttackerName { get; set; } = "";
    public string DefenderName { get; set; } = "";
    public double DamageDealt { get; set; }
    public double AttackerHpAfter { get; set; }
    public double DefenderHpAfter { get; set; }
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
