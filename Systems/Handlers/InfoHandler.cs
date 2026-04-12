using System;
using System.Collections.Generic;
using System.Linq;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.UIData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Универсальный обработчик для группы "Информация"
/// Действия: ViewInfo (просмотр информации)
/// </summary>
public class InfoHandler : BaseActionHandler
{
    // Статический метод для вывода информации об NPC (может вызываться из любого места)
    public static void ShowNpcInfoStatic(Npc target, Action<string, string> logAction)
    {
        if (target == null) return;
        if (!target.IsAlive)
        {
            logAction?.Invoke($"{target.Name} мертв", LogEntry.ColorDanger);
            return;
        }

        // Заголовок
        logAction?.Invoke($"── {target.Name} [{target.GenderLabel}] {target.Age} лет  {target.Profession}", LogEntry.ColorDay);

        // Основные характеристики
        logAction?.Invoke($"  HP:{target.Health:F0}  Выносл:{target.Stamina:F0}  Чакра:{target.Chakra:F0}  Вера:{target.Faith:F0}",
            HealthColor(target.Health));
        logAction?.Invoke($"  Страх:{target.Fear:F0}  Доверие:{target.Trust:F0}  Инициатива:{target.Initiative:F0}  Последователь:[{target.FollowerLabel}]",
            LogEntry.ColorNormal);

        // Черты характера
        if (target.CharTraits.Count > 0)
            logAction?.Invoke($"  Черты: {string.Join(", ", target.CharTraits.Select(c => c.ToLabel()))}", LogEntry.ColorNormal);

        // Эмоции
        if (target.Emotions.Count > 0)
            logAction?.Invoke($"  Эмоции: {string.Join("  ", target.Emotions.Select(em => $"{em.Name} {em.Percentage:F0}%"))}",
                LogEntry.ColorSpeech);

        // Цели
        logAction?.Invoke($"  Цель: {target.Goal}", LogEntry.ColorNormal);
        logAction?.Invoke($"  Мечта: {target.Dream}", LogEntry.ColorNormal);
        logAction?.Invoke($"  Желание: {target.Desire}", LogEntry.ColorNormal);

        // Специализации
        if (target.Specializations.Count > 0)
            logAction?.Invoke($"  Специализации: {string.Join(", ", target.Specializations)}", LogEntry.ColorNormal);

        // Потребности
        logAction?.Invoke("  ПОТРЕБНОСТИ:", LogEntry.ColorDay);
        foreach (var need in target.Needs.Where(n => n.IsUrgent || n.IsCritical))
        {
            logAction?.Invoke($"    {need.Name} [{need.Level}]: {need.Value:F0}% {(need.IsCritical ? "КРИТИЧНО" : "")}",
                need.IsCritical ? LogEntry.ColorDanger : LogEntry.ColorWarning);
        }

        // Характеристики
        LogStatsStatic(target, logAction);

        // Память
        if (target.Memory.Count > 0)
        {
            logAction?.Invoke("  ПАМЯТЬ (последние):", LogEntry.ColorDay);
            foreach (var mem in target.Memory.TakeLast(5))
                logAction?.Invoke($"    {mem.Icon} День {mem.Day}: {mem.Text}", LogEntry.ColorNormal);
        }

        logAction?.Invoke("──────────────────────────────────────────────", LogEntry.ColorNormal);
    }

    private static void LogStatsStatic(Npc target, Action<string, string> logAction)
    {
        // Физические
        logAction?.Invoke("  ФИЗИЧЕСКИЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        logAction?.Invoke($"    Выносливость:          {target.Stats.Endurance.FinalValue,3}  (база: {target.Stats.Endurance.FullBase})",
            StatColor(target.Stats.Endurance.FinalValue));
        logAction?.Invoke($"    Стойкость:             {target.Stats.Toughness.FinalValue,3}  (база: {target.Stats.Toughness.FullBase})",
            StatColor(target.Stats.Toughness.FinalValue));
        logAction?.Invoke($"    Сила:                  {target.Stats.Strength.FinalValue,3}  (база: {target.Stats.Strength.FullBase})",
            StatColor(target.Stats.Strength.FinalValue));
        logAction?.Invoke($"    Восстановление (физ):  {target.Stats.RecoveryPhys.FinalValue,3}  (база: {target.Stats.RecoveryPhys.FullBase})",
            StatColor(target.Stats.RecoveryPhys.FinalValue));
        logAction?.Invoke($"    Рефлексы:              {target.Stats.Reflexes.FinalValue,3}  (база: {target.Stats.Reflexes.FullBase})",
            StatColor(target.Stats.Reflexes.FinalValue));
        logAction?.Invoke($"    Ловкость:              {target.Stats.Agility.FinalValue,3}  (база: {target.Stats.Agility.FullBase})",
            StatColor(target.Stats.Agility.FinalValue));
        logAction?.Invoke($"    Адаптация:             {target.Stats.Adaptation.FinalValue,3}  (база: {target.Stats.Adaptation.FullBase})",
            StatColor(target.Stats.Adaptation.FinalValue));
        logAction?.Invoke($"    Регенерация:           {target.Stats.Regeneration.FinalValue,3}  (база: {target.Stats.Regeneration.FullBase})",
            StatColor(target.Stats.Regeneration.FinalValue));
        logAction?.Invoke($"    Сенсорика:             {target.Stats.Sensorics.FinalValue,3}  (база: {target.Stats.Sensorics.FullBase})",
            StatColor(target.Stats.Sensorics.FinalValue));
        logAction?.Invoke($"    Долголетие:            {target.Stats.Longevity.FinalValue,3}  (база: {target.Stats.Longevity.FullBase})",
            StatColor(target.Stats.Longevity.FinalValue));

        // Ментальные
        logAction?.Invoke("  МЕНТАЛЬНЫЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        logAction?.Invoke($"    Фокус:                 {target.Stats.Focus.FinalValue,3}  (база: {target.Stats.Focus.FullBase})",
            StatColor(target.Stats.Focus.FinalValue));
        logAction?.Invoke($"    Память:                {target.Stats.Memory.FinalValue,3}  (база: {target.Stats.Memory.FullBase})",
            StatColor(target.Stats.Memory.FinalValue));
        logAction?.Invoke($"    Логика:                {target.Stats.Logic.FinalValue,3}  (база: {target.Stats.Logic.FullBase})",
            StatColor(target.Stats.Logic.FinalValue));
        logAction?.Invoke($"    Дедукция:              {target.Stats.Deduction.FinalValue,3}  (база: {target.Stats.Deduction.FullBase})",
            StatColor(target.Stats.Deduction.FinalValue));
        logAction?.Invoke($"    Интеллект:             {target.Stats.Intelligence.FinalValue,3}  (база: {target.Stats.Intelligence.FullBase})",
            StatColor(target.Stats.Intelligence.FinalValue));
        logAction?.Invoke($"    Воля:                  {target.Stats.Will.FinalValue,3}  (база: {target.Stats.Will.FullBase})",
            StatColor(target.Stats.Will.FinalValue));
        logAction?.Invoke($"    Обучение:              {target.Stats.Learning.FinalValue,3}  (база: {target.Stats.Learning.FullBase})",
            StatColor(target.Stats.Learning.FinalValue));
        logAction?.Invoke($"    Гибкость:              {target.Stats.Flexibility.FinalValue,3}  (база: {target.Stats.Flexibility.FullBase})",
            StatColor(target.Stats.Flexibility.FinalValue));
        logAction?.Invoke($"    Интуиция:              {target.Stats.Intuition.FinalValue,3}  (база: {target.Stats.Intuition.FullBase})",
            StatColor(target.Stats.Intuition.FinalValue));
        logAction?.Invoke($"    Соц. интеллект:        {target.Stats.SocialIntel.FinalValue,3}  (база: {target.Stats.SocialIntel.FullBase})",
            StatColor(target.Stats.SocialIntel.FinalValue));
        logAction?.Invoke($"    Творчество:            {target.Stats.Creativity.FinalValue,3}  (база: {target.Stats.Creativity.FullBase})",
            StatColor(target.Stats.Creativity.FinalValue));
        logAction?.Invoke($"    Математика:            {target.Stats.Mathematics.FinalValue,3}  (база: {target.Stats.Mathematics.FullBase})",
            StatColor(target.Stats.Mathematics.FinalValue));

        // Энергетические
        logAction?.Invoke("  ЭНЕРГЕТИЧЕСКИЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        logAction?.Invoke($"    Запас энергии:         {target.Stats.EnergyReserve.FinalValue,3}  (база: {target.Stats.EnergyReserve.FullBase})",
            StatColor(target.Stats.EnergyReserve.FinalValue));
        logAction?.Invoke($"    Восстановление (энерг):{target.Stats.EnergyRecovery.FinalValue,3}  (база: {target.Stats.EnergyRecovery.FullBase})",
            StatColor(target.Stats.EnergyRecovery.FinalValue));
        logAction?.Invoke($"    Контроль:              {target.Stats.Control.FinalValue,3}  (база: {target.Stats.Control.FullBase})",
            StatColor(target.Stats.Control.FinalValue));
        logAction?.Invoke($"    Концентрация:          {target.Stats.Concentration.FinalValue,3}  (база: {target.Stats.Concentration.FullBase})",
            StatColor(target.Stats.Concentration.FinalValue));
        logAction?.Invoke($"    Выход:                 {target.Stats.Output.FinalValue,3}  (база: {target.Stats.Output.FullBase})",
            StatColor(target.Stats.Output.FinalValue));
        logAction?.Invoke($"    Тонкость:              {target.Stats.Precision.FinalValue,3}  (база: {target.Stats.Precision.FullBase})",
            StatColor(target.Stats.Precision.FinalValue));
        logAction?.Invoke($"    Устойчивость (энерг):  {target.Stats.EnergyResist.FinalValue,3}  (база: {target.Stats.EnergyResist.FullBase})",
            StatColor(target.Stats.EnergyResist.FinalValue));
        logAction?.Invoke($"    Восприятие энергии:    {target.Stats.EnergySense.FinalValue,3}  (база: {target.Stats.EnergySense.FullBase})",
            StatColor(target.Stats.EnergySense.FinalValue));
    }

    private static string HealthColor(double hp) =>
        hp < 30 ? LogEntry.ColorDanger : hp < 60 ? LogEntry.ColorWarning : LogEntry.ColorSuccess;

    private static string StatColor(int value) =>
        value >= 75 ? LogEntry.ColorSuccess : value >= 50 ? LogEntry.ColorNormal : LogEntry.ColorWarning;

    public InfoHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
        : base(db, rnd, logAction) { }

    public override string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        if (!parameters.ContainsKey("_actionKey") || parameters["_actionKey"] == null)
            return "Не указано действие";

        var actionKey = parameters["_actionKey"].ToString();

        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не указан целевой NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Ошибка: неверный тип целевого NPC";

        if (actionKey == "ViewInfo")
        {
            ShowNpcInfoStatic(target, _logAction);
            return $"Просмотрена информация о {target.Name}";
        }

        return $"Неизвестное действие: {actionKey}";
    }
}