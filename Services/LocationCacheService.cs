using System.Collections.Concurrent;
using ApocMinimal.Models.LocationData;

namespace ApocMinimal.Services;

/// <summary>
/// Кэш для локаций с предвычисленными индексами.
/// Оптимизирован для быстрого поиска по иерархии.
/// </summary>
public class LocationCacheService
{
    private readonly ConcurrentDictionary<int, Location> _locationsById = new();
    private readonly ConcurrentDictionary<int, List<int>> _childrenByParent = new();
    private readonly ConcurrentDictionary<LocationType, List<int>> _locationsByType = new();

    // Предвычисленные пути для быстрого доступа
    private readonly ConcurrentDictionary<int, List<Location>> _pathCache = new();

    private int _cacheHits;
    private int _cacheMisses;

    public int TotalLocations => _locationsById.Count;
    public int CacheHits => _cacheHits;
    public int CacheMisses => _cacheMisses;

    public LocationCacheService()
    {
        foreach (LocationType type in Enum.GetValues(typeof(LocationType)))
        {
            _locationsByType[type] = new List<int>();
        }
    }

    public void Initialize(List<Location> locations)
    {
        _locationsById.Clear();
        _childrenByParent.Clear();
        _pathCache.Clear();

        foreach (var type in _locationsByType.Keys.ToList())
        {
            _locationsByType[type].Clear();
        }

        foreach (var loc in locations)
        {
            _locationsById[loc.Id] = loc;

            if (!_childrenByParent.ContainsKey(loc.ParentId))
            {
                _childrenByParent[loc.ParentId] = new List<int>();
            }
            _childrenByParent[loc.ParentId].Add(loc.Id);

            if (_locationsByType.ContainsKey(loc.Type))
            {
                _locationsByType[loc.Type].Add(loc.Id);
            }
        }
    }

    public Location? GetLocation(int id)
    {
        if (_locationsById.TryGetValue(id, out Location? loc))
        {
            Interlocked.Increment(ref _cacheHits);
            return loc;
        }
        Interlocked.Increment(ref _cacheMisses);
        return null;
    }

    public List<Location> GetChildren(int parentId)
    {
        var result = new List<Location>();
        if (_childrenByParent.TryGetValue(parentId, out List<int>? childIds))
        {
            Interlocked.Increment(ref _cacheHits);
            foreach (var id in childIds)
            {
                if (_locationsById.TryGetValue(id, out Location? loc))
                {
                    result.Add(loc);
                }
            }
        }
        else
        {
            Interlocked.Increment(ref _cacheMisses);
        }
        return result;
    }

    public List<Location> GetAllDescendants(int parentId)
    {
        var result = new List<Location>();
        CollectDescendants(parentId, result);
        return result;
    }

    private void CollectDescendants(int parentId, List<Location> result)
    {
        if (_childrenByParent.TryGetValue(parentId, out List<int>? childIds))
        {
            foreach (var childId in childIds)
            {
                if (_locationsById.TryGetValue(childId, out Location? child))
                {
                    result.Add(child);
                    CollectDescendants(childId, result);
                }
            }
        }
    }

    public List<Location> GetPathToRoot(int locationId)
    {
        if (_pathCache.TryGetValue(locationId, out List<Location>? cachedPath))
        {
            Interlocked.Increment(ref _cacheHits);
            return cachedPath.ToList();
        }

        Interlocked.Increment(ref _cacheMisses);
        var path = new List<Location>();
        var current = GetLocation(locationId);

        while (current != null)
        {
            path.Add(current);
            current = GetLocation(current.ParentId);
        }

        path.Reverse();
        _pathCache[locationId] = path;
        return path;
    }

    public List<Location> GetLocationsByType(LocationType type)
    {
        var result = new List<Location>();
        if (_locationsByType.TryGetValue(type, out List<int>? ids))
        {
            foreach (var id in ids)
            {
                if (_locationsById.TryGetValue(id, out Location? loc))
                {
                    result.Add(loc);
                }
            }
        }
        return result;
    }

    public void UpdateLocation(Location loc)
    {
        _locationsById[loc.Id] = loc;
        _pathCache.TryRemove(loc.Id, out _);
        loc.IsDirty = true;
    }

    public void MarkAsExplored(int locationId)
    {
        var loc = GetLocation(locationId);
        if (loc != null && !loc.IsExplored)
        {
            loc.IsExplored = true;
            loc.IsDirty = true;
        }
    }

    public void MarkAsCleared(int locationId)
    {
        var loc = GetLocation(locationId);
        if (loc != null && loc.Status != LocationStatus.Cleared)
        {
            loc.Status = LocationStatus.Cleared;
            loc.IsDirty = true;
        }
    }

    public List<Location> GetDirtyLocations()
    {
        var result = new List<Location>();
        foreach (var loc in _locationsById.Values)
        {
            if (loc.IsDirty)
                result.Add(loc);
        }
        return result;
    }

    public void ClearDirtyFlags()
    {
        foreach (var loc in _locationsById.Values)
        {
            loc.ClearDirty();
        }
    }

    public double GetHitRate()
    {
        int total = _cacheHits + _cacheMisses;
        return total == 0 ? 0 : (double)_cacheHits / total;
    }
}