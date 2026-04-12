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
/// Обробник перегляду інформації про NPC
/// </summary>
public class ViewInfoHandler : BaseActionHandler
{
    public ViewInfoHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
        : base(db, rnd, logAction) { }

    public override string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        // Перевірка параметрів
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не вказано цільового NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Помилка: невірний тип цільового NPC";
        if (!target.IsAlive) return $"{target.Name} мертвий";

        // Виведення заголовка
        Log($"── {target.Name} [{target.GenderLabel}] {target.Age} лет  {target.Profession}", LogEntry.ColorDay);

        // Основні характеристики
        Log($"  HP:{target.Health:F0}  Выносл:{target.Stamina:F0}  Чакра:{target.Chakra:F0}  Вера:{target.Faith:F0}", HealthColor(target.Health));
        Log($"  Страх:{target.Fear:F0}  Доверие:{target.Trust:F0}  Инициатива:{target.Initiative:F0}  Последователь:[{target.FollowerLabel}]", LogEntry.ColorNormal);

        // Черты характера
        if (target.CharTraits.Count > 0)
            Log($"  Черты: {string.Join(", ", target.CharTraits.Select(c => c.ToLabel()))}", LogEntry.ColorNormal);

        // Эмоции
        if (target.Emotions.Count > 0)
            Log($"  Эмоции: {string.Join("  ", target.Emotions.Select(em => $"{em.Name} {em.Percentage:F0}%"))}", LogEntry.ColorSpeech);

        // Цели и желания
        Log($"  Цель: {target.Goal}", LogEntry.ColorNormal);
        Log($"  Мечта: {target.Dream}", LogEntry.ColorNormal);
        Log($"  Желание: {target.Desire}", LogEntry.ColorNormal);

        // Специализации
        if (target.Specializations.Count > 0)
            Log($"  Специализации: {string.Join(", ", target.Specializations)}", LogEntry.ColorNormal);

        // Потребности
        Log("  ПОТРЕБНОСТИ:", LogEntry.ColorDay);
        foreach (var need in target.Needs.Where(n => n.IsUrgent || n.IsCritical))
        {
            Log($"    {need.Name} [{need.Level}]: {need.Value:F0}% {(need.IsCritical ? "КРИТИЧНО" : "")}",
                need.IsCritical ? LogEntry.ColorDanger : LogEntry.ColorWarning);
        }

        // Физические характеристики
        Log("  ФИЗИЧЕСКИЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        Log($"    Выносливость:          {target.Stats.Endurance.FinalValue,3}  (база: {target.Stats.Endurance.FullBase})", StatColor(target.Stats.Endurance.FinalValue));
        Log($"    Стойкость:             {target.Stats.Toughness.FinalValue,3}  (база: {target.Stats.Toughness.FullBase})", StatColor(target.Stats.Toughness.FinalValue));
        Log($"    Сила:                  {target.Stats.Strength.FinalValue,3}  (база: {target.Stats.Strength.FullBase})", StatColor(target.Stats.Strength.FinalValue));
        Log($"    Восстановление (физ):  {target.Stats.RecoveryPhys.FinalValue,3}  (база: {target.Stats.RecoveryPhys.FullBase})", StatColor(target.Stats.RecoveryPhys.FinalValue));
        Log($"    Рефлексы:              {target.Stats.Reflexes.FinalValue,3}  (база: {target.Stats.Reflexes.FullBase})", StatColor(target.Stats.Reflexes.FinalValue));
        Log($"    Ловкость:              {target.Stats.Agility.FinalValue,3}  (база: {target.Stats.Agility.FullBase})", StatColor(target.Stats.Agility.FinalValue));
        Log($"    Адаптация:             {target.Stats.Adaptation.FinalValue,3}  (база: {target.Stats.Adaptation.FullBase})", StatColor(target.Stats.Adaptation.FinalValue));
        Log($"    Регенерация:           {target.Stats.Regeneration.FinalValue,3}  (база: {target.Stats.Regeneration.FullBase})", StatColor(target.Stats.Regeneration.FinalValue));
        Log($"    Сенсорика:             {target.Stats.Sensorics.FinalValue,3}  (база: {target.Stats.Sensorics.FullBase})", StatColor(target.Stats.Sensorics.FinalValue));
        Log($"    Долголетие:            {target.Stats.Longevity.FinalValue,3}  (база: {target.Stats.Longevity.FullBase})", StatColor(target.Stats.Longevity.FinalValue));

        // Ментальные характеристики
        Log("  МЕНТАЛЬНЫЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        Log($"    Фокус:                 {target.Stats.Focus.FinalValue,3}  (база: {target.Stats.Focus.FullBase})", StatColor(target.Stats.Focus.FinalValue));
        Log($"    Память:                {target.Stats.Memory.FinalValue,3}  (база: {target.Stats.Memory.FullBase})", StatColor(target.Stats.Memory.FinalValue));
        Log($"    Логика:                {target.Stats.Logic.FinalValue,3}  (база: {target.Stats.Logic.FullBase})", StatColor(target.Stats.Logic.FinalValue));
        Log($"    Дедукция:              {target.Stats.Deduction.FinalValue,3}  (база: {target.Stats.Deduction.FullBase})", StatColor(target.Stats.Deduction.FinalValue));
        Log($"    Интеллект:             {target.Stats.Intelligence.FinalValue,3}  (база: {target.Stats.Intelligence.FullBase})", StatColor(target.Stats.Intelligence.FinalValue));
        Log($"    Воля:                  {target.Stats.Will.FinalValue,3}  (база: {target.Stats.Will.FullBase})", StatColor(target.Stats.Will.FinalValue));
        Log($"    Обучение:              {target.Stats.Learning.FinalValue,3}  (база: {target.Stats.Learning.FullBase})", StatColor(target.Stats.Learning.FinalValue));
        Log($"    Гибкость:              {target.Stats.Flexibility.FinalValue,3}  (база: {target.Stats.Flexibility.FullBase})", StatColor(target.Stats.Flexibility.FinalValue));
        Log($"    Интуиция:              {target.Stats.Intuition.FinalValue,3}  (база: {target.Stats.Intuition.FullBase})", StatColor(target.Stats.Intuition.FinalValue));
        Log($"    Соц. интеллект:        {target.Stats.SocialIntel.FinalValue,3}  (база: {target.Stats.SocialIntel.FullBase})", StatColor(target.Stats.SocialIntel.FinalValue));
        Log($"    Творчество:            {target.Stats.Creativity.FinalValue,3}  (база: {target.Stats.Creativity.FullBase})", StatColor(target.Stats.Creativity.FinalValue));
        Log($"    Математика:            {target.Stats.Mathematics.FinalValue,3}  (база: {target.Stats.Mathematics.FullBase})", StatColor(target.Stats.Mathematics.FinalValue));

        // Энергетические характеристики
        Log("  ЭНЕРГЕТИЧЕСКИЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        Log($"    Запас энергии:         {target.Stats.EnergyReserve.FinalValue,3}  (база: {target.Stats.EnergyReserve.FullBase})", StatColor(target.Stats.EnergyReserve.FinalValue));
        Log($"    Восстановление (энерг):{target.Stats.EnergyRecovery.FinalValue,3}  (база: {target.Stats.EnergyRecovery.FullBase})", StatColor(target.Stats.EnergyRecovery.FinalValue));
        Log($"    Контроль:              {target.Stats.Control.FinalValue,3}  (база: {target.Stats.Control.FullBase})", StatColor(target.Stats.Control.FinalValue));
        Log($"    Концентрация:          {target.Stats.Concentration.FinalValue,3}  (база: {target.Stats.Concentration.FullBase})", StatColor(target.Stats.Concentration.FinalValue));
        Log($"    Выход:                 {target.Stats.Output.FinalValue,3}  (база: {target.Stats.Output.FullBase})", StatColor(target.Stats.Output.FinalValue));
        Log($"    Тонкость:              {target.Stats.Precision.FinalValue,3}  (база: {target.Stats.Precision.FullBase})", StatColor(target.Stats.Precision.FinalValue));
        Log($"    Устойчивость (энерг):  {target.Stats.EnergyResist.FinalValue,3}  (база: {target.Stats.EnergyResist.FullBase})", StatColor(target.Stats.EnergyResist.FinalValue));
        Log($"    Восприятие энергии:    {target.Stats.EnergySense.FinalValue,3}  (база: {target.Stats.EnergySense.FullBase})", StatColor(target.Stats.EnergySense.FinalValue));

        // Память (последние 5 записей)
        if (target.Memory.Count > 0)
        {
            Log("  ПАМЯТЬ (последние):", LogEntry.ColorDay);
            foreach (var mem in target.Memory.TakeLast(5))
                Log($"    {mem.Icon} День {mem.Day}: {mem.Text}", LogEntry.ColorNormal);
        }

        // Разделитель
        Log("──────────────────────────────────────────────", LogEntry.ColorNormal);

        return $"Переглянуто інформацію про {target.Name}";
    }

    // Вспомогательные методы для определения цветов
    private static string HealthColor(double hp) =>
        hp < 30 ? LogEntry.ColorDanger : hp < 60 ? LogEntry.ColorWarning : LogEntry.ColorSuccess;

    private static string StatColor(int value) =>
        value >= 75 ? LogEntry.ColorSuccess : value >= 50 ? LogEntry.ColorNormal : LogEntry.ColorWarning;
}