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
    private readonly TechniqueService _techniqueService;
    private readonly QuestService _questService;
    private readonly ShopService _shopService;

    private Player _player = null!;
    private List<Npc> _npcs = new();
    private Dictionary<int, Npc> _npcById = new();
    private List<Resource> _resources = new();
    private List<Quest> _quests = new();
    private List<Location> _locations = new();
    private Dictionary<string, ResourceCatalogEntry> _catalog = new();
    private Dictionary<string, double> _gameConfig = new();
    private List<QuestCatalogEntry> _questCatalog = new();
    private List<PlayerLibraryEntry> _playerLibrary = new();
    private List<string> _shopUnlocks = new();
    private List<int> _appliedExchangeIds = new();
    private List<string> _techInventory = new();
    private List<Technique> _inventoryTechniques = new();
    private List<MonsterFaction> _monsterFactions = new();
    private TrueTerminal _trueTerminal = new();
    public List<PresidentialExchangeEntry> PendingExchanges { get; private set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly OptimizedGameLoopService _optimizedLoop = new();
    public NpcCacheService NpcCache { get; private set; }
    public LocationCacheService LocationCache { get; private set; }

    public GameViewModel(DatabaseManager db, Action<string, string> logAction, GameInitState? state = null)
    {
        _db = db;

        _locationService = state?.LocationService ?? new LocationService(_db);
        if (state?.LocationService == null) _locationService.Initialize();

        _techniqueService = state?.TechniqueService ?? new TechniqueService(_db);
        if (state?.TechniqueService == null) _techniqueService.Initialize();

        _questService = new QuestService(db);
        _shopService = new ShopService(db);

        LoadData();
        _locationService.UpdateNpcCache(_npcs);

        _actionManager = new ActionManager(_db, _techniqueService, _rnd, logAction, _catalog, _gameConfig);
        ActionGroups = _actionManager.GetGroups();

        // После загрузки данных:
        NpcCache = new NpcCacheService();
        NpcCache.LoadAll(_npcs);
        LocationCache = new LocationCacheService();
        LocationCache.Initialize(_locations);
    }

    // Observable properties
    private int _currentDay;
    public int CurrentDay
    {
        get => _currentDay;
        set { _currentDay = value; OnPropertyChanged(); OnPropertyChanged(nameof(DayDisplay)); }
    }

    private double _devPoints;
    private int _baseUnits;
    public double DevPoints
    {
        get => _devPoints;
        set { _devPoints = value; OnPropertyChanged(); OnPropertyChanged(nameof(DevPointsDisplay)); OnPropertyChanged(nameof(CanUpgrade)); }
    }
    public int BaseUnits
    {
        get => _baseUnits;
        set { _baseUnits = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanUpgrade)); }
    }

    private int _terminalLevel;
    public int TerminalLevel
    {
        get => _terminalLevel;
        set { _terminalLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(TerminalDisplay)); OnPropertyChanged(nameof(UpgradeCost)); OnPropertyChanged(nameof(CanUpgrade)); }
    }

    private int _actionsToday;
    public int ActionsToday
    {
        get => _actionsToday;
        set
        {
            _actionsToday = value;
            if (_player != null) _player.PlayerActionsToday = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActionsDisplay));
            OnPropertyChanged(nameof(HasActionsLeft));
        }
    }
    public string DayDisplay => $"День {CurrentDay}";
    public string DevPointsDisplay => $"ОР: {DevPoints:F0}";
    public string TerminalDisplay => $"Терминал: ур.{TerminalLevel}";
    public string ActionsDisplay => $"Действий: {ActionsToday}/{Player.MaxPlayerActionsPerDay}";
    public bool HasActionsLeft => ActionsToday < Player.MaxPlayerActionsPerDay;
    public long UpgradeCost => _player?.UpgradeCost ?? (long)(200 * Math.Pow(5, TerminalLevel - 1));
    public bool CanUpgrade => TerminalLevel < 10 && DevPoints >= UpgradeCost;
    public string PlayerName => _player?.Name ?? "Игрок";
    public PlayerFaction PlayerFaction => _player?.Faction ?? PlayerFaction.ElementMages;
    public int AliveNpcsCount => _npcs.Count(n => n.IsAlive);
    public int MaxActiveFollowers => _player?.MaxActiveFollowers ?? 0;

    public List<MonsterFaction> MonsterFactions => _monsterFactions;
    public TrueTerminal TrueTerminal => _trueTerminal;
    public bool IsVictory => _trueTerminal.IsAchieved;
    public bool IsDefeat => !_trueTerminal.IsAchieved && _player != null && _player.CurrentDay >= 365;

    public List<PlayerActionGroup> ActionGroups { get; private set; } = new();
    public PlayerGameAction? SelectedAction { get; set; }

    public List<PlayerGameAction> GetActionsByGroup(int groupId) =>
        _actionManager.GetActionsByGroup(groupId);

    public string ExecuteAction(PlayerGameAction action, Dictionary<string, object> parameters) =>
        _actionManager.ExecuteAction(action, parameters, _player, _npcs, _resources, _quests);

    // Properties for UI binding
    public Player GetPlayer() => _player;
    public List<Npc> AllNpcs => _npcs;
    public List<Npc> AliveNpcs => _npcs.Where(n => n.IsAlive).ToList();
    public List<Resource> Resources => _resources;
    public List<Quest> AvailableQuests => _quests.Where(q => q.Status == QuestStatus.Available).ToList();
    public List<Quest> ActiveQuests => _quests.Where(q => q.Status == QuestStatus.Active).ToList();
    // Теперь вместо _locations используем _locationService
    public List<Location> Locations => _locationService.GetAllLocations();
    public IEnumerable<Technique> UnlockedTechniques => _inventoryTechniques;
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
        System.Diagnostics.Debug.WriteLine("=== LoadData: НАЧАЛО ===");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var memSw = System.Diagnostics.Stopwatch.StartNew();

        System.Diagnostics.Debug.WriteLine($"  Память до загрузки: {GC.GetTotalMemory(true) / (1024 * 1024)} МБ");

        // Player
        System.Diagnostics.Debug.WriteLine("  Загрузка Player...");
        _player = _db.GetPlayer()!;
        System.Diagnostics.Debug.WriteLine($"    Player загружен за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // NPC
        System.Diagnostics.Debug.WriteLine("  Загрузка NPC...");
        _npcs = _db.GetAllNpcsOptimized();
        _npcById = _npcs.ToDictionary(n => n.Id);
        System.Diagnostics.Debug.WriteLine($"    NPC загружено: {_npcs.Count} шт. за {memSw.ElapsedMilliseconds} мс, память: {GC.GetTotalMemory(false) / (1024 * 1024)} МБ");
        memSw.Restart();

        // Resources
        System.Diagnostics.Debug.WriteLine("  Загрузка ресурсов...");
        _resources = _db.GetAllResources();
        System.Diagnostics.Debug.WriteLine($"    Ресурсов: {_resources.Count} шт. за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Quests
        System.Diagnostics.Debug.WriteLine("  Загрузка квестов...");
        _quests = _db.GetAllQuests();
        System.Diagnostics.Debug.WriteLine($"    Квестов: {_quests.Count} шт. за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Locations
        System.Diagnostics.Debug.WriteLine("  Загрузка локаций...");
        _locations = _db.GetAllLocations();
        System.Diagnostics.Debug.WriteLine($"    Локаций: {_locations.Count} шт. за {memSw.ElapsedMilliseconds} мс, память: {GC.GetTotalMemory(false) / (1024 * 1024)} МБ");
        memSw.Restart();

        // Resource catalog
        System.Diagnostics.Debug.WriteLine("  Загрузка каталога ресурсов...");
        var catalogList = _db.GetResourceCatalog();
        _catalog = new Dictionary<string, ResourceCatalogEntry>(catalogList.Count);
        for (int i = 0; i < catalogList.Count; i++)
            _catalog[catalogList[i].Name] = catalogList[i];
        System.Diagnostics.Debug.WriteLine($"    Каталог ресурсов: {catalogList.Count} шт. за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Game config
        System.Diagnostics.Debug.WriteLine("  Загрузка конфигурации...");
        _gameConfig = _db.GetGameConfig();
        System.Diagnostics.Debug.WriteLine($"    Конфигурация: {_gameConfig.Count} ключей за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Quest catalog
        System.Diagnostics.Debug.WriteLine("  Загрузка каталога квестов...");
        _questCatalog = _db.GetQuestCatalog(999);
        System.Diagnostics.Debug.WriteLine($"    Каталог квестов: {_questCatalog.Count} шт. за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Player library
        System.Diagnostics.Debug.WriteLine("  Загрузка библиотеки квестов...");
        _playerLibrary = _db.GetPlayerLibrary(_db.CurrentSaveId);
        System.Diagnostics.Debug.WriteLine($"    Библиотека: {_playerLibrary.Count} шт. за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Shop unlocks
        System.Diagnostics.Debug.WriteLine("  Загрузка магазина...");
        _shopUnlocks = _db.GetShopUnlocks(_db.CurrentSaveId);
        System.Diagnostics.Debug.WriteLine($"    Магазин: {_shopUnlocks.Count} шт. за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Applied exchanges
        System.Diagnostics.Debug.WriteLine("  Загрузка обменов...");
        _appliedExchangeIds = _db.GetAppliedExchanges(_db.CurrentSaveId);
        System.Diagnostics.Debug.WriteLine($"    Обменов: {_appliedExchangeIds.Count} шт. за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Tech inventory
        System.Diagnostics.Debug.WriteLine("  Загрузка техник...");
        _techInventory = _db.GetTechInventory(_db.CurrentSaveId);
        RefreshInventoryTechniques();
        System.Diagnostics.Debug.WriteLine($"    Техник в инвентаре: {_techInventory.Count} шт. за {memSw.ElapsedMilliseconds} мс");
        memSw.Restart();

        // Миграция потребностей NPC
        System.Diagnostics.Debug.WriteLine("  Проверка миграции NPC...");
        bool needsMigration = _npcs.Count > 0 && _npcs.Any(n =>
            n.Needs.Count(nd => nd.Category == NeedCategory.Special) > 5 ||
            !n.Needs.Any(nd => nd.Name == "Самосовершенствование"));

        if (needsMigration)
        {
            System.Diagnostics.Debug.WriteLine("    Выполняется миграция NPC...");
            int migrated = 0;
            foreach (var npc in _npcs)
            {
                NpcGenerator.RefreshStatsAndNeeds(npc, _rnd);
                _db.SaveNpc(npc);
                migrated++;
                if (migrated % 100 == 0)
                    System.Diagnostics.Debug.WriteLine($"      Мигрировано {migrated}/{_npcs.Count} NPC");
            }
            System.Diagnostics.Debug.WriteLine($"    Мигрировано {migrated} NPC");
        }
        memSw.Restart();

        // Set UI properties
        CurrentDay = _player.CurrentDay;
        DevPoints = _player.DevPoints;
        TerminalLevel = _player.TerminalLevel;
        ActionsToday = _player.PlayerActionsToday;

        // Monster factions
        if (_monsterFactions.Count == 0)
            _monsterFactions = MonsterFactionFactory.CreateDefault();

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"=== LoadData: ВСЕГО {sw.ElapsedMilliseconds} мс ===");
        System.Diagnostics.Debug.WriteLine($"=== ИТОГОВАЯ ПАМЯТЬ: {GC.GetTotalMemory(true) / (1024 * 1024)} МБ ===");
    }
    public void ReloadQuestLibrary()
    {
        _questCatalog = _db.GetQuestCatalog(999);
        _playerLibrary = _db.GetPlayerLibrary(_db.CurrentSaveId);
    }

    public string BuyQuest(QuestCatalogEntry entry, QuestType type)
    {
        var result = _questService.BuyQuest(_player, _playerLibrary, entry, type);
        DevPoints = _player.DevPoints;
        ReloadQuestLibrary();
        return result;
    }

    public string PublishQuest(PlayerLibraryEntry entry)
    {
        var result = _questService.PublishQuest(_quests, _resources, entry, CurrentDay);
        ReloadQuestLibrary();
        return result;
    }

    public string UnpublishQuest(Quest quest)
    {
        var result = _questService.UnpublishQuest(_quests, _playerLibrary, quest);
        ReloadQuestLibrary();
        return result;
    }

    public List<string> CollectCompletedQuests()
    {
        var logs = _questService.CollectCompletedQuests(
            _player, _quests, _resources, _npcById, _playerLibrary, CurrentDay);
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
        OnPropertyChanged(nameof(QuestShop));
        OnPropertyChanged(nameof(PurchasedQuests));
        OnPropertyChanged(nameof(PublishedQuests));
        OnPropertyChanged(nameof(CompletedQuests));
    }

    public void SaveAll()
    {
        _db.SavePlayer(_player);

        using var tx = _db.GetConnection().BeginTransaction();
        try
        {
            foreach (var n in _npcs)
                _db.SaveNpcInTransaction(n, tx);
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }

        foreach (var r in _resources)
            _db.SaveResource(r);

        var dirty = _locations.Where(l => l.IsDirty).ToList();
        if (dirty.Count > 0)
        {
            _db.SaveLocationsBatch(dirty);
            foreach (var loc in dirty) loc.ClearDirty();
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

        // ── Monster faction simulation ──────────────────────────────────────
        var monsterLogs = MonsterFactionFactory.SimulateDay(_monsterFactions, _rnd);
        foreach (var log in monsterLogs)
            dayResult.Logs.Add((log, true));

        // NPC AI: high monster threat erodes devotion
        double avgThreat = _monsterFactions.Count > 0
            ? _monsterFactions.Average(f => f.ThreatLevel)
            : 0;
        if (avgThreat > 60)
        {
            int penalty = avgThreat > 80 ? 5 : 2;
            foreach (var npc in _npcs.Where(n => n.IsAlive))
                npc.Devotion = Math.Max(0, npc.Devotion - penalty);
            dayResult.Logs.Add(($"🔴 Давление монстров ({avgThreat:F0}): выжившие теряют -{penalty} Преданности", true));
        }

        _player.CurrentDay++;
        _player.PlayerActionsToday = 0;
        CurrentDay = _player.CurrentDay;
        DevPoints = _player.DevPoints;
        ActionsToday = _player.PlayerActionsToday;

        // ── Victory check ───────────────────────────────────────────────────
        if (!_trueTerminal.IsAchieved)
        {
            int aliveFollowers = _npcs.Count(n => n.IsAlive && n.FollowerLevel > 0);
            if (_trueTerminal.CheckAchieved(_player.TerminalLevel, aliveFollowers, _player.DevPoints, _player.CurrentDay))
            {
                _trueTerminal.IsAchieved = true;
                _trueTerminal.AchievedDay = _player.CurrentDay;
                dayResult.Logs.Add(("🏆 ИСТИННЫЙ ТЕРМИНАЛ ДОСТИГНУТ! ПОБЕДА!", false));
            }
        }

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

    public Npc? GetNpcById(int id) =>
        _npcById.TryGetValue(id, out var npc) ? npc : null;

    public List<ResourceCatalogEntry> GetShoppableResources()
    {
        return _catalog.Values.ToList();
    }

    public bool IsShopUnlocked(string resourceName) => _shopService.IsUnlocked(_shopUnlocks, resourceName);

    public string UnlockShopResource(string resourceName)
    {
        var result = _shopService.Unlock(_player, _resources, _shopUnlocks, resourceName);
        DevPoints = _player.DevPoints;
        return result;
    }

    public string BuyShopResource(string resourceName)
    {
        var result = _shopService.Buy(_player, _resources, _shopUnlocks, _catalog, resourceName);
        DevPoints = _player.DevPoints;
        return result;
    }

    public List<PresidentialExchangeEntry> AppliedExchangesList =>
        ExchangeCatalog.All.Where(e => _appliedExchangeIds.Contains(e.Id)).ToList();

    public int BarrierLevel
    {
        get => _player?.BarrierLevel ?? 1;
        set { if (_player != null) _player.BarrierLevel = value; OnPropertyChanged(); }
    }

    public List<int> ControlledZoneIds => _player?.ControlledZoneIds ?? new();

    public int MaxBaseUnits => _player?.MaxBaseUnits ?? 0;

    public int FreeBaseUnits => _player?.FreeBaseUnits ?? 0;

    public string ProtectLocation(int locationId)
    {
        if (_player == null) return "Ошибка";
        var loc = _locations.FirstOrDefault(l => l.Id == locationId);
        if (loc == null) return "Локация не найдена";
        if (_player.ControlledZoneIds.Contains(locationId))
            return $"«{loc.Name}» уже под защитой";

        int cost = loc.Type switch
        {
            LocationType.Apartment => 1,
            LocationType.Floor => 3,
            LocationType.Building => 15,
            LocationType.Street => 150,
            _ => 300
        };
        if (_player.FreeBaseUnits < cost)
            return $"Недостаточно БЕ (нужно {cost:F0})";

        _player.BaseUnits += cost;
        _player.ControlledZoneIds.Add(locationId);
        _player.TerritoryControl = _player.ControlledZoneIds.Count;
        _db.SavePlayer(_player);
        DevPoints = _player.DevPoints;
        return $"«{loc.Name}» взята под защиту ({cost:F0} БЕ)";
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

    // ── Tech catalog & inventory ─────────────────────────────────────────────

    public List<Technique> GetTechniqueCatalog() => _techniqueService.GetByFaction(_player.Faction.ToString(), 10);

    public Dictionary<string, int> TechInventoryCounts =>
        _techInventory.GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());

    public List<string> TechInventory => _techInventory;

    /// <summary>Buy one copy of a technique from the catalog (costs OPCost ОР).</summary>
    public string BuyTechnique(Technique tech)
    {
        if (TerminalLevel < tech.TerminalLevel)
            return $"Требуется уровень Терминала {tech.TerminalLevel} (сейчас {TerminalLevel})";
        if (_player.DevPoints < tech.OPCost)
            return $"Недостаточно ОР (нужно {tech.OPCost:F0}, есть {_player.DevPoints:F0})";

        _player.DevPoints -= tech.OPCost;
        _db.SavePlayer(_player);
        _db.AddTechInventoryItem(_db.CurrentSaveId, tech.CatalogKey);
        _techInventory.Add(tech.CatalogKey);
        RefreshInventoryTechniques();
        DevPoints = _player.DevPoints;
        return $"Куплено: «{tech.Name}» за {tech.OPCost:F0} ОР";
    }

    /// <summary>
    /// Teach a technique from inventory to an NPC.
    /// Removes one copy from inventory. NPC cannot re-learn what it already knows.
    /// </summary>
    public string TeachTechnique(Npc npc, Technique tech)
    {
        if (!_techInventory.Contains(tech.CatalogKey))
            return "Нет в инвентаре";
        if (npc.LearnedTechIds.Contains(tech.CatalogKey))
            return $"«{npc.Name}» уже знает технику «{tech.Name}»";

        // Check required stats
        foreach (var (statId, minVal) in tech.RequiredStats)
        {
            int cur = npc.Stats.GetStatValue(statId);
            if (cur < minVal)
            {
                string sname = npc.Stats.GetByNumber(statId)?.Name ?? $"Стат {statId}";
                return $"Требуется {sname} ≥ {minVal} (есть {cur})";
            }
        }

        npc.LearnedTechIds.Add(tech.CatalogKey);
        _db.RemoveTechInventoryItem(_db.CurrentSaveId, tech.CatalogKey);
        _techInventory.Remove(tech.CatalogKey);
        RefreshInventoryTechniques();
        _db.SaveNpc(npc);
        return $"«{npc.Name}» обучен технике «{tech.Name}»";
    }

    private void RefreshInventoryTechniques() =>
        _inventoryTechniques = _techniqueService.Resolve(_techInventory);

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public async Task<DayResult> ProcessNpcDayAsync(IProgress<(int current, int total)>? progress = null)
    {
        return await _optimizedLoop.ProcessNpcDaysParallelAsync(
            _player, _npcs, _resources, _locations, _player.CurrentDay,
            _player.FactionCoeffs.CoeffStatGrowth, progress);
    }

    public async Task<DayResult> ProcessNpcDayOptimizedAsync(IProgress<(int, int)>? progress = null, bool alertsOnly = false)
    {
        return await _optimizedLoop.ProcessNpcDaysParallelAsync(
            _player,
            _npcs,
            _resources,
            _locations,
            _player.CurrentDay,
            _player.FactionCoeffs.CoeffStatGrowth,
            progress,
            alertsOnly);
    }

    // Добавьте этот метод для быстрого применения обменов
    public List<PresidentialExchangeEntry> SetupAndApplyDayExchanges(int day)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"      SetupAndApplyDayExchanges: день {day} START");

        if (!ExchangeCatalog.IsCriticalDay(day))
        {
            return new List<PresidentialExchangeEntry>();
        }

        var exchanges = ExchangeCatalog.GetForDay(day, _appliedExchangeIds, _rnd);

        if (exchanges.Count == 0)
            return exchanges;

        // Применяем обмены быстро (без сохранения)
        foreach (var ex in exchanges)
        {
            if (_appliedExchangeIds.Contains(ex.Id)) continue;

            ExchangeSystem.ApplyFast(ex, _npcs, _resources);
            _appliedExchangeIds.Add(ex.Id);
            _db.SaveAppliedExchange(_db.CurrentSaveId, ex.Id);
        }

        // Сохраняем всех NPC одной транзакцией
        var saveSw = System.Diagnostics.Stopwatch.StartNew();
        using (var transaction = _db.GetConnection().BeginTransaction())
        {
            try
            {
                foreach (var npc in _npcs)
                    if (npc.IsAlive)
                        _db.SaveNpcInTransaction(npc, transaction);

                transaction.Commit();
                System.Diagnostics.Debug.WriteLine($"      Сохранено NPC за {saveSw.ElapsedMilliseconds} мс");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"      Ошибка сохранения: {ex.Message}");
                throw;
            }
        }

        // Сохраняем ресурсы
        foreach (var res in _resources)
            _db.SaveResource(res);

        PendingExchanges = new List<PresidentialExchangeEntry>();
        OnPropertyChanged(nameof(PendingExchanges));
        OnPropertyChanged(nameof(AppliedExchangesList));

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"      SetupAndApplyDayExchanges: ВСЕГО {sw.ElapsedMilliseconds} мс");
        return exchanges;
    }
}
