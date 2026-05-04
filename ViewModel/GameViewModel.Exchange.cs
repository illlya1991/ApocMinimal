// ViewModel/GameViewModel.Exchange.cs — Exchange and Barrier/Territory methods
using ApocMinimal.Models.ExchangeData;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Systems;

namespace ApocMinimal.ViewModels;

public partial class GameViewModel
{
    // ── Exchanges ────────────────────────────────────────────────────────────

    public List<PresidentialExchangeEntry> AppliedExchangesList =>
        ExchangeCatalog.All.Where(e => _appliedExchangeIds.Contains(e.Id)).ToList();

    public string ApplyExchange(PresidentialExchangeEntry ex)
    {
        if (_appliedExchangeIds.Contains(ex.Id))
            return $"Обмен «{ex.Name}» уже был применён.";

        ExchangeSystem.Apply(ex, _npcs, _resources);
        _appliedExchangeIds.Add(ex.Id);
        _db.SaveAppliedExchange(_db.CurrentSaveId, ex.Id);
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

    public void SetupDayExchanges(int day) => SetupAndApplyDayExchanges(day);

    public List<PresidentialExchangeEntry> SetupAndApplyDayExchanges(int day)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"      SetupAndApplyDayExchanges: день {day} START");

        if (!ExchangeCatalog.IsCriticalDay(day))
            return new List<PresidentialExchangeEntry>();

        var exchanges = ExchangeCatalog.GetForDay(day, _appliedExchangeIds, _rnd);
        if (exchanges.Count == 0) return exchanges;

        foreach (var ex in exchanges)
        {
            if (_appliedExchangeIds.Contains(ex.Id)) continue;
            ExchangeSystem.ApplyFast(ex, _npcs, _resources);
            _appliedExchangeIds.Add(ex.Id);
            _db.SaveAppliedExchange(_db.CurrentSaveId, ex.Id);
        }

        PendingExchanges = new List<PresidentialExchangeEntry>();
        OnPropertyChanged(nameof(PendingExchanges));
        OnPropertyChanged(nameof(AppliedExchangesList));

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"      SetupAndApplyDayExchanges: ВСЕГО {sw.ElapsedMilliseconds} мс");
        return exchanges;
    }

    // ── Barrier / Territory ──────────────────────────────────────────────────

    public int BarrierLevel
    {
        get => _player?.BarrierLevel ?? 1;
        set { if (_player != null) _player.BarrierLevel = value; OnPropertyChanged(); }
    }

    public List<int> ControlledZoneIds => _player?.ControlledZoneIds ?? new();
    public int MaxBaseUnits            => _player?.MaxBaseUnits ?? 0;
    public int FreeBaseUnits           => _player?.FreeBaseUnits ?? 0;

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
            LocationType.Floor     => 3,
            LocationType.Building  => 15,
            LocationType.Street    => 150,
            _                      => 300
        };
        if (_player.FreeBaseUnits < cost)
            return $"Недостаточно БЕ (нужно {cost:F0})";

        _player.BaseUnits += cost;
        _player.ControlledZoneIds.Add(locationId);
        _player.TerritoryControl = _player.ControlledZoneIds.Count;
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
        return $"«{loc?.Name ?? $"#{locationId}"}» снята с защиты";
    }
}
