using System.Collections.Generic;

namespace ApocMinimal.Models.GameActions;

/// <summary>
/// Група дій (для першого рівня комбобокса)
/// </summary>
public class ActionGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Дія гравця
/// </summary>
public class GameActionDb
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string ActionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HandlerMethod { get; set; } = string.Empty;
    public bool ConsumesAction { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    // Навігаційні властивості
    public ActionGroup? Group { get; set; }
    public List<ActionParam> Parameters { get; set; } = new();
    public List<HandlerParamMapping> ParamMappings { get; set; } = new();
    public ResultTemplate? ResultTemplate { get; set; }
}