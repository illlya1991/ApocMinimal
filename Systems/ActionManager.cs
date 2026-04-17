// ActionManager.cs

using System;
using System.Collections.Generic;
using ApocMinimal.Database;
using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Systems.Handlers;

namespace ApocMinimal.Systems;

public class ActionManager
{
    private readonly DatabaseManager _db;
    private readonly Random _rnd;
    private readonly Dictionary<string, ResourceCatalogEntry> _catalog;

    private List<PlayerActionGroup> _groups = new List<PlayerActionGroup>();
    private List<PlayerGameAction> _actions = new List<PlayerGameAction>();
    private List<HandlerEntry> _handlers = new List<HandlerEntry>();

    private class HandlerEntry
    {
        public string Name { get; set; } = "";
        public BaseActionHandler Handler { get; set; } = null!;
    }

    public ActionManager(DatabaseManager db, Random rnd, Action<string, string> logAction,
        Dictionary<string, ResourceCatalogEntry> catalog, Dictionary<string, double> gameConfig)
    {
        _db = db;
        _rnd = rnd;
        _catalog = catalog;
        LoadActions();
        InitializeHandlers(logAction, gameConfig);
    }

    private void LoadActions()
    {
        _groups = _db.GetPlayerActionGroups();
        _actions = _db.GetAllPlayerGameActions();

        List<PlayerParamType> paramTypes = _db.GetPlayerParamTypes();

        for (int i = 0; i < _actions.Count; i++)
        {
            PlayerGameAction action = _actions[i];
            action.Parameters = _db.GetPlayerActionParams(action.Id);
            action.ParamMappings = _db.GetPlayerHandlerParamMappings(action.Id);
            action.ResultTemplate = _db.GetPlayerResultTemplate(action.Id);

            for (int j = 0; j < _groups.Count; j++)
            {
                if (_groups[j].Id == action.GroupId)
                {
                    action.Group = _groups[j];
                    break;
                }
            }

            for (int j = 0; j < action.Parameters.Count; j++)
            {
                PlayerActionParam param = action.Parameters[j];
                for (int k = 0; k < paramTypes.Count; k++)
                {
                    if (paramTypes[k].Id == param.ParamTypeId)
                    {
                        param.ParamType = paramTypes[k];
                        break;
                    }
                }
            }
        }
    }

    private void InitializeHandlers(Action<string, string> logAction, Dictionary<string, double> gameConfig)
    {
        _handlers.Add(new HandlerEntry { Name = "InteractionHandler", Handler = new InteractionHandler(_db, _rnd, logAction, gameConfig) });
        _handlers.Add(new HandlerEntry { Name = "ResourceHandler",    Handler = new ResourceHandler(_db, _rnd, logAction, gameConfig) });
        _handlers.Add(new HandlerEntry { Name = "TechniqueHandler",   Handler = new TechniqueHandler(_db, _rnd, logAction) });
        _handlers.Add(new HandlerEntry { Name = "ManagementHandler",  Handler = new ManagementHandler(_db, _rnd, logAction, gameConfig) });
    }

    public List<PlayerActionGroup> GetGroups()
    {
        return _groups;
    }

    public List<PlayerGameAction> GetActionsByGroup(int groupId)
    {
        List<PlayerGameAction> result = new List<PlayerGameAction>();
        for (int i = 0; i < _actions.Count; i++)
        {
            if (_actions[i].GroupId == groupId)
                result.Add(_actions[i]);
        }
        return result;
    }

    public List<PlayerGameAction> GetAllActions()
    {
        return _actions;
    }

    public PlayerGameAction? GetActionByKey(string key)
    {
        for (int i = 0; i < _actions.Count; i++)
        {
            if (_actions[i].ActionKey == key)
                return _actions[i];
        }
        return null;
    }

    public string ExecuteAction(
        PlayerGameAction action,
        Dictionary<string, object> parameterValues,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        BaseActionHandler? handler = null;
        for (int i = 0; i < _handlers.Count; i++)
        {
            if (_handlers[i].Name == action.HandlerMethod)
            {
                handler = _handlers[i].Handler;
                break;
            }
        }

        if (handler == null)
            return $"Обработчик '{action.HandlerMethod}' не найден";

        Dictionary<string, object> extendedParams = new Dictionary<string, object>();

        foreach (var kvp in parameterValues)
        {
            extendedParams[kvp.Key] = kvp.Value;
        }
        extendedParams["_actionKey"] = action.ActionKey;

        string result = handler.Execute(extendedParams, player, npcs, resources, quests);

        if (action.ResultTemplate != null && !string.IsNullOrEmpty(action.ResultTemplate.SuccessTemplate))
        {
            result = FormatWithTemplate(action.ResultTemplate.SuccessTemplate, parameterValues, result);
        }

        return result;
    }

    private string FormatWithTemplate(string template, Dictionary<string, object> values, string defaultResult)
    {
        string result = template;
        foreach (var kvp in values)
        {
            string valueStr = kvp.Value?.ToString() ?? "null";
            result = result.Replace($"{{{kvp.Key}}}", valueStr);
        }
        return (result != template) ? result : defaultResult;
    }

    public List<object> GetDataSource(
        string sourceName,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests)
    {
        List<object> result = new List<object>();

        if (sourceName == "alive_npcs")
        {
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i].IsAlive)
                    result.Add(npcs[i]);
            }
        }
        else if (sourceName == "resources_all")
        {
            for (int i = 0; i < resources.Count; i++)
            {
                result.Add(resources[i]);
            }
        }
        else if (sourceName == "resources_food")
        {
            // Data-driven: include any resource with food or water restore value in catalog
            for (int i = 0; i < resources.Count; i++)
            {
                if (_catalog.TryGetValue(resources[i].Name, out var entry) &&
                    (entry.FoodRestore > 0 || entry.WaterRestore > 0))
                    result.Add(resources[i]);
                else if (!_catalog.ContainsKey(resources[i].Name) &&
                    (resources[i].Category == "Еда" || resources[i].Category == "Вода"))
                    result.Add(resources[i]); // fallback: match by category if not in catalog
            }
        }
        else if (sourceName == "available_techniques")
        {
            List<Technique> techniques = _db.GetTechniquesByAltarLevel(player.AltarLevel);
            for (int i = 0; i < techniques.Count; i++)
            {
                result.Add(techniques[i]);
            }
        }

        return result;
    }
}
