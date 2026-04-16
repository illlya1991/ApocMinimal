// PlayerActionParamModels.cs
using System.Collections.Generic;

namespace ApocMinimal.Models.GameActions
{
    /// <summary>
    /// Тип параметра действия игрока
    /// </summary>
    public class PlayerParamType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ControlType { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public bool IsList { get; set; }
    }

    /// <summary>
    /// Параметр действия игрока
    /// </summary>
    public class PlayerActionParam
    {
        public int Id { get; set; }
        public int ActionId { get; set; }
        public int ParamTypeId { get; set; }
        public string ParamKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; } = true;
        public string FilterCondition { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public string ValidationRules { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;

        public PlayerParamType? ParamType { get; set; }
    }

    /// <summary>
    /// Обработчик действия игрока
    /// </summary>
    public class PlayerHandler
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Маппинг параметров действия игрока к обработчику
    /// </summary>
    public class PlayerHandlerParamMapping
    {
        public int Id { get; set; }
        public int ActionId { get; set; }
        public int HandlerId { get; set; }
        public string HandlerParamName { get; set; } = string.Empty;
        public string ActionParamKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Шаблон результата действия игрока
    /// </summary>
    public class PlayerResultTemplate
    {
        public int Id { get; set; }
        public int ActionId { get; set; }
        public string SuccessTemplate { get; set; } = string.Empty;
        public string FailTemplate { get; set; } = string.Empty;
        public string Color { get; set; } = "success";
    }
}