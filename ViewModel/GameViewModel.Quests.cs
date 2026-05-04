// ViewModel/GameViewModel.Quests.cs — Quest, Shop, Technique methods
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Models.TechniqueData;

namespace ApocMinimal.ViewModels;

public partial class GameViewModel
{
    // ── Quest library ────────────────────────────────────────────────────────

    public void ReloadQuestLibrary()
    {
        _questCatalog  = _db.GetQuestCatalog(999);
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

    // ── Shop ─────────────────────────────────────────────────────────────────

    public List<ResourceCatalogEntry> GetShoppableResources() => _catalog.Values.ToList();

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

    // ── Techniques ───────────────────────────────────────────────────────────

    public List<Technique> GetTechniqueCatalog() =>
        _techniqueService.GetByFaction(_player.Faction.ToString(), 10);

    public Dictionary<string, int> TechInventoryCounts =>
        _techInventory.GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());

    public List<string> TechInventory => _techInventory;

    public string BuyTechnique(Technique tech)
    {
        if (TerminalLevel < tech.TerminalLevel)
            return $"Требуется уровень Терминала {tech.TerminalLevel} (сейчас {TerminalLevel})";
        if (_player.DevPoints < tech.OPCost)
            return $"Недостаточно ОР (нужно {tech.OPCost:F0}, есть {_player.DevPoints:F0})";

        _player.DevPoints -= tech.OPCost;
        _db.AddTechInventoryItem(_db.CurrentSaveId, tech.CatalogKey);
        _techInventory.Add(tech.CatalogKey);
        RefreshInventoryTechniques();
        DevPoints = _player.DevPoints;
        return $"Куплено: «{tech.Name}» за {tech.OPCost:F0} ОР";
    }

    public string TeachTechnique(Npc npc, Technique tech)
    {
        if (!_techInventory.Contains(tech.CatalogKey))
            return "Нет в инвентаре";
        if (npc.LearnedTechIds.Contains(tech.CatalogKey))
            return $"«{npc.Name}» уже знает технику «{tech.Name}»";

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
        return $"«{npc.Name}» обучен технике «{tech.Name}»";
    }

    private void RefreshInventoryTechniques() =>
        _inventoryTechniques = _techniqueService.Resolve(_techInventory);
}
