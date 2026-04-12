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
/// Обработчик квестов: выдача, завершение, публичные квесты
/// </summary>
public class QuestHandler : BaseActionHandler
{
    public QuestHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
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

        return actionKey switch
        {
            "GiveQuest" => ExecuteGiveQuest(parameters, player, npcs, resources, quests),
            "CompleteQuest" => ExecuteCompleteQuest(parameters, player, npcs, resources, quests),
            "AssignPublicQuest" => ExecuteAssignPublicQuest(parameters, player, npcs, resources, quests),
            _ => $"Неизвестное действие: {actionKey}"
        };
    }

    // ============================================================
    // 1. Выдача квеста конкретному NPC
    // ============================================================
    private string ExecuteGiveQuest(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        // Проверка параметров
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не указан целевой NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Ошибка: неверный тип целевого NPC";
        if (!target.IsAlive) return $"{target.Name} мертв";

        if (!parameters.ContainsKey("questId") || parameters["questId"] == null)
            return "Не указан квест";

        var quest = parameters["questId"] as Quest;
        if (quest == null) return "Ошибка: неверный тип квеста";

        // Проверка: квест должен быть доступен
        if (quest.Status != QuestStatus.Available)
            return $"Квест '{quest.Title}' уже {GetQuestStatusName(quest.Status)}";

        // Проверка: NPC не должен быть занят
        if (target.HasTask)
            return $"{target.Name} уже занят заданием: {target.ActiveTask}";

        // Проверка требований к характеристикам NPC
        if (!CheckStatRequirements(target, quest))
        {
            Log($"  {target.Name} не соответствует требованиям квеста", LogEntry.ColorWarning);
            return $"{target.Name} не может взять этот квест";
        }

        // Шанс отказа для трусов
        if (target.Trait == NpcTrait.Coward && _rnd.NextDouble() < 0.5)
        {
            Log($"  {target.Name} (Трус) отказался от квеста", LogEntry.ColorWarning);
            return $"{target.Name} отказался";
        }

        // Назначаем квест
        QuestSystem.AssignQuest(quest, target);
        _db.SaveNpc(target);
        _db.SaveQuest(quest);

        // Логирование
        Log($"  {target.Name} взял квест:", LogEntry.ColorSuccess);
        Log($"    «{quest.Title}»", LogEntry.ColorSpeech);
        Log($"    Осталось дней: {quest.DaysRemaining}", LogEntry.ColorNormal);

        target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Quest,
            $"Взял квест: «{quest.Title}» ({quest.DaysRequired} дн.)"));
        _db.SaveNpc(target);

        return $"Квест «{quest.Title}» выдан {target.Name}";
    }

    // ============================================================
    // 2. Принудительное завершение квеста (с наградой или без)
    // ============================================================
    private string ExecuteCompleteQuest(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        if (!parameters.ContainsKey("targetNpc") || parameters["targetNpc"] == null)
            return "Не указан целевой NPC";

        var target = parameters["targetNpc"] as Npc;
        if (target == null) return "Ошибка: неверный тип целевого NPC";
        if (!target.IsAlive) return $"{target.Name} мертв";

        if (!target.HasTask)
            return $"{target.Name} не имеет активного задания";

        // Находим квест NPC
        var quest = quests.FirstOrDefault(q => q.Status == QuestStatus.Active && q.AssignedNpcId == target.Id);
        if (quest == null)
        {
            // Если квеста нет в списке, но у NPC есть задание - очищаем
            target.ActiveTask = "";
            target.TaskDaysLeft = 0;
            _db.SaveNpc(target);
            return $"{target.Name} не имеет активного квеста (очищено)";
        }

        // Успех или провал?
        bool success = parameters.ContainsKey("forceSuccess") && (bool)parameters["forceSuccess"];
        if (!success)
        {
            // Шанс успеха зависит от характеристик
            double successChance = CalculateSuccessChance(target, quest);
            success = _rnd.NextDouble() < successChance;
        }

        if (success)
        {
            // Выдаём награду
            var rewardResource = resources.FirstOrDefault(r => r.Id == quest.RewardResourceId);
            if (rewardResource != null && quest.RewardAmount > 0)
            {
                rewardResource.Amount += quest.RewardAmount;
                _db.SaveResource(rewardResource);
                Log($"  Награда: +{quest.RewardAmount:F0} ед. «{rewardResource.Name}»", LogEntry.ColorSuccess);
            }

            // Повышение доверия
            int trustGain = (int)Math.Min(20, 5 + quest.DaysRequired);
            target.Trust = Math.Min(100, target.Trust + trustGain);
            Log($"  Доверие +{trustGain} (теперь {target.Trust:F0})", LogEntry.ColorSuccess);

            // Повышение уровня последователя (шанс)
            if (target.FollowerLevel < 5 && _rnd.NextDouble() < 0.3)
            {
                target.FollowerLevel++;
                Log($"  Уровень последователя повышен до {target.FollowerLabel}!", LogEntry.ColorAltarColor);
            }

            target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Quest,
                $"Выполнил квест: «{quest.Title}»"));
        }
        else
        {
            // Провал - штрафы
            int trustLoss = (int)Math.Min(15, 5 + quest.DaysRequired / 2);
            target.Trust = Math.Max(0, target.Trust - trustLoss);
            Log($"  {target.Name} провалил квест! Доверие -{trustLoss}", LogEntry.ColorDanger);

            target.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Quest,
                $"Провалил квест: «{quest.Title}»"));
        }

        // Очищаем задание NPC
        target.ActiveTask = "";
        target.TaskDaysLeft = 0;
        target.TaskRewardResId = 0;
        target.TaskRewardAmt = 0;

        // Обновляем статус квеста
        quest.Status = success ? QuestStatus.Completed : QuestStatus.Failed;
        quest.AssignedNpcId = 0;

        _db.SaveNpc(target);
        _db.SaveQuest(quest);

        return success
            ? $"Квест «{quest.Title}» выполнен {target.Name}!"
            : $"Квест «{quest.Title}» провален {target.Name}";
    }

    // ============================================================
    // 3. Публичный квест (любой NPC может взять)
    // ============================================================
    private string ExecuteAssignPublicQuest(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        // Проверка параметров
        if (!parameters.ContainsKey("questId") || parameters["questId"] == null)
            return "Не указан квест";

        var quest = parameters["questId"] as Quest;
        if (quest == null) return "Ошибка: неверный тип квеста";

        // Квест должен быть доступен
        if (quest.Status != QuestStatus.Available)
            return $"Квест '{quest.Title}' уже {GetQuestStatusName(quest.Status)}";

        // Находим свободных NPC
        var idleNpcs = npcs.Where(n => n.IsAlive && !n.HasTask).ToList();
        if (idleNpcs.Count == 0)
            return "Нет свободных NPC для публичного квеста";

        // Выбираем лучшего кандидата
        var bestCandidate = idleNpcs
            .OrderByDescending(n => CalculateQuestSuitability(n, quest))
            .FirstOrDefault();

        if (bestCandidate == null)
            return "Нет подходящих NPC для этого квеста";

        // Назначаем квест
        QuestSystem.AssignQuest(quest, bestCandidate);
        _db.SaveNpc(bestCandidate);
        _db.SaveQuest(quest);

        Log($"  Публичный квест «{quest.Title}» взят {bestCandidate.Name}!", LogEntry.ColorSuccess);
        Log($"    Требует {quest.DaysRequired} дн., награда: {quest.RewardAmount:F0} ед.", LogEntry.ColorNormal);

        bestCandidate.Remember(new MemoryEntry(player.CurrentDay, MemoryType.Quest,
            $"Взял публичный квест: «{quest.Title}»"));

        return $"Публичный квест «{quest.Title}» назначен";
    }

    // ============================================================
    // Вспомогательные методы
    // ============================================================

    private double CalculateSuccessChance(Npc npc, Quest quest)
    {
        double chance = 0.6; // Базовая вероятность 60%

        // Бонус от характеристик
        int statBonus = 0;
        foreach (var (statId, minVal) in quest.RequiredStats)
        {
            int statValue = npc.Stats.GetStatValue(statId);
            if (statValue >= minVal)
                statBonus += (int)((statValue - minVal) / 10);
            else
                statBonus -= (int)((minVal - statValue) / 5);
        }
        chance += statBonus * 0.05;

        // Бонус от уровня последователя
        chance += npc.FollowerLevel * 0.05;

        // Бонус от специализаций
        var matchingSpec = npc.Specializations.Any(s =>
            quest.Title.Contains(s) || quest.Description.Contains(s));
        if (matchingSpec) chance += 0.15;

        // Бонус/штраф от черт
        if (npc.CharTraits.Contains(CharacterTrait.Brave)) chance += 0.1;
        if (npc.CharTraits.Contains(CharacterTrait.Cowardly)) chance -= 0.2;
        if (npc.CharTraits.Contains(CharacterTrait.Lazy)) chance -= 0.15;

        // Штраф от страха
        chance -= npc.Fear / 200.0;

        return Math.Clamp(chance, 0.1, 0.95);
    }

    private double CalculateQuestSuitability(Npc npc, Quest quest)
    {
        double suitability = npc.Initiative / 100.0; // Базовая инициатива

        // Бонус от специализаций
        var matchingSpec = npc.Specializations.Any(s =>
            quest.Title.Contains(s) || quest.Description.Contains(s));
        if (matchingSpec) suitability += 0.3;

        // Бонус от уровня последователя
        suitability += npc.FollowerLevel * 0.1;

        return Math.Clamp(suitability, 0, 1);
    }

    private bool CheckStatRequirements(Npc npc, Quest quest)
    {
        foreach (var (statId, minVal) in quest.RequiredStats)
        {
            int statValue = npc.Stats.GetStatValue(statId);
            if (statValue < minVal)
                return false;
        }
        return true;
    }

    private static string GetQuestStatusName(QuestStatus status) => status switch
    {
        QuestStatus.Available => "доступен",
        QuestStatus.Active => "активен",
        QuestStatus.Completed => "завершён",
        QuestStatus.Failed => "провален",
        _ => status.ToString()
    };
}