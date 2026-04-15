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
                1 => stats.Endurance,
                2 => stats.Toughness,
                3 => stats.Strength,
                4 => stats.RecoveryPhys,
                5 => stats.Reflexes,
                6 => stats.Agility,
                7 => stats.Adaptation,
                8 => stats.Regeneration,
                9 => stats.Sensorics,
                10 => stats.Longevity,

                // Ментальные (11-22)
                11 => stats.Focus,
                12 => stats.Memory,
                13 => stats.Logic,
                14 => stats.Deduction,
                15 => stats.Intelligence,
                16 => stats.Will,
                17 => stats.Learning,
                18 => stats.Flexibility,
                19 => stats.Intuition,
                20 => stats.SocialIntel,
                21 => stats.Creativity,
                22 => stats.Mathematics,

                // Энергетические (23-30)
                23 => stats.EnergyReserve,
                24 => stats.EnergyRecovery,
                25 => stats.Control,
                26 => stats.Concentration,
                27 => stats.Output,
                28 => stats.Precision,
                29 => stats.EnergyResist,
                30 => stats.EnergySense,

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