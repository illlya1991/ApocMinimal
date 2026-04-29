using ApocMinimal.Database;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.Services;

/// <summary>
/// Завантажує всі техніки один раз із БД і надає швидкий доступ через індекси в пам'яті.
/// </summary>
public class TechniqueService
{
    private readonly DatabaseManager _db;

    // Основне сховище: CatalogKey → Technique
    private Dictionary<string, Technique> _byKey = new();

    // Індекс: faction → список технік (включно з "" = загальні)
    private Dictionary<string, List<Technique>> _byFaction = new();

    // Індекс: terminalLevel → список технік з TerminalLevel <= key
    // (зберігаємо просто список для фільтрації in-memory)
    private List<Technique> _all = new();

    public int TotalTechniques { get; private set; }

    public TechniqueService(DatabaseManager db)
    {
        _db = db;
    }

    public void Initialize()
    {
        _all = _db.GetAllTechniques();
        TotalTechniques = _all.Count;

        foreach (var t in _all)
        {
            _byKey[t.CatalogKey] = t;

            if (!_byFaction.ContainsKey(t.Faction))
                _byFaction[t.Faction] = new List<Technique>();
            _byFaction[t.Faction].Add(t);
        }
    }

    /// <summary>Усі техніки фракції + загальні (Faction==""), рівень ≤ maxLevel.</summary>
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

    /// <summary>Усі техніки з TerminalLevel ≤ maxLevel.</summary>
    public List<Technique> GetByMaxLevel(int maxLevel)
    {
        var result = new List<Technique>(_all.Count);
        foreach (var t in _all)
            if (t.TerminalLevel <= maxLevel) result.Add(t);
        return result;
    }

    /// <summary>Пошук за CatalogKey. O(1).</summary>
    public Technique? GetByKey(string key)
    {
        _byKey.TryGetValue(key, out var t);
        return t;
    }

    /// <summary>Усі техніки (копія списку).</summary>
    public List<Technique> GetAll() => _all;

    /// <summary>Фільтрує список ключів до об'єктів Technique.</summary>
    public List<Technique> Resolve(IEnumerable<string> keys)
    {
        var result = new List<Technique>();
        foreach (var k in keys)
            if (_byKey.TryGetValue(k, out var t)) result.Add(t);
        return result;
    }
}
