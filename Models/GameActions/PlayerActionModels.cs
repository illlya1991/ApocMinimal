// PlayerActionModels.cs
using System.Collections.Generic;

namespace ApocMinimal.Models.GameActions
{
    /// <summary>
    /// Группа действий игрока (для первого уровня комбобокса)
    /// </summary>
    public class PlayerActionGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Действие игрока из базы данных
    /// </summary>
    public class PlayerGameAction
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

        // Навигационные свойства
        public PlayerActionGroup? Group { get; set; }
        public List<PlayerActionParam> Parameters { get; set; } = new();
        public List<PlayerHandlerParamMapping> ParamMappings { get; set; } = new();
        public PlayerResultTemplate? ResultTemplate { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}