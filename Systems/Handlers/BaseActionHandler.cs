using System;
using System.Collections.Generic;
using System.Linq;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems.Handlers;

public abstract class BaseActionHandler
{
    protected Database.DatabaseManager _db;
    protected Random _rnd;
    protected Action<string, string>? _logAction;
    protected Dictionary<string, double> _config;

    protected BaseActionHandler(Database.DatabaseManager db, Random rnd)
    {
        _db = db;
        _rnd = rnd;
        _config = new Dictionary<string, double>();
    }

    protected BaseActionHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction)
    {
        _db = db;
        _rnd = rnd;
        _logAction = logAction;
        _config = new Dictionary<string, double>();
    }

    protected BaseActionHandler(Database.DatabaseManager db, Random rnd, Action<string, string> logAction,
        Dictionary<string, double> config)
    {
        _db = db;
        _rnd = rnd;
        _logAction = logAction;
        _config = config;
    }

    public abstract string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests);

    protected void Log(string text, string color)
    {
        _logAction?.Invoke(text, color);
    }

    protected double GetConfig(string key, double fallback) =>
        _config.TryGetValue(key, out var v) ? v : fallback;

    protected Npc? GetNpcById(List<Npc> npcs, int id) =>
        npcs.FirstOrDefault(n => n.Id == id);

    protected Resource? GetResourceByName(List<Resource> resources, string name) =>
        resources.FirstOrDefault(r => r.Name == name);

    protected string FormatTemplate(string template, Dictionary<string, object> values)
    {
        var result = template;
        foreach (var kvp in values)
        {
            var valueStr = kvp.Value?.ToString() ?? "null";
            result = result.Replace($"{{{kvp.Key}}}", valueStr);
        }
        return result;
    }
}
