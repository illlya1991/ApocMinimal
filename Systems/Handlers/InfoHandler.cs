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
        foreach (var stat in target.Stats.GetPhysicalStats())
        {
            logAction?.Invoke($"    {stat.Name,-20}: {stat.FinalValue,3}  (база: {stat.FullBase})", StatColor(stat.FinalValue));
        }

        // Ментальные
        logAction?.Invoke("  МЕНТАЛЬНЫЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        foreach (var stat in target.Stats.GetMentalStats())
        {
            logAction?.Invoke($"    {stat.Name,-20}: {stat.FinalValue,3}  (база: {stat.FullBase})", StatColor(stat.FinalValue));
        }

        // Энергетические
        logAction?.Invoke("  ЭНЕРГЕТИЧЕСКИЕ ХАРАКТЕРИСТИКИ", LogEntry.ColorDay);
        foreach (var stat in target.Stats.GetEnergyStats())
        {
            logAction?.Invoke($"    {stat.Name,-20}: {stat.FinalValue,3}  (база: {stat.FullBase})", StatColor(stat.FinalValue));
        }
    }

    private static string StatColor(int value)
    {
        if (value >= 75) return LogEntry.ColorSuccess;
        if (value >= 50) return LogEntry.ColorNormal;
        return LogEntry.ColorWarning;
    }
    private static string HealthColor(double hp) =>
        hp < 30 ? LogEntry.ColorDanger : hp < 60 ? LogEntry.ColorWarning : LogEntry.ColorSuccess;

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