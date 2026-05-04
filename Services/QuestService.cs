using ApocMinimal.Database;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Services;

public class QuestService
{
    private readonly DatabaseManager _db;

    public QuestService(DatabaseManager db) => _db = db;

    public string BuyQuest(Player player, List<PlayerLibraryEntry> library,
        QuestCatalogEntry entry, QuestType type)
    {
        double? priceNullable = type switch
        {
            QuestType.OneTime     => entry.PriceOneTime,
            QuestType.Repeatable  => entry.PriceRepeatable,
            QuestType.Eternal     => entry.PriceEternal,
            _                     => null
        };
        if (priceNullable == null) return "Этот тип покупки недоступен для данного квеста";
        double price = priceNullable.Value;

        if (player.DevPoints < price)
            return $"Недостаточно ОР (нужно {price:F0}, есть {player.DevPoints:F0})";

        for (int i = 0; i < library.Count; i++)
        {
            if (library[i].CatalogId != entry.Id || library[i].QuestType != type) continue;
            if (type == QuestType.OneTime) return "Этот квест уже куплен";
            if (type == QuestType.Eternal) return "Вечный квест уже куплен";
        }

        player.DevPoints -= price;
        _db.PurchaseQuest(_db.CurrentSaveId, entry, type);

        string typeLabel = type switch
        {
            QuestType.OneTime    => "×1",
            QuestType.Repeatable => "×10",
            QuestType.Eternal    => "∞",
            _                    => ""
        };
        return $"Куплен квест «{entry.Title}» [{typeLabel}] за {price:F0} ОР";
    }

    public string PublishQuest(List<Quest> quests, List<Resource> resources,
        PlayerLibraryEntry entry, int currentDay)
    {
        if (!entry.CanPublish) return "Нет доступных публикаций";
        var catalog = entry.Catalog;
        if (catalog == null) return "Данные квеста не найдены";

        int rewardResId = 0;
        if (!string.IsNullOrEmpty(catalog.RewardResource))
        {
            var res = resources.FirstOrDefault(r => r.Name == catalog.RewardResource);
            if (res != null) rewardResId = res.Id;
        }

        var quest = new Quest
        {
            Title         = catalog.Title,
            Description   = catalog.Description,
            Source        = QuestSource.Player,
            Status        = QuestStatus.Available,
            DaysRequired  = catalog.CompleteDays,
            DaysRemaining = catalog.CompleteDays,
            RewardResourceId = rewardResId,
            RewardAmount  = catalog.RewardAmount,
            OPCost        = 0,
            QuestType     = entry.QuestType,
            LibraryId     = entry.Id,
            CompleteType  = catalog.CompleteType,
            CompleteTarget = catalog.CompleteAmount,
            DayTaken      = currentDay,
            RewardType    = catalog.RewardType,
            RewardTechnique = catalog.RewardTechnique,
        };

        _db.SaveQuestFull(quest);
        quests.Add(quest);

        if (entry.PublishesLeft != -1) entry.PublishesLeft--;
        if (entry.QuestType == QuestType.OneTime && entry.PublishesLeft == 0)
            _db.DeleteLibraryEntry(entry.Id);
        else
            _db.UpdateLibraryEntry(entry);

        return $"Квест «{quest.Title}» опубликован";
    }

    public string UnpublishQuest(List<Quest> quests, List<PlayerLibraryEntry> library, Quest quest)
    {
        if (quest.Status != QuestStatus.Available) return "Можно снять только опубликованный квест";

        if (quest.LibraryId > 0)
        {
            var entry = library.FirstOrDefault(e => e.Id == quest.LibraryId);
            if (entry?.Catalog != null)
            {
                if (entry.PublishesLeft != -1) entry.PublishesLeft++;
                _db.UpdateLibraryEntry(entry);
            }
        }

        _db.DeleteQuest(quest.Id);
        quests.Remove(quest);
        return $"Квест «{quest.Title}» снят";
    }

    public List<string> CollectCompletedQuests(Player player, List<Quest> quests,
        List<Resource> resources, Dictionary<int, Npc> npcById,
        List<PlayerLibraryEntry> library, int currentDay)
    {
        var logs = new List<string>();
        var completed = quests.Where(q => q.Status == QuestStatus.Completed).ToList();

        foreach (var quest in completed)
        {
            string rewardGiven = "";
            if (quest.RewardResourceId > 0 && quest.RewardAmount > 0)
            {
                var res = resources.FirstOrDefault(r => r.Id == quest.RewardResourceId);
                if (res != null)
                {
                    res.Amount += quest.RewardAmount;
                    rewardGiven = $"+{quest.RewardAmount:F0} {res.Name}";
                    logs.Add($"Получено: {rewardGiven} за «{quest.Title}»");
                }
            }
            else
            {
                logs.Add($"Принято: «{quest.Title}»");
            }

            npcById.TryGetValue(quest.AssignedNpcId, out var npc);
            var historyEntry = new QuestHistoryEntry
            {
                SaveId      = _db.CurrentSaveId,
                CatalogId   = quest.LibraryId > 0
                    ? (library.FirstOrDefault(e => e.Id == quest.LibraryId)?.CatalogId ?? 0) : 0,
                QuestTitle  = quest.Title,
                NpcName     = npc?.Name ?? "",
                DayTaken    = quest.DayTaken > 0 ? quest.DayTaken : currentDay,
                DayCompleted = currentDay,
                RewardGiven = rewardGiven,
            };
            _db.SaveQuestHistory(historyEntry);

            if (quest.LibraryId > 0)
            {
                var entry = library.FirstOrDefault(e => e.Id == quest.LibraryId);
                if (entry?.Catalog != null)
                {
                    entry.TimesCompleted++;
                    if (entry.QuestType == QuestType.Repeatable || entry.QuestType == QuestType.Eternal)
                        if (entry.PublishesLeft != -1) entry.PublishesLeft++;
                    _db.UpdateLibraryEntry(entry);
                }
            }

            _db.DeleteQuest(quest.Id);
            quests.Remove(quest);
        }
        return logs;
    }
}
