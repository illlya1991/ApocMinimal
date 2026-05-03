using System.Collections.Concurrent;
using ApocMinimal.Models.PersonData;

namespace ApocMinimal.Services;

/// <summary>
/// Потокобезопасный кэш для NPC.
/// Обеспечивает O(1) доступ по ID и инвалидацию при изменениях.
/// </summary>
public class NpcCacheService
{
    private readonly ConcurrentDictionary<int, Npc> _npcsById = new();
    private readonly ConcurrentDictionary<string, int> _npcsByName = new();
    private readonly ReaderWriterLockSlim _lock = new();

    // Статистика кэша
    private int _cacheHits;
    private int _cacheMisses;

    public int CacheHits => _cacheHits;
    public int CacheMisses => _cacheMisses;
    public int Size => _npcsById.Count;

    public NpcCacheService()
    {
        // Ежедневная очистка статистики
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromDays(1));
                ResetStatistics();
            }
        });
    }

    public void LoadAll(List<Npc> npcs)
    {
        _lock.EnterWriteLock();
        try
        {
            _npcsById.Clear();
            _npcsByName.Clear();

            foreach (var npc in npcs)
            {
                _npcsById[npc.Id] = npc;
                _npcsByName[npc.Name.ToLowerInvariant()] = npc.Id;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Npc? GetById(int id)
    {
        _lock.EnterReadLock();
        try
        {
            if (_npcsById.TryGetValue(id, out Npc? npc))
            {
                Interlocked.Increment(ref _cacheHits);
                return npc;
            }
            Interlocked.Increment(ref _cacheMisses);
            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Npc? GetByName(string name)
    {
        _lock.EnterReadLock();
        try
        {
            if (_npcsByName.TryGetValue(name.ToLowerInvariant(), out int id))
            {
                return GetById(id);
            }
            Interlocked.Increment(ref _cacheMisses);
            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public List<Npc> GetAll()
    {
        _lock.EnterReadLock();
        try
        {
            return _npcsById.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public List<Npc> GetAlive()
    {
        _lock.EnterReadLock();
        try
        {
            var result = new List<Npc>();
            foreach (var npc in _npcsById.Values)
            {
                if (npc.IsAlive)
                    result.Add(npc);
            }
            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Update(Npc npc)
    {
        _lock.EnterWriteLock();
        try
        {
            _npcsById[npc.Id] = npc;
            _npcsByName[npc.Name.ToLowerInvariant()] = npc.Id;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void UpdateBatch(List<Npc> npcs)
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var npc in npcs)
            {
                _npcsById[npc.Id] = npc;
                _npcsByName[npc.Name.ToLowerInvariant()] = npc.Id;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Remove(int id)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_npcsById.TryRemove(id, out Npc? npc))
            {
                if (npc != null)
                    _npcsByName.TryRemove(npc.Name.ToLowerInvariant(), out _);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _npcsById.Clear();
            _npcsByName.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void ResetStatistics()
    {
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
    }

    public double GetHitRate()
    {
        int total = _cacheHits + _cacheMisses;
        return total == 0 ? 0 : (double)_cacheHits / total;
    }
}