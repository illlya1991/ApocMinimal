// ViewModels/GameViewModel.cs
using ApocMinimal.Database;
using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Systems;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApocMinimal.ViewModels;

public class GameViewModel : INotifyPropertyChanged
{
    private readonly DatabaseManager _db;
    private readonly ActionManager _actionManager;
    private readonly Random _rnd = new();

    private Player _player = null!;
    private List<Npc> _npcs = new();
    private List<Resource> _resources = new();
    private List<Quest> _quests = new();
    private List<Location> _locations = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    // Observable properties
    private int _currentDay;
    public int CurrentDay
    {
        get => _currentDay;
        set { _currentDay = value; OnPropertyChanged(); OnPropertyChanged(nameof(DayDisplay)); }
    }

    private double _faithPoints;
    public double FaithPoints
    {
        get => _faithPoints;
        set { _faithPoints = value; OnPropertyChanged(); OnPropertyChanged(nameof(FaithDisplay)); OnPropertyChanged(nameof(CanUpgrade)); }
    }

    private int _altarLevel;
    public int AltarLevel
    {
        get => _altarLevel;
        set { _altarLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(AltarDisplay)); OnPropertyChanged(nameof(UpgradeCost)); OnPropertyChanged(nameof(CanUpgrade)); OnPropertyChanged(nameof(UnlockedTechniques)); }
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
    public string FaithDisplay => $"{FaithPoints:F0} веры";
    public string AltarDisplay => $"Алтарь: ур.{AltarLevel}";
    public string ActionsDisplay => $"Действий: {ActionsToday}/{Player.MaxPlayerActionsPerDay}";
    public bool HasActionsLeft => ActionsToday < Player.MaxPlayerActionsPerDay;
    public long UpgradeCost => (long)(200 * Math.Pow(5, AltarLevel - 1));
    public bool CanUpgrade => AltarLevel < 10 && FaithPoints >= UpgradeCost;
    public string PlayerName => _player?.Name ?? "Божество";
    public int AliveNpcsCount => _npcs.Count(n => n.IsAlive);
    public int MaxActiveFollowers => _player?.MaxActiveFollowers ?? 0;

    public List<ActionGroup> ActionGroups { get; private set; } = new();
    public GameActionDb? SelectedAction { get; set; }

    // Properties for UI binding
    public List<Npc> AllNpcs => _npcs;
    public List<Npc> AliveNpcs => _npcs.Where(n => n.IsAlive).ToList();
    public List<Resource> Resources => _resources;
    public List<Quest> AvailableQuests => _quests.Where(q => q.Status == QuestStatus.Available).ToList();
    public List<Quest> ActiveQuests => _quests.Where(q => q.Status == QuestStatus.Active).ToList();
    public List<Location> Locations => _locations;
    public IEnumerable<Technique> UnlockedTechniques => _player?.UnlockedTechniques ?? Enumerable.Empty<Technique>();
    public Technique[] AllTechniques => Player.AllTechniques;

    public GameViewModel(DatabaseManager db, Action<string, string> logAction)
    {
        _db = db;
        _actionManager = new ActionManager(_db, _rnd, logAction);
        LoadData();
        ActionGroups = _actionManager.GetGroups();
    }

    private void LoadData()
    {
        _player = _db.GetPlayer()!;
        _npcs = _db.GetAllNpcs();
        _resources = _db.GetAllResources();
        _quests = _db.GetAllQuests();
        _locations = _db.GetAllLocations();

        CurrentDay = _player.CurrentDay;
        FaithPoints = _player.FaithPoints;
        AltarLevel = _player.AltarLevel;
        ActionsToday = _player.PlayerActionsToday;
        BarrierSize = _player.BarrierSize;
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
    }

    public void SaveAll()
    {
        _db.SavePlayer(_player);
        foreach (var n in _npcs) _db.SaveNpc(n);
        foreach (var r in _resources) _db.SaveResource(r);
        foreach (var q in _quests.Where(q => q.Status != QuestStatus.Available)) _db.SaveQuest(q);
    }

    public void SavePlayer()
    {
        _db.SavePlayer(_player);
    }

    public void SaveNpc(Npc npc)
    {
        _db.SaveNpc(npc);
    }

    public List<GameActionDb> GetActionsByGroup(int groupId) =>
        _actionManager.GetActionsByGroup(groupId);

    public string ExecuteAction(GameActionDb action, Dictionary<string, object> parameters) =>
        _actionManager.ExecuteAction(action, parameters, _player, _npcs, _resources, _quests);

    public void ProcessEndOfDay(Action<string, string> logAction)
    {
        _player.CurrentDay++;
        _player.PlayerActionsToday = 0;

        var dayResult = GameLoopService.ProcessDay(_player, _npcs, _resources, _quests, _rnd);

        foreach (var (npc, q) in dayResult.QuestRewards)
        {
            var res = _resources.FirstOrDefault(r => r.Id == q.RewardResourceId);
            if (res != null) res.Amount += q.RewardAmount;
        }

        _quests.AddRange(dayResult.NewQuests);

        // Update properties
        CurrentDay = _player.CurrentDay;
        FaithPoints = _player.FaithPoints;
        ActionsToday = _player.PlayerActionsToday;
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

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}