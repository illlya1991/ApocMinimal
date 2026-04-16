// ActionManager.cs - оптимизированная версия

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

    private List<ActionGroup> _groups = new List<ActionGroup>();
    private List<GameActionDb> _actions = new List<GameActionDb>();
    private List<HandlerEntry> _handlers = new List<HandlerEntry>();

    private class HandlerEntry
    {
        public string Name { get; set; } = "";
        public BaseActionHandler Handler { get; set; } = null!;
    }

    public ActionManager(DatabaseManager db, Random rnd, Action<string, string> logAction)
    {
        _db = db;
        _rnd = rnd;
        LoadActions();
        InitializeHandlers(logAction);
    }

    private void LoadActions()
    {
        _groups = _db.GetActionGroups();
        _actions = _db.GetAllGameActions();

        List<ParamType> paramTypes = _db.GetParamTypes();

        for (int i = 0; i < _actions.Count; i++)
        {
            GameActionDb action = _actions[i];
            action.Parameters = _db.GetActionParams(action.Id);
            action.ParamMappings = _db.GetHandlerParamMappings(action.Id);
            action.ResultTemplate = _db.GetResultTemplate(action.Id);

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
                ActionParam param = action.Parameters[j];
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

    private void InitializeHandlers(Action<string, string> logAction)
    {
        _handlers.Add(new HandlerEntry { Name = "InfoHandler", Handler = new InfoHandler(_db, _rnd, logAction) });
        _handlers.Add(new HandlerEntry { Name = "InteractionHandler", Handler = new InteractionHandler(_db, _rnd, logAction) });
        _handlers.Add(new HandlerEntry { Name = "ResourceHandler", Handler = new ResourceHandler(_db, _rnd, logAction) });
        _handlers.Add(new HandlerEntry { Name = "QuestHandler", Handler = new QuestHandler(_db, _rnd, logAction) });
        _handlers.Add(new HandlerEntry { Name = "TechniqueHandler", Handler = new TechniqueHandler(_db, _rnd, logAction) });
        _handlers.Add(new HandlerEntry { Name = "ManagementHandler", Handler = new ManagementHandler(_db, _rnd, logAction) });
    }

    public List<ActionGroup> GetGroups()
    {
        return _groups;
    }

    public List<GameActionDb> GetActionsByGroup(int groupId)
    {
        List<GameActionDb> result = new List<GameActionDb>();
        for (int i = 0; i < _actions.Count; i++)
        {
            if (_actions[i].GroupId == groupId)
                result.Add(_actions[i]);
        }
        return result;
    }

    public List<GameActionDb> GetAllActions()
    {
        return _actions;
    }

    public GameActionDb? GetActionByKey(string key)
    {
        for (int i = 0; i < _actions.Count; i++)
        {
            if (_actions[i].ActionKey == key)
                return _actions[i];
        }
        return null;
    }

    public string ExecuteAction(
        GameActionDb action,
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
            return $"Обробник '{action.HandlerMethod}' не знайдено";

        Dictionary<string, object> extendedParams = new Dictionary<string, object>();

        // Копируем вручную без LINQ
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
            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Name == "Еда" || resources[i].Name == "Вода")
                    result.Add(resources[i]);
            }
        }
        else if (sourceName == "available_quests")
        {
            for (int i = 0; i < quests.Count; i++)
            {
                if (quests[i].Status == QuestStatus.Available)
                    result.Add(quests[i]);
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