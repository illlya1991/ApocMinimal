using System.Collections.Generic;

namespace ApocMinimal.Models.GameActions;

/// <summary>
/// Тип параметра
/// </summary>
public class ParamType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;  // 'Npc', 'Resource', 'Location', 'Quest', 'Technique', 'Text', 'Number'
    public string ControlType { get; set; } = string.Empty;  // 'ComboBox', 'TextBox', 'Slider', 'CheckBox'
    public string ValueType { get; set; } = string.Empty;  // 'Npc', 'Resource', 'double', 'string', 'int'
    public bool IsList { get; set; }
}

/// <summary>
/// Параметр дії
/// </summary>
public class ActionParam
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public int ParamTypeId { get; set; }
    public string ParamKey { get; set; } = string.Empty;  // ім'я змінної
    public string DisplayName { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public bool IsRequired { get; set; } = true;

    /// <summary>JSON фільтр: {"IsAlive": true, "MinTrust": 50, "Category": "Базовий"}</summary>
    public string FilterCondition { get; set; } = string.Empty;

    /// <summary>Джерело даних: 'alive_npcs', 'resources_food', 'resources_all', 'locations_explored'</summary>
    public string DataSource { get; set; } = string.Empty;

    /// <summary>JSON валідації: {"Min": 1, "Max": 100, "Step": 1, "IsInteger": true}</summary>
    public string ValidationRules { get; set; } = string.Empty;

    public string DefaultValue { get; set; } = string.Empty;

    // Навігаційні властивості
    public ParamType? ParamType { get; set; }
}

/// <summary>
/// Обробник
/// </summary>
public class Handler
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Маппінг параметрів дії до обробника
/// </summary>
public class HandlerParamMapping
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public int HandlerId { get; set; }
    public string HandlerParamName { get; set; } = string.Empty;
    public string ActionParamKey { get; set; } = string.Empty;
}

/// <summary>
/// Шаблон результату
/// </summary>
public class ResultTemplate
{
    public int Id { get; set; }
    public int ActionId { get; set; }
    public string SuccessTemplate { get; set; } = string.Empty;
    public string FailTemplate { get; set; } = string.Empty;
    public string Color { get; set; } = "success";  // 'success', 'warning', 'danger', 'normal'
}