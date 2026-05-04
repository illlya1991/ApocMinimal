// ViewModel/GameViewModel.cs — Core: fields, ctor, LoadData, save, advance
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

public partial class GameViewModel : INotifyPropertyChanged
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
        _shopService  = new ShopService(db);

        LoadData();
        _locationService.UpdateNpcCache(_npcs);

        _actionManager = new ActionManager(_db, _techniqueService, _rnd, logAction, _catalog, _gameConfig);
        ActionGroups = _actionManager.GetGroups();

        NpcCache = new NpcCacheService();
        NpcCache.LoadAll(_npcs);
        LocationCache = new LocationCacheService();
        LocationCache.Initialize(_locations);
    }

    // ── Observable properties ────────────────────────────────────────────────

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

    public string DayDisplay     => $"День {CurrentDay}";
    public string DevPointsDisplay => $"ОР: {DevPoints:F0}";
    public string TerminalDisplay => $"Терминал: ур.{TerminalLevel}";
    public string ActionsDisplay  => $"Действий: {ActionsToday}/{Player.MaxPlayerActionsPerDay}";
    public bool   HasActionsLeft  => ActionsToday < Player.MaxPlayerActionsPerDay;
    public long   UpgradeCost     => _player?.UpgradeCost ?? (long)(200 * Math.Pow(5, TerminalLevel - 1));
    public bool   CanUpgrade      => TerminalLevel < 10 && DevPoints >= UpgradeCost;
    public string PlayerName      => _player?.Name ?? "Игрок";
    public PlayerFaction PlayerFaction => _player?.Faction ?? PlayerFaction.ElementMages;
    public int    AliveNpcsCount  => _npcs.Count(n => n.IsAlive);
    public int    MaxActiveFollowers => _player?.MaxActiveFollowers ?? 0;

    public List<MonsterFaction> MonsterFactions => _monsterFactions;
    public TrueTerminal TrueTerminal => _trueTerminal;
    public bool IsVictory => _trueTerminal.IsAchieved;
    public bool IsDefeat  => !_trueTerminal.IsAchieved && _player != null && _player.CurrentDay >= 365;

    public List<PlayerActionGroup> ActionGroups { get; private set; } = new();
    public PlayerGameAction? SelectedAction { get; set; }

    public List<PlayerGameAction> GetActionsByGroup(int groupId) =>
        _actionManager.GetActionsByGroup(groupId);

    public string ExecuteAction(PlayerGameAction action, Dictionary<string, object> parameters) =>
        _actionManager.ExecuteAction(action, parameters, _player, _npcs, _resources, _quests);

    public Player GetPlayer()           => _player;
    public List<Npc>      AllNpcs       => _npcs;
    public List<Npc>      AliveNpcs     => _npcs.Where(n => n.IsAlive).ToList();
    public List<Resource> Resources     => _resources;
    public List<Quest>    AvailableQuests => _quests.Where(q => q.Status == QuestStatus.Available).ToList();
    public List<Quest>    ActiveQuests   => _quests.Where(q => q.Status == QuestStatus.Active).ToList();
    public List<Location> Locations      => _locationService.GetAllLocations();
    public IEnumerable<Technique> UnlockedTechniques => _inventoryTechniques;
    public List<QuestCatalogEntry>  QuestShop       => _questCatalog.Where(q => q.MinTerminalLevel <= TerminalLevel).ToList();
    public List<PlayerLibraryEntry> PurchasedQuests => _playerLibrary;
    public List<Quest>    PublishedQuests  => _quests.Where(q => q.Status == QuestStatus.Available).ToList();
    public List<Quest>    CompletedQuests  => _quests.Where(q => q.Status == QuestStatus.Completed).ToList();
    public List<QuestHistoryEntry> QuestHistory => _db.GetQuestHistory(_db.CurrentSaveId);

    public List<Location> GetChildren(int parentId)          => _locationService.GetChildren(parentId);
    public int GetNpcCountAtLocation(int locationId)          => _locationService.GetNpcCountAtLocation(locationId);
    public Npc? GetNpcById(int id)                            => _npcById.TryGetValue(id, out var npc) ? npc : null;
    public int  GetFollowerLimit(int followerLevel)           => _player?.GetFollowerLimit(followerLevel) ?? 0;
    public int  GetFollowerCountAtLevel(int followerLevel)    => _db.GetFollowerCountAtLevel(followerLevel);

    // ── Data load ────────────────────────────────────────────────────────────

    private void LoadData()
    {
        System.Diagnostics.Debug.WriteLine("=== LoadData: НАЧАЛО ===");
        var sw    = System.Diagnostics.Stopwatch.StartNew();
        var memSw = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"  Память до загрузки: {GC.GetTotalMemory(true) / (1024 * 1024)} МБ");

        _player = _db.GetPlayer()!;
        System.Diagnostics.Debug.WriteLine($"    Player загружен за {memSw.ElapsedMilliseconds} мс"); memSw.Restart();

        _npcs    = _db.GetAllNpcsOptimized();
        _npcById = _npcs.ToDictionary(n => n.Id);
        System.Diagnostics.Debug.WriteLine($"    NPC: {_npcs.Count} шт. за {memSw.ElapsedMilliseconds} мс"); memSw.Restart();

        _resources = _db.GetAllResources();
        _quests    = _db.GetAllQuests();
        _locations = _db.GetAllLocations();
        System.Diagnostics.Debug.WriteLine($"    Ресурсы/Квесты/Локации за {memSw.ElapsedMilliseconds} мс"); memSw.Restart();

        var catalogList = _db.GetResourceCatalog();
        _catalog = new Dictionary<string, ResourceCatalogEntry>(catalogList.Count);
        for (int i = 0; i < catalogList.Count; i++)
            _catalog[catalogList[i].Name] = catalogList[i];

        _gameConfig    = _db.GetGameConfig();
        _questCatalog  = _db.GetQuestCatalog(999);
        _playerLibrary = _db.GetPlayerLibrary(_db.CurrentSaveId);
        _shopUnlocks   = _db.GetShopUnlocks(_db.CurrentSaveId);
        _appliedExchangeIds = _db.GetAppliedExchanges(_db.CurrentSaveId);
        _techInventory = _db.GetTechInventory(_db.CurrentSaveId);
        RefreshInventoryTechniques();
        System.Diagnostics.Debug.WriteLine($"    Каталоги/Магазин/Техники за {memSw.ElapsedMilliseconds} мс"); memSw.Restart();

        bool needsMigration = _npcs.Count > 0 && _npcs.Any(n =>
            n.Needs.Count(nd => nd.Category == NeedCategory.Special) > 5 ||
            !n.Needs.Any(nd => nd.Name == "Самосовершенствование"));
        if (needsMigration)
        {
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

        CurrentDay  = _player.CurrentDay;
        DevPoints   = _player.DevPoints;
        TerminalLevel = _player.TerminalLevel;
        ActionsToday  = _player.PlayerActionsToday;

        if (_monsterFactions.Count == 0)
            _monsterFactions = MonsterFactionFactory.CreateDefault();

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"=== LoadData: ВСЕГО {sw.ElapsedMilliseconds} мс, память: {GC.GetTotalMemory(true) / (1024 * 1024)} МБ ===");
    }

    // ── Refresh / Save ───────────────────────────────────────────────────────

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

    public void SaveLocation(Location loc) { _db.SaveLocation(loc); loc.ClearDirty(); }
    public void SavePlayer()               => _db.SavePlayer(_player);
    public void SaveNpc(Npc npc)           => _db.SaveNpc(npc);

    // ── Day advance ──────────────────────────────────────────────────────────

    public DayResult AdvanceToNextDay()
    {
        var dayResult = GameLoopService.ProcessDayEnd(_player, _npcs, _resources, _quests, _rnd, _catalog);

        foreach (var (npc, q) in dayResult.QuestRewards)
        {
            var res = _resources.FirstOrDefault(r => r.Id == q.RewardResourceId);
            if (res != null) res.Amount += q.RewardAmount;
        }
        _quests.AddRange(dayResult.NewQuests);

        var monsterLogs = MonsterFactionFactory.SimulateDay(_monsterFactions, _rnd);
        foreach (var log in monsterLogs)
            dayResult.Logs.Add((log, true));

        double avgThreat = _monsterFactions.Count > 0 ? _monsterFactions.Average(f => f.ThreatLevel) : 0;
        if (avgThreat > 60)
        {
            int penalty = avgThreat > 80 ? 5 : 2;
            foreach (var npc in _npcs.Where(n => n.IsAlive))
                npc.Devotion = Math.Max(0, npc.Devotion - penalty);
            dayResult.Logs.Add(($"🔴 Давление монстров ({avgThreat:F0}): выжившие теряют -{penalty} Преданности", true));
        }

        _player.CurrentDay++;
        _player.PlayerActionsToday = 0;
        CurrentDay   = _player.CurrentDay;
        DevPoints    = _player.DevPoints;
        ActionsToday = _player.PlayerActionsToday;

        if (!_trueTerminal.IsAchieved)
        {
            int aliveFollowers = _npcs.Count(n => n.IsAlive && n.FollowerLevel > 0);
            if (_trueTerminal.CheckAchieved(_player.TerminalLevel, aliveFollowers, _player.DevPoints, _player.CurrentDay))
            {
                _trueTerminal.IsAchieved  = true;
                _trueTerminal.AchievedDay = _player.CurrentDay;
                dayResult.Logs.Add(("🏆 ИСТИННЫЙ ТЕРМИНАЛ ДОСТИГНУТ! ПОБЕДА!", false));
            }
        }

        return dayResult;
    }

    public async Task<DayResult> ProcessNpcDayAsync(IProgress<(int current, int total)>? progress = null) =>
        await _optimizedLoop.ProcessNpcDaysParallelAsync(
            _player, _npcs, _resources, _locations, _player.CurrentDay,
            _player.FactionCoeffs.CoeffStatGrowth, progress);

    public async Task<DayResult> ProcessNpcDayOptimizedAsync(IProgress<(int, int)>? progress = null, bool alertsOnly = false) =>
        await _optimizedLoop.ProcessNpcDaysParallelAsync(
            _player, _npcs, _resources, _locations, _player.CurrentDay,
            _player.FactionCoeffs.CoeffStatGrowth, progress, alertsOnly);

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
