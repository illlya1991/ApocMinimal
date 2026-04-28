using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.PersonData;

namespace ApocMinimal.Systems;

/// <summary>
/// Applies stat growth to an NPC after performing an action.
/// Formula: Gain = base × (100 / (stat + 100)) × (Learning / 100) × rnd(0.8..1.2)
/// Uses probabilistic rounding so fractional gains accumulate fairly over time.
/// </summary>
public static class StatGrowthSystem
{
    /// <summary>
    /// Apply stat growth for the given action to the NPC.
    /// Returns a list of (statName, gain) for logging; empty if no growth occurred.
    /// </summary>
    public static List<(string StatName, int Gain)> Apply(Npc npc, NpcAction action, Random rnd, double statGrowthCoeff = 1.0)
    {
        var results = new List<(string, int)>();
        if (action.StatGrowthIds.Count == 0) return results;

        double learning = Math.Max(1, npc.Stats.Learning); // stat #17

        foreach (var kvp in action.StatGrowthIds)
        {
            var characteristic = npc.Stats.GetByNumber(kvp.Key);
            if (characteristic == null) continue;

            double baseGain   = kvp.Value * statGrowthCoeff;
            int    current    = characteristic.FinalValue;

            // Diminishing returns: harder to grow an already-high stat
            double scaleFactor  = 100.0 / (current + 100.0);
            // Learning modifier
            double learningMod  = learning / 100.0;
            // Random variance ±20 %
            double variance     = 0.8 + rnd.NextDouble() * 0.4;

            double gain = baseGain * scaleFactor * learningMod * variance;

            // Probabilistic rounding: floor + chance of +1 based on fractional part
            int intGain     = (int)Math.Floor(gain);
            double fraction = gain - intGain;
            if (rnd.NextDouble() < fraction) intGain++;

            if (intGain > 0)
            {
                characteristic.AddDeviation(intGain);
                results.Add((characteristic.Name, intGain));
            }
        }

        return results;
    }
}
