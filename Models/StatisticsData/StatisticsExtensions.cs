using ApocalypseSimulation.Models.StatisticsData;

namespace ApocMinimal.Systems
{
    public static class StatisticsExtensions
    {
        public static int GetStatValue(this Statistics stats, int statId)
        {
            return statId switch
            {
                // Физические (1-10)
                1 => stats.Endurance.FinalValue,
                2 => stats.Toughness.FinalValue,
                3 => stats.Strength.FinalValue,
                4 => stats.RecoveryPhys.FinalValue,
                5 => stats.Reflexes.FinalValue,
                6 => stats.Agility.FinalValue,
                7 => stats.Adaptation.FinalValue,
                8 => stats.Regeneration.FinalValue,
                9 => stats.Sensorics.FinalValue,
                10 => stats.Longevity.FinalValue,

                // Ментальные (11-22)
                11 => stats.Focus.FinalValue,
                12 => stats.Memory.FinalValue,
                13 => stats.Logic.FinalValue,
                14 => stats.Deduction.FinalValue,
                15 => stats.Intelligence.FinalValue,
                16 => stats.Will.FinalValue,
                17 => stats.Learning.FinalValue,
                18 => stats.Flexibility.FinalValue,
                19 => stats.Intuition.FinalValue,
                20 => stats.SocialIntel.FinalValue,
                21 => stats.Creativity.FinalValue,
                22 => stats.Mathematics.FinalValue,

                // Энергетические (23-30)
                23 => stats.EnergyReserve.FinalValue,
                24 => stats.EnergyRecovery.FinalValue,
                25 => stats.Control.FinalValue,
                26 => stats.Concentration.FinalValue,
                27 => stats.Output.FinalValue,
                28 => stats.Precision.FinalValue,
                29 => stats.EnergyResist.FinalValue,
                30 => stats.EnergySense.FinalValue,

                _ => 0
            };
        }

        public static bool TryGetStatValue(this Statistics stats, int statId, out int value)
        {
            value = GetStatValue(stats, statId);
            return value != 0;
        }
    }
}