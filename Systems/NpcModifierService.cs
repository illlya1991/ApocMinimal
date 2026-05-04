// Systems/NpcModifierService.cs — FollowerLevel и EvolutionLevel мультипликаторы
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.StatisticsData;

namespace ApocMinimal.Systems;

public static class NpcModifierService
{
    private const string FollowerSource  = "FollowerLevel";
    private const string EvolutionSource = "EvolutionLevel";

    // FollowerLevel 0-5 → мультипликатор ко всем статам
    private static readonly double[] FollowerMults = { 1.0, 1.1, 1.2, 1.3, 1.4, 1.5 };

    // EvolutionLevel 0-10 → базовый множитель уровня (LevelMult из спецификации)
    private static readonly double[] EvoLevelMults = { 0, 1.5, 2.0, 3.0, 5.0, 8.0, 10.0, 12.0, 15.0, 20.0, 30.0 };

    // ── FollowerLevel ────────────────────────────────────────────────────────

    /// <summary>
    /// Пересчитывает модификаторы FollowerLevel для НПС.
    /// Удаляет старые и добавляет новые исходя из текущего npc.FollowerLevel.
    /// Вызывать при изменении FollowerLevel.
    /// </summary>
    public static void ApplyFollowerLevel(Npc npc)
    {
        npc.Stats.RemoveModifiersFromSource(FollowerSource);

        int lvl = Math.Clamp(npc.FollowerLevel, 0, 5);
        if (lvl == 0) return; // ×1.0 — нейтрально, модификатор не нужен

        double mult = FollowerMults[lvl];
        foreach (var stat in npc.Stats.AllStats)
        {
            stat.AddModifier(new PermanentModifier(
                id:     $"follower_{stat.Id}",
                name:   $"Последователь ур.{lvl}",
                source: FollowerSource,
                type:   ModifierType.Multiplicative,
                value:  mult));
        }
    }

    // ── EvolutionLevel ───────────────────────────────────────────────────────

    /// <summary>
    /// Пересчитывает модификаторы EvolutionLevel для НПС.
    /// Применяет взвешенные мультипликаторы по категориям:
    ///   Энергетические (60%), Физические (30%), Ментальные (10%).
    /// Формула: CategoryMult = 1 + LevelMult × Weight
    /// Вызывать при изменении EvolutionLevel.
    /// </summary>
    public static void ApplyEvolutionLevel(Npc npc)
    {
        npc.Stats.RemoveModifiersFromSource(EvolutionSource);

        int lvl = Math.Clamp(npc.EvolutionLevel, 0, 10);
        if (lvl == 0) return; // нет эффекта на 0-м уровне

        double lm = EvoLevelMults[lvl];
        double energyMult   = 1.0 + lm * 0.60;
        double physicalMult = 1.0 + lm * 0.30;
        double mentalMult   = 1.0 + lm * 0.10;

        foreach (var stat in npc.Stats.AllStats)
        {
            double mult = stat.Type switch
            {
                StatType.Energy   => energyMult,
                StatType.Physical => physicalMult,
                StatType.Mental   => mentalMult,
                _                 => 1.0
            };
            if (mult <= 1.0) continue;

            stat.AddModifier(new PermanentModifier(
                id:     $"evo_{stat.Id}",
                name:   $"Энергия ур.{lvl}",
                source: EvolutionSource,
                type:   ModifierType.Multiplicative,
                value:  mult));
        }
    }
}
