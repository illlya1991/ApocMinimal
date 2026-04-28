// ViewModels/GameViewModel.cs
using ApocMinimal.Database;
using ApocMinimal.Models.ExchangeData;
using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.StatisticsData;
using ApocMinimal.Models.TechniqueData;
using ApocMinimal.Systems;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ApocMinimal.Services;

namespace ApocMinimal.ViewModels;

public class GameViewModel : INotifyPropertyChanged
{
    private readonly DatabaseManager _db;
    private readonly ActionManager _actionManager;
    private readonly Random _rnd = new();
    private readonly LocationService _locationService;

    private Player _player = null!;
    private List<Npc> _npcs = new();
    private List<Resource> _resources = new();
    private List<Quest> _quests = new();
    private List<Location> _locations = new();
    private Dictionary<string, ResourceCatalogEntry> _catalog = new();
    private Dictionary<string, double> _gameConfig = new();
    private List<QuestCatalogEntry> _questCatalog = new();
    private List<PlayerLibraryEntry> _playerLibrary = new();
    private List<string> _shopUnlocks = new();
    private List<int> _appliedExchangeIds = new();
    private List<(int RowId, string ItemId, string ItemType)> _techInventory = new();
    public List<PresidentialExchangeEntry> PendingExchanges { get; private set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public GameViewModel(DatabaseManager db, Action<string, string> logAction)
    {
        _db = db;
        // Инициализируем сервис локаций
        _locationService = new LocationService(_db);
        _locationService.Initialize();

        LoadData();

        // Обновляем кэш NPC
        _locationService.UpdateNpcCache(_npcs);

        _actionManager = new ActionManager(_db, _rnd, logAction, _catalog, _gameConfig);
        ActionGroups = _actionManager.GetGroups();
    }

    // Observable properties
    private int _currentDay;
    public int CurrentDay
    {
        get => _currentDay;
        set { _currentDay = value; OnPropertyChanged(); OnPropertyChanged(nameof(DayDisplay)); }
    }

    private double _devPoints;
    public double DevPoints
    {
        get => _devPoints;
        set { _devPoints = value; OnPropertyChanged(); OnPropertyChanged(nameof(DevPointsDisplay)); OnPropertyChanged(nameof(CanUpgrade)); }
    }

    private int _terminalLevel;
    public int TerminalLevel
    {
        get => _terminalLevel;
        set { _terminalLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(TerminalDisplay)); OnPropertyChanged(nameof(UpgradeCost)); OnPropertyChanged(nameof(CanUpgrade)); OnPropertyChanged(nameof(UnlockedTechniques)); }
    }

    private int _actionsToday;
    public int ActionsToday
    {
        get => _actionsToday;
        set { _actionsToday = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActionsDisplay)); OnPropertyChanged(nameof(HasActionsLeft)); }
    }

    private double _barrierSize;
    public double BarrierSize
    {
        get => _barrierSize;
        set { _barrierSize = value; OnPropertyChanged(); }
    }

    public string DayDisplay => $"День {CurrentDay}";
    public string DevPointsDisplay => $"ОР: {DevPoints:F0}";
    public string TerminalDisplay => $"Терминал: ур.{TerminalLevel}";
    public string ActionsDisplay => $"Действий: {ActionsToday}/{Player.MaxPlayerActionsPerDay}";
    public bool HasActionsLeft => ActionsToday < Player.MaxPlayerActionsPerDay;
    public long UpgradeCost => (long)(200 * Math.Pow(5, TerminalLevel - 1));
    public bool CanUpgrade => TerminalLevel < 10 && DevPoints >= UpgradeCost;
    public string PlayerName => _player?.Name ?? "Божество";
    public int AliveNpcsCount => _npcs.Count(n => n.IsAlive);
    public int MaxActiveFollowers => _player?.MaxActiveFollowers ?? 0;

    public List<PlayerActionGroup> ActionGroups { get; private set; } = new();
    public PlayerGameAction? SelectedAction { get; set; }

    public List<PlayerGameAction> GetActionsByGroup(int groupId) =>
        _actionManager.GetActionsByGroup(groupId);

    public string ExecuteAction(PlayerGameAction action, Dictionary<string, object> parameters) =>
        _actionManager.ExecuteAction(action, parameters, _player, _npcs, _resources, _quests);

    // Properties for UI binding
    public List<Npc> AllNpcs => _npcs;
    public List<Npc> AliveNpcs => _npcs.Where(n => n.IsAlive).ToList();
    public List<Resource> Resources => _resources;
    public List<Quest> AvailableQuests => _quests.Where(q => q.Status == QuestStatus.Available).ToList();
    public List<Quest> ActiveQuests => _quests.Where(q => q.Status == QuestStatus.Active).ToList();
    // Теперь вместо _locations используем _locationService
    public List<Location> Locations => _locationService.GetAllLocations();
    public IEnumerable<Technique> UnlockedTechniques => _player?.UnlockedTechniques ?? Enumerable.Empty<Technique>();
    public Technique[] AllTechniques => Player.AllTechniques;

    public List<QuestCatalogEntry> QuestShop => _questCatalog.Where(q => q.MinTerminalLevel <= TerminalLevel).ToList();
    public List<PlayerLibraryEntry> PurchasedQuests => _playerLibrary;
    public List<Quest> PublishedQuests => _quests.Where(q => q.Status == QuestStatus.Available).ToList();
    public List<Quest> CompletedQuests => _quests.Where(q => q.Status == QuestStatus.Completed).ToList();
    public List<QuestHistoryEntry> QuestHistory => _db.GetQuestHistory(_db.CurrentSaveId);

    public List<Location> GetChildren(int parentId)
    {
        return _locationService.GetChildren(parentId);
    }
    public int GetNpcCountAtLocation(int locationId)
    {
        return _locationService.GetNpcCountAtLocation(locationId);
    }
    private void LoadData()
    {
        _player = _db.GetPlayer()!;
        _npcs = _db.GetAllNpcs();
        // Миграция: обновить потребности и статы у НПС с устаревшей структурой
        bool needsMigration = _npcs.Count > 0 && _npcs.Any(n =>
            n.Needs.Count(nd => nd.Category == NeedCategory.Special) > 5 ||
            !n.Needs.Any(nd => nd.Name == "Самосовершенствование"));
        if (needsMigration)
        {
            foreach (var npc in _npcs)
            {
                NpcGenerator.RefreshStatsAndNeeds(npc, _rnd);
                _db.SaveNpc(npc);
            }
        }
        _resources = _db.GetAllResources();
        _quests = _db.GetAllQuests();
        _locations = _db.GetAllLocations();

        var catalogList = _db.GetResourceCatalog();
        _catalog = new Dictionary<string, ResourceCatalogEntry>(catalogList.Count);
        for (int i = 0; i < catalogList.Count; i++)
            _catalog[catalogList[i].Name] = catalogList[i];

        _gameConfig = _db.GetGameConfig();
        _questCatalog = _db.GetQuestCatalog(999);
        _playerLibrary = _db.GetPlayerLibrary(_db.CurrentSaveId);
        _shopUnlocks = _db.GetShopUnlocks(_db.CurrentSaveId);
        _appliedExchangeIds = _db.GetAppliedExchanges(_db.CurrentSaveId);
        _techInventory = _db.GetTechInventory(_db.CurrentSaveId);

        CurrentDay = _player.CurrentDay;
        DevPoints = _player.DevPoints;
        TerminalLevel = _player.TerminalLevel;
        ActionsToday = _player.PlayerActionsToday;
        BarrierSize = _player.BarrierSize;
    }

    public void ReloadQuestLibrary()
    {
        _questCatalog = _db.GetQuestCatalog(999);
        _playerLibrary = _db.GetPlayerLibrary(_db.CurrentSaveId);
    }

    public string BuyQuest(QuestCatalogEntry entry, QuestType type)
    {
        double price = type switch
        {
            QuestType.OneTime => entry.PriceOneTime ?? 0,
            QuestType.Repeatable => entry.PriceRepeatable ?? 0,
            QuestType.Eternal => entry.PriceEternal ?? 0,
            _ => 0
        };

        double? priceNullable = type switch
        {
            QuestType.OneTime => entry.PriceOneTime,
            QuestType.Repeatable => entry.PriceRepeatable,
            QuestType.Eternal => entry.PriceEternal,
            _ => null
        };

        if (priceNullable == null)
            return "Этот тип покупки недоступен для данного квеста";

        if (_player.DevPoints < price)
            return $"Недостаточно ОР (нужно {price:F0}, есть {_player.DevPoints:F0})";

        if (type == QuestType.OneTime)
        {
            for (int i = 0; i < _playerLibrary.Count; i++)
            {
                if (_playerLibrary[i].CatalogId == entry.Id && _playerLibrary[i].QuestType == QuestType.OneTime)
                    return "Этот квест уже куплен";
            }
        }

        if (type == QuestType.Eternal)
        {
            for (int i = 0; i < _playerLibrary.Count; i++)
            {
                if (_playerLibrary[i].CatalogId == entry.Id && _playerLibrary[i].QuestType == QuestType.Eternal)
                    return "Вечный квест уже куплен";
            }
        }

        _player.DevPoints -= price;
        _db.SavePlayer(_player);
        _db.PurchaseQuest(_db.CurrentSaveId, entry, type);
        ReloadQuestLibrary();
        DevPoints = _player.DevPoints;
        string typeLabel = type switch
        {
            QuestType.OneTime => "×1",
            QuestType.Repeatable => "×10",
            QuestType.Eternal => "∞",
            _ => ""
        };
        return $"Куплен квест «{entry.Title}» [{typeLabel}] за {price:F0} ОР";
    }

    public string PublishQuest(PlayerLibraryEntry entry)
    {
        if (!entry.CanPublish)
            return "Нет доступных публикаций";

        var catalog = entry.Catalog;
        if (catalog == null)
            return "Данные квеста не найдены";

        int rewardResId = 0;
        if (!string.IsNullOrEmpty(catalog.RewardResource))
        {
            var res = _resources.FirstOrDefault(r => r.Name == catalog.RewardResource);
            if (res != null) rewardResId = res.Id;
        }

        var quest = new Quest
        {
            Title = catalog.Title,
            Description = catalog.Description,
            Source = QuestSource.Player,
            Status = QuestStatus.Available,
            DaysRequired = catalog.CompleteDays,
            DaysRemaining = catalog.CompleteDays,
            RewardResourceId = rewardResId,
            RewardAmount = catalog.RewardAmount,
            OPCost = 0,
            QuestType = entry.QuestType,
            LibraryId = entry.Id,
            CompleteType = catalog.CompleteType,
            CompleteTarget = catalog.CompleteAmount,
            DayTaken = CurrentDay,
            RewardType = catalog.RewardType,
            RewardTechnique = catalog.RewardTechnique,
        };

        _db.SaveQuestFull(quest);
        _quests.Add(quest);

        if (entry.PublishesLeft != -1)
            entry.PublishesLeft--;

        if (entry.QuestType == QuestType.OneTime && entry.PublishesLeft == 0)
        {
            _db.DeleteLibraryEntry(entry.Id);
        }
        else
        {
            _db.UpdateLibraryEntry(entry);
        }
        ReloadQuestLibrary();

        return $"Квест «{quest.Title}» опубликован";
    }

    public string UnpublishQuest(Quest quest)
    {
        if (quest.Status != QuestStatus.Available)
            return "Можно снять только опубликованный квест";

        if (quest.LibraryId > 0)
        {
            var entry = _playerLibrary.FirstOrDefault(e => e.Id == quest.LibraryId);
            if (entry != null && entry.Catalog != null)
            {
                if (entry.PublishesLeft != -1)
                    entry.PublishesLeft++;
                _db.UpdateLibraryEntry(entry);
            }
        }

        _db.DeleteQuest(quest.Id);
        _quests.Remove(quest);
        ReloadQuestLibrary();
        return $"Квест «{quest.Title}» снят";
    }

    public List<string> CollectCompletedQuests()
    {
        var logs = new List<string>();
        var completed = _quests.Where(q => q.Status == QuestStatus.Completed).ToList();

        for (int i = 0; i < completed.Count; i++)
        {
            var quest = completed[i];
            string rewardGiven = "";

            if (quest.RewardResourceId > 0 && quest.RewardAmount > 0)
            {
                var res = _resources.FirstOrDefault(r => r.Id == quest.RewardResourceId);
                if (res != null)
                {
                    res.Amount += quest.RewardAmount;
                    _db.SaveResource(res);
                    rewardGiven = $"+{quest.RewardAmount:F0} {res.Name}";
                    logs.Add($"Получено: {rewardGiven} за «{quest.Title}»");
                }
            }
            else
            {
                logs.Add($"Принято: «{quest.Title}»");
            }

            var npc = _npcs.FirstOrDefault(n => n.Id == quest.AssignedNpcId);
            var historyEntry = new QuestHistoryEntry
            {
                SaveId = _db.CurrentSaveId,
                CatalogId = quest.LibraryId > 0
                    ? (_playerLibrary.FirstOrDefault(e => e.Id == quest.LibraryId)?.CatalogId ?? 0)
                    : 0,
                QuestTitle = quest.Title,
                NpcName = npc?.Name ?? "",
                DayTaken = quest.DayTaken > 0 ? quest.DayTaken : CurrentDay,
                DayCompleted = CurrentDay,
                RewardGiven = rewardGiven,
            };
            _db.SaveQuestHistory(historyEntry);

            if (quest.LibraryId > 0)
            {
                var entry = _playerLibrary.FirstOrDefault(e => e.Id == quest.LibraryId);
                if (entry != null && entry.Catalog != null)
                {
                    entry.TimesCompleted++;
                    var qt = entry.QuestType;
                    if (qt == QuestType.Repeatable || qt == QuestType.Eternal)
                    {
                        if (entry.PublishesLeft != -1)
                            entry.PublishesLeft++;
                    }
                    _db.UpdateLibraryEntry(entry);
                }
            }

            _db.DeleteQuest(quest.Id);
            _quests.Remove(quest);
        }

        ReloadQuestLibrary();
        return logs;
    }

    public void Refresh()
    {
        LoadData();
        OnPropertyChanged(nameof(ActionGroups));
        OnPropertyChanged(nameof(AllNpcs));
        OnPropertyChanged(nameof(AliveNpcs));
        OnPropertyChanged(nameof(Resources));
        OnPropertyChanged(nameof(AvailableQuests));
        OnPropertyChanged(nameof(ActiveQuests));
        OnPropertyChanged(nameof(Locations));
        OnPropertyChanged(nameof(UnlockedTechniques));
        OnPropertyChanged(nameof(QuestShop));
        OnPropertyChanged(nameof(PurchasedQuests));
        OnPropertyChanged(nameof(PublishedQuests));
        OnPropertyChanged(nameof(CompletedQuests));
    }

    public void SaveAll()
    {
        _db.SavePlayer(_player);

        foreach (var n in _npcs)
            _db.SaveNpc(n);

        foreach (var r in _resources)
            _db.SaveResource(r);

        foreach (var q in _quests.Where(q => q.Status != QuestStatus.Available))
            _db.SaveQuest(q);

        // Сохраняем только измененные локации
        var dirtyLocations = _locations.Where(l => l.IsDirty).ToList();
        if (dirtyLocations.Any())
        {
            _db.SaveLocationsBatch(dirtyLocations);

            // Сбрасываем флаг для сохраненных локаций
            foreach (var loc in dirtyLocations)
                loc.ClearDirty();

            System.Diagnostics.Debug.WriteLine($"Saved {dirtyLocations.Count} dirty locations out of {_locations.Count}");
        }
    }

    public void SaveLocation(Location loc)
    {
        _db.SaveLocation(loc);
        loc.ClearDirty();
    }

    public void SavePlayer()
    {
        _db.SavePlayer(_player);
    }

    public void SaveNpc(Npc npc)
    {
        _db.SaveNpc(npc);
    }

    /// <summary>Process NPC actions for CurrentDay. Does NOT advance the day.</summary>
    public DayResult ProcessNpcDay()
    {
        var ctx = new ActionContext { Resources = _resources, Locations = _locations, Npcs = _npcs };
        return GameLoopService.ProcessNpcActionsOnly(_player, _npcs, _rnd, ctx);
    }

    /// <summary>Process end-of-day (quests, needs, faith) and advance CurrentDay. Returns results for logging.</summary>
    public DayResult AdvanceToNextDay()
    {
        var dayResult = GameLoopService.ProcessDayEnd(_player, _npcs, _resources, _quests, _rnd, _catalog);

        foreach (var (npc, q) in dayResult.QuestRewards)
        {
            var res = _resources.FirstOrDefault(r => r.Id == q.RewardResourceId);
            if (res != null) res.Amount += q.RewardAmount;
        }

        _quests.AddRange(dayResult.NewQuests);

        _player.CurrentDay++;
        _player.PlayerActionsToday = 0;
        CurrentDay = _player.CurrentDay;
        DevPoints = _player.DevPoints;
        ActionsToday = _player.PlayerActionsToday;

        return dayResult;
    }

    public int GetFollowerLimit(int followerLevel)
    {
        return _player?.GetFollowerLimit(followerLevel) ?? 0;
    }

    public int GetFollowerCountAtLevel(int followerLevel)
    {
        return _db.GetFollowerCountAtLevel(followerLevel);
    }

    public Npc? GetNpcById(int id)
    {
        return _npcs.FirstOrDefault(n => n.Id == id);
    }

    public List<ResourceCatalogEntry> GetShoppableResources()
    {
        return _catalog.Values.ToList();
    }

    public bool IsShopUnlocked(string resourceName) => _shopUnlocks.Contains(resourceName);

    /// <summary>
    /// Unlock a resource for purchase: costs 5 OV + 1 unit of the resource from community pool.
    /// </summary>
    public string UnlockShopResource(string resourceName)
    {
        if (_shopUnlocks.Contains(resourceName))
            return $"{resourceName} уже разблокирован";

        var res = _resources.FirstOrDefault(r => r.Name == resourceName);
        if (res == null || res.Amount < 1)
            return $"Недостаточно {resourceName} (нужна 1 ед.)";
        if (_player.DevPoints < 5)
            return "Недостаточно ОР (нужно 5)";

        res.Amount -= 1;
        _player.DevPoints -= 5;
        _db.SaveResource(res);
        _db.SavePlayer(_player);
        _db.UnlockShopResource(_db.CurrentSaveId, resourceName);
        _shopUnlocks.Add(resourceName);
        DevPoints = _player.DevPoints;
        return $"Разблокирована покупка: {resourceName}";
    }

    /// <summary>
    /// Buy 10 units of a resource. Returns result message.
    /// </summary>
    public string BuyShopResource(string resourceName)
    {
        if (!_shopUnlocks.Contains(resourceName))
            return $"{resourceName} не разблокирован";

        if (!_catalog.TryGetValue(resourceName, out var entry))
            return "Ресурс не найден в каталоге";

        double price = entry.Quality switch
        {
            1 => 2, 2 => 3, 3 => 5, 4 => 10, 5 => 20, _ => 5
        };

        if (_player.DevPoints < price)
            return $"Недостаточно ОР (нужно {price:F0})";

        _player.DevPoints -= price;
        _db.SavePlayer(_player);

        var res = _resources.FirstOrDefault(r => r.Name == resourceName);
        if (res != null)
        {
            res.Amount += 10;
            _db.SaveResource(res);
        }

        DevPoints = _player.DevPoints;
        return $"Куплено 10 ед. {resourceName} за {price:F0} ОР";
    }

    public List<PresidentialExchangeEntry> AppliedExchangesList =>
        ExchangeCatalog.All.Where(e => _appliedExchangeIds.Contains(e.Id)).ToList();

    public int BarrierLevel
    {
        get => _player?.BarrierLevel ?? 1;
        set { if (_player != null) _player.BarrierLevel = value; OnPropertyChanged(); }
    }

    public List<int> ControlledZoneIds => _player?.ControlledZoneIds ?? new();

    public int BaseUnits => _player?.BaseUnits ?? 0;

    public string ProtectLocation(int locationId)
    {
        if (_player == null) return "Ошибка";
        var loc = _locations.FirstOrDefault(l => l.Id == locationId);
        if (loc == null) return "Локация не найдена";
        if (_player.ControlledZoneIds.Contains(locationId))
            return $"«{loc.Name}» уже под защитой";

        double cost = loc.Type switch
        {
            LocationType.Apartment => 5,
            LocationType.Floor => 10,
            LocationType.Building => 20,
            LocationType.Street => 50,
            _ => 100
        };
        if (_player.DevPoints < cost)
            return $"Недостаточно ОР (нужно {cost:F0})";

        _player.DevPoints -= cost;
        _player.ControlledZoneIds.Add(locationId);
        _player.TerritoryControl = _player.ControlledZoneIds.Count;
        _db.SavePlayer(_player);
        DevPoints = _player.DevPoints;
        return $"«{loc.Name}» взята под защиту ({cost:F0} ОР)";
    }

    public string UnprotectLocation(int locationId)
    {
        if (_player == null) return "Ошибка";
        var loc = _locations.FirstOrDefault(l => l.Id == locationId);
        if (!_player.ControlledZoneIds.Contains(locationId))
            return "Локация не защищена";

        _player.ControlledZoneIds.Remove(locationId);
        _player.TerritoryControl = _player.ControlledZoneIds.Count;
        _db.SavePlayer(_player);
        return $"«{loc?.Name ?? $"#{locationId}"}» снята с защиты";
    }

    public List<PresidentialExchangeEntry> SetupAndApplyDayExchanges(int day)
    {
        if (!ExchangeCatalog.IsCriticalDay(day))
            return new List<PresidentialExchangeEntry>();

        var exchanges = ExchangeCatalog.GetForDay(day, _appliedExchangeIds, _rnd);
        foreach (var ex in exchanges)
        {
            if (_appliedExchangeIds.Contains(ex.Id)) continue;
            ExchangeSystem.Apply(ex, _npcs, _resources);
            _appliedExchangeIds.Add(ex.Id);
            _db.SaveAppliedExchange(_db.CurrentSaveId, ex.Id);
            foreach (var npc in _npcs)
                if (npc.IsAlive) _db.SaveNpc(npc);
        }
        PendingExchanges = new List<PresidentialExchangeEntry>();
        OnPropertyChanged(nameof(PendingExchanges));
        OnPropertyChanged(nameof(AppliedExchangesList));
        return exchanges;
    }

    public void SetupDayExchanges(int day) => SetupAndApplyDayExchanges(day);

    public string ApplyExchange(PresidentialExchangeEntry ex)
    {
        if (_appliedExchangeIds.Contains(ex.Id))
            return $"Обмен «{ex.Name}» уже был применён.";

        ExchangeSystem.Apply(ex, _npcs, _resources);
        _appliedExchangeIds.Add(ex.Id);
        _db.SaveAppliedExchange(_db.CurrentSaveId, ex.Id);

        foreach (var npc in _npcs)
            if (npc.IsAlive) _db.SaveNpc(npc);

        PendingExchanges.Remove(ex);
        OnPropertyChanged(nameof(PendingExchanges));
        return $"✓ Принят: «{ex.Name}»";
    }

    public int NextCriticalDay()
    {
        foreach (int d in ExchangeCatalog.CriticalDays)
            if (d > CurrentDay) return d;
        return -1;
    }

    // ── Tech / Ability inventory ─────────────────────────────────────────────

    public List<(int RowId, string ItemId, string ItemType)> TechInventory => _techInventory;

    /// <summary>Buy one copy of a technique or ability from the altar shop.</summary>
    public string BuyTechItem(string itemId, string itemType)
    {
        double cost;
        string name;
        int reqAltar;

        if (itemType == "Technique")
        {
            var def = TechAbilityCatalog.FindTech(itemId);
            if (def == null) return "Техника не найдена";
            cost = def.BuyCost;
            name = def.Name;
            reqAltar = def.TerminalLevel;
        }
        else
        {
            var def = TechAbilityCatalog.FindAbility(itemId);
            if (def == null) return "Способность не найдена";
            cost = def.BuyCost;
            name = def.Name;
            reqAltar = def.TerminalLevel;
        }

        if (TerminalLevel < reqAltar)
            return $"Требуется уровень Терминала {reqAltar} (сейчас {TerminalLevel})";
        if (_player.DevPoints < cost)
            return $"Недостаточно ОР (нужно {cost:F0}, есть {_player.DevPoints:F0})";

        _player.DevPoints -= cost;
        _db.SavePlayer(_player);
        _db.AddTechInventoryItem(_db.CurrentSaveId, itemId, itemType);
        _techInventory = _db.GetTechInventory(_db.CurrentSaveId);
        DevPoints = _player.DevPoints;
        return $"Куплено: «{name}» за {cost:F0} ОР";
    }

    /// <summary>
    /// Teach a technique or ability from inventory to an NPC.
    /// Removes the item from player inventory. Applies passive stat bonuses.
    /// </summary>
    public string TeachTechItem(Npc npc, int rowId, string itemId, string itemType)
    {
        // Confirm the row still exists
        var entry = _techInventory.FirstOrDefault(e => e.RowId == rowId);
        if (entry == default) return "Предмет не найден в инвентаре";

        if (itemType == "Technique")
        {
            var def = TechAbilityCatalog.FindTech(itemId);
            if (def == null) return "Техника не найдена";

            // Cannot re-learn standalone tech if already learned standalone
            if (npc.LearnedTechIds.Contains(itemId))
                return $"«{npc.Name}» уже знает технику «{def.Name}»";

            // Cannot learn standalone tech if it's already part of a learned ability
            foreach (var abilId in npc.LearnedAbilityIds)
            {
                var abil = TechAbilityCatalog.FindAbility(abilId);
                if (abil != null && abil.TechniqueIds.Contains(itemId))
                    return $"Техника «{def.Name}» уже входит в способность «{abil.Name}»";
            }

            npc.LearnedTechIds.Add(itemId);
            ApplyTechStatBonus(npc, def);
        }
        else // Ability
        {
            var def = TechAbilityCatalog.FindAbility(itemId);
            if (def == null) return "Способность не найдена";

            if (npc.LearnedAbilityIds.Contains(itemId))
                return $"«{npc.Name}» уже знает способность «{def.Name}»";

            // Merge standalone techs that are included in the ability
            foreach (var techId in def.TechniqueIds)
            {
                if (npc.LearnedTechIds.Remove(techId))
                {
                    // Stat bonus was already applied when tech was learned standalone — skip re-apply
                }
                else
                {
                    // Tech not yet learned: apply its passive bonus now
                    var techDef = TechAbilityCatalog.FindTech(techId);
                    if (techDef != null) ApplyTechStatBonus(npc, techDef);
                }
            }

            npc.LearnedAbilityIds.Add(itemId);
        }

        _db.RemoveTechInventoryItem(rowId);
        _techInventory = _db.GetTechInventory(_db.CurrentSaveId);
        _db.SaveNpc(npc);
        return $"«{npc.Name}» обучен успешно";
    }

    private static void ApplyTechStatBonus(Npc npc, TechniqueDefinition def)
    {
        if (def.Kind != TechKind.Passive) return;
        foreach (var (statId, bonus) in def.StatBonus)
        {
            var stat = npc.Stats.GetByNumber(statId);
            if (stat == null) continue;
            stat.AddModifier(new PermanentModifier(
                $"tech_{def.Id}",
                def.Name,
                "Техника",
                ModifierType.Additive,
                bonus));
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}