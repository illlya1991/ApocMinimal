using System;
using System.Collections.Generic;
using System.Linq;
using ApocMinimal.Database;
using ApocMinimal.Models.GameActions;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;
using ApocMinimal.Systems.Handlers;

namespace ApocMinimal.Systems;

/// <summary>
/// Менеджер дій гравця
/// </summary>
public class ActionManager
{
    private readonly DatabaseManager _db;
    private readonly Random _rnd;

    private List<ActionGroup> _groups = new();
    private List<GameActionDb> _actions = new();
    private Dictionary<string, GameActionDb> _actionMap = new();
    private Dictionary<string, BaseActionHandler> _handlers = new();

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
        _actionMap = _actions.ToDictionary(a => a.ActionKey, a => a);

        foreach (var action in _actions)
        {
            action.Parameters = _db.GetActionParams(action.Id);
            action.ParamMappings = _db.GetHandlerParamMappings(action.Id);
            action.ResultTemplate = _db.GetResultTemplate(action.Id);
            action.Group = _groups.FirstOrDefault(g => g.Id == action.GroupId);

            // Завантажуємо типи параметрів
            var paramTypes = _db.GetParamTypes();
            foreach (var param in action.Parameters)
            {
                param.ParamType = paramTypes.FirstOrDefault(pt => pt.Id == param.ParamTypeId);
            }
        }
    }

    private void InitializeHandlers(Action<string, string> logAction)
    {
        // Группа Информация
        _handlers["ViewInfoHandler"] = new ViewInfoHandler(_db, _rnd, logAction);

        // Группа Взаимодействие - один универсальный хендлер
        _handlers["InteractionHandler"] = new InteractionHandler(_db, _rnd, logAction);

        // Группа Ресурсы
        _handlers["TransferResourceHandler"] = new TransferResourceHandler(_db, _rnd, logAction);
        _handlers["DemandResourceHandler"] = new DemandResourceHandler(_db, _rnd, logAction);

        // Группа Квесты
        _handlers["GiveQuestHandler"] = new GiveQuestHandler(_db, _rnd, logAction);
        _handlers["CompleteQuestHandler"] = new CompleteQuestHandler(_db, _rnd, logAction);
        _handlers["AssignPublicQuestHandler"] = new AssignPublicQuestHandler(_db, _rnd, logAction);

        // Группа Техники
        _handlers["TeachTechniqueHandler"] = new TeachTechniqueHandler(_db, _rnd, logAction);

        // Группа Управление
        _handlers["RewardNpcHandler"] = new RewardNpcHandler(_db, _rnd, logAction);
        _handlers["PunishNpcHandler"] = new PunishNpcHandler(_db, _rnd, logAction);

        // Заглушка
        _handlers["EmptyHandler"] = new EmptyHandler(_db, _rnd, logAction);
    }

    /// <summary>
    /// Отримати групи дій (для першого комбобокса)
    /// </summary>
    public List<ActionGroup> GetGroups() => _groups;

    /// <summary>
    /// Отримати дії в групі
    /// </summary>
    public List<GameActionDb> GetActionsByGroup(int groupId)
    {
        return _actions.Where(a => a.GroupId == groupId).ToList();
    }

    /// <summary>
    /// Отримати всі дії
    /// </summary>
    public List<GameActionDb> GetAllActions() => _actions;

    /// <summary>
    /// Отримати дію за ключем
    /// </summary>
    public GameActionDb? GetActionByKey(string key)
    {
        return _actionMap.GetValueOrDefault(key);
    }

    /// <summary>
    /// Виконати дію
    /// </summary>
    public string ExecuteAction(
    GameActionDb action,
    Dictionary<string, object> parameterValues,
    Player player,
    List<Npc> npcs,
    List<Resource> resources,
    List<Quest> quests)
    {
        // Получаем обработчик
        if (!_handlers.ContainsKey(action.HandlerMethod))
            return $"Обробник '{action.HandlerMethod}' не знайдено";

        var handler = _handlers[action.HandlerMethod];

        // Добавляем ActionKey в параметры для универсальных хендлеров
        var extendedParams = new Dictionary<string, object>(parameterValues);
        extendedParams["_actionKey"] = action.ActionKey;

        // Выполняем
        var result = handler.Execute(extendedParams, player, npcs, resources, quests);

        // Применяем шаблон если есть
        if (action.ResultTemplate != null && !string.IsNullOrEmpty(action.ResultTemplate.SuccessTemplate))
        {
            result = FormatWithTemplate(action.ResultTemplate.SuccessTemplate, parameterValues, result);
        }

        return result;
    }

    private string FormatWithTemplate(string template, Dictionary<string, object> values, string defaultResult)
    {
        var result = template;
        foreach (var kvp in values)
        {
            var valueStr = kvp.Value?.ToString() ?? "null";
            result = result.Replace($"{{{kvp.Key}}}", valueStr);
        }
        return result != template ? result : defaultResult;
    }
}