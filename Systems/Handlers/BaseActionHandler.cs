using System;
using System.Collections.Generic;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.PlayerData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems.Handlers;

/// <summary>
/// Базовий клас для всіх обробників дій
/// </summary>
public abstract class BaseActionHandler
{
    protected Database.DatabaseManager _db;
    protected Random _rnd;

    public BaseActionHandler(Database.DatabaseManager db, Random rnd)
    {
        _db = db;
        _rnd = rnd;
    }

    /// <summary>
    /// Виконати дію
    /// </summary>
    /// <param name="parameters">Словник параметрів (ключ = ParamKey, значення = обране значення)</param>
    /// <param name="player">Гравець</param>
    /// <param name="npcs">Список NPC</param>
    /// <param name="resources">Список ресурсів</param>
    /// <param name="quests">Список квестів</param>
    /// <returns>Результат виконання (текст для логу)</returns>
    public abstract string Execute(
        Dictionary<string, object> parameters,
        Player player,
        List<Npc> npcs,
        List<Resource> resources,
        List<Quest> quests);

    /// <summary>
    /// Отримати NPC за ID
    /// </summary>
    protected Npc? GetNpcById(List<Npc> npcs, int id)
    {
        return npcs.FirstOrDefault(n => n.Id == id);
    }

    /// <summary>
    /// Отримати ресурс за назвою
    /// </summary>
    protected Resource? GetResourceByName(List<Resource> resources, string name)
    {
        return resources.FirstOrDefault(r => r.Name == name);
    }

    /// <summary>
    /// Форматувати шаблон з підстановкою значень
    /// </summary>
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