// Services/TechniqueService.cs (синхронная версия — быстрее)
using ApocMinimal.Database;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.Services;

/// <summary>
/// Загружает все техники один раз из БД и предоставляет быстрый доступ через индексы в памяти.
/// </summary>
public class TechniqueService
{
    private readonly DatabaseManager _db;

    // Основное хранилище: CatalogKey → Technique
    private Dictionary<string, Technique> _byKey = new();

    // Индекс: faction → список техник (включая "" = общие)
    private Dictionary<string, List<Technique>> _byFaction = new();

    // Индекс: terminalLevel → список техник
    private List<Technique> _all = new();

    public int TotalTechniques { get; private set; }
    public bool IsInitialized { get; private set; }

    public TechniqueService(DatabaseManager db)
    {
        _db = db;
    }

    /// <summary>
    /// Синхронная инициализация (быстрая, ~0.1 сек для 1000 техник)
    /// </summary>
    public void Initialize()
    {
        if (IsInitialized) return;

        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"=== TechniqueService: НАЧАЛО ЗАГРУЗКИ ===");

        var loadSw = System.Diagnostics.Stopwatch.StartNew();
        _all = _db.GetAllTechniquesFast(); // Используем быстрый метод!
        loadSw.Stop();
        TotalTechniques = _all.Count;
        System.Diagnostics.Debug.WriteLine($"  Загрузка из БД: {loadSw.ElapsedMilliseconds} мс, техник: {TotalTechniques}");

        var buildSw = System.Diagnostics.Stopwatch.StartNew();
        _byKey.Clear();
        _byFaction.Clear();

        foreach (var t in _all)
        {
            _byKey[t.CatalogKey] = t;

            if (!_byFaction.ContainsKey(t.Faction))
                _byFaction[t.Faction] = new List<Technique>();
            _byFaction[t.Faction].Add(t);
        }
        buildSw.Stop();
        System.Diagnostics.Debug.WriteLine($"  Построение индексов: {buildSw.ElapsedMilliseconds} мс");

        totalSw.Stop();
        System.Diagnostics.Debug.WriteLine($"=== TechniqueService: ВСЕГО {totalSw.ElapsedMilliseconds} мс ===");

        IsInitialized = true;
    }
    public List<Technique> GetByFaction(string faction, int maxLevel)
    {
        var result = new List<Technique>();

        if (_byFaction.TryGetValue("", out var general))
            foreach (var t in general)
                if (t.TerminalLevel <= maxLevel) result.Add(t);

        if (faction != "" && _byFaction.TryGetValue(faction, out var factionList))
            foreach (var t in factionList)
                if (t.TerminalLevel <= maxLevel) result.Add(t);

        return result;
    }

    public List<Technique> GetByMaxLevel(int maxLevel)
    {
        var result = new List<Technique>(_all.Count);
        foreach (var t in _all)
            if (t.TerminalLevel <= maxLevel) result.Add(t);
        return result;
    }

    public Technique? GetByKey(string key)
    {
        _byKey.TryGetValue(key, out var t);
        return t;
    }

    public List<Technique> GetAll() => _all;

    public List<Technique> Resolve(IEnumerable<string> keys)
    {
        var result = new List<Technique>();
        foreach (var k in keys)
            if (_byKey.TryGetValue(k, out var t)) result.Add(t);
        return result;
    }
}