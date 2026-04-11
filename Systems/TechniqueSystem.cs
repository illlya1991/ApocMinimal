using ApocalypseSimulation.Models.StatisticsData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.LocationData;

namespace ApocMinimal.Systems;

/// <summary>
/// Handles applying techniques to NPCs and location clearance rewards.
/// Stat distribution: primary 60%, secondary 30%, tertiary 10%.
/// </summary>
public static class TechniqueSystem
{
    // Stat IDs by category (priority order from concept)
    private static readonly int[] EnergyStats = { 23, 24, 25, 26, 27, 28, 29, 30 };
    private static readonly int[] PhysicalStats = { 3, 9, 1, 5, 7, 2, 6, 4, 8, 10 };
    private static readonly int[] MentalStats = { 16, 13, 12, 19, 11, 21, 22, 15, 17, 14, 18, 20 };

    /// <summary>
    /// Try to apply a technique to an NPC.
    /// Returns true on success; log contains a result message.
    /// </summary>
    public static bool Apply(Technique tech, Npc npc, out string log)
    {
        if (npc.Chakra < tech.ChakraCost)
        { log = $"Недостаточно чакры: {npc.Chakra:F0}/{tech.ChakraCost:F0}"; return false; }

        if (npc.Stamina < tech.StaminaCost)
        { log = $"Недостаточно выносливости: {npc.Stamina:F0}/{tech.StaminaCost:F0}"; return false; }

        // Проверка требований к характеристикам (новая система)
        foreach (var (statId, minVal) in tech.RequiredStats)
        {
            int currentValue = GetStatValue(npc.Stats, statId);
            if (currentValue < minVal)
            {
                string sname = GetStatName(statId);
                log = $"Требуется {sname} ≥ {minVal} (есть {currentValue})";
                return false;
            }
        }

        npc.Chakra = Math.Max(0, npc.Chakra - tech.ChakraCost);
        npc.Stamina = Math.Max(0, npc.Stamina - tech.StaminaCost);

        // Healing techniques restore HP instead of boosting stats
        if (tech.HealAmount > 0)
        {
            double actual = Math.Min(tech.HealAmount, 100 - npc.Health);
            npc.Health = Math.Min(100, npc.Health + tech.HealAmount);
            log = $"{tech.Name} применена → {npc.Name}: здоровье +{actual:F0} → {npc.Health:F0}";
            return true;
        }

        double mult = tech.TechLevel.GetMultiplier();
        double bonus = mult * 3.0;

        var (primary, secondary, tertiary) = tech.TechType switch
        {
            TechniqueType.Physical => (PhysicalStats, EnergyStats, MentalStats),
            TechniqueType.Mental => (MentalStats, EnergyStats, PhysicalStats),
            _ => (EnergyStats, PhysicalStats, MentalStats),
        };

        ApplyGroup(npc, primary, bonus * 0.60);
        ApplyGroup(npc, secondary, bonus * 0.30);
        ApplyGroup(npc, tertiary, bonus * 0.10);

        log = $"{tech.Name} [{tech.TechLevel.ToLabel()} ×{mult}] применена → {npc.Name}";
        return true;
    }

    /// <summary>ОВ reward for clearing a location: Floor=5, Building=50.</summary>
    public static double ClearanceReward(LocationType type) => type switch
    {
        LocationType.Floor => 5.0,
        LocationType.Building => 50.0,
        _ => 0.0,
    };

    /// <summary>
    /// Применение бонусов к группе характеристик (через отклонения)
    /// </summary>
    private static void ApplyGroup(Npc npc, int[] ids, double total)
    {
        if (ids.Length == 0) return;
        double perStat = total / ids.Length;
        foreach (var id in ids)
        {
            // Добавляем отклонение вместо прямого изменения
            int delta = (int)Math.Round(perStat);
            if (delta != 0)
            {
                AddStatDeviation(npc.Stats, id, delta);
            }
        }
    }

    /// <summary>
    /// Получение значения характеристики по ID (1-30)
    /// </summary>
    private static int GetStatValue(Statistics stats, int statId)
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

    /// <summary>
    /// Добавление отклонения к характеристике по ID
    /// </summary>
    private static void AddStatDeviation(Statistics stats, int statId, int delta)
    {
        var stat = GetCharacteristicById(stats, statId);
        stat?.AddDeviation(delta);
    }

    /// <summary>
    /// Получение объекта характеристики по ID
    /// </summary>
    private static Characteristic? GetCharacteristicById(Statistics stats, int statId)
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

            _ => null
        };
    }

    /// <summary>
    /// Получение имени характеристики по ID
    /// </summary>
    private static string GetStatName(int statId)
    {
        return statId switch
        {
            // Физические
            1 => "Выносливость",
            2 => "Стойкость",
            3 => "Сила",
            4 => "Восстановление (физ)",
            5 => "Рефлексы",
            6 => "Ловкость",
            7 => "Адаптация",
            8 => "Регенерация",
            9 => "Сенсорика",
            10 => "Долголетие",

            // Ментальные
            11 => "Фокус",
            12 => "Память",
            13 => "Логика",
            14 => "Дедукция",
            15 => "Интеллект",
            16 => "Воля",
            17 => "Обучение",
            18 => "Гибкость",
            19 => "Интуиция",
            20 => "Социальный интеллект",
            21 => "Творчество",
            22 => "Математика",

            // Энергетические
            23 => "Запас энергии",
            24 => "Восстановление (энерг)",
            25 => "Контроль",
            26 => "Концентрация",
            27 => "Выход",
            28 => "Тонкость",
            29 => "Устойчивость",
            30 => "Восприятие энергии",

            _ => $"Stat_{statId}"
        };
    }
}