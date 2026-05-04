using ApocMinimal.Database;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Services;

public class ShopService
{
    private readonly DatabaseManager _db;

    public ShopService(DatabaseManager db) => _db = db;

    public bool IsUnlocked(List<string> unlocks, string resourceName)
        => unlocks.Contains(resourceName);

    public string Unlock(Player player, List<Resource> resources,
        List<string> unlocks, string resourceName)
    {
        if (unlocks.Contains(resourceName)) return $"{resourceName} уже разблокирован";
        var res = resources.FirstOrDefault(r => r.Name == resourceName);
        if (res == null || res.Amount < 1) return $"Недостаточно {resourceName} (нужна 1 ед.)";
        if (player.DevPoints < 5) return "Недостаточно ОР (нужно 5)";

        res.Amount -= 1;
        player.DevPoints -= 5;
        _db.SaveResource(res);
        _db.SavePlayer(player);
        _db.UnlockShopResource(_db.CurrentSaveId, resourceName);
        unlocks.Add(resourceName);
        return $"Разблокирована покупка: {resourceName}";
    }

    public string Buy(Player player, List<Resource> resources, List<string> unlocks,
        Dictionary<string, ResourceCatalogEntry> catalog, string resourceName)
    {
        if (!unlocks.Contains(resourceName)) return $"{resourceName} не разблокирован";
        if (!catalog.TryGetValue(resourceName, out var entry)) return "Ресурс не найден в каталоге";

        double price = entry.Quality switch { 1 => 2, 2 => 3, 3 => 5, 4 => 10, 5 => 20, _ => 5 };
        if (player.DevPoints < price) return $"Недостаточно ОР (нужно {price:F0})";

        player.DevPoints -= price;
        _db.SavePlayer(player);

        var res = resources.FirstOrDefault(r => r.Name == resourceName);
        if (res != null) { res.Amount += 10; _db.SaveResource(res); }

        return $"Куплено 10 ед. {resourceName} за {price:F0} ОР";
    }
}
