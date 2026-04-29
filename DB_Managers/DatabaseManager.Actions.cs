using System.Data.SQLite;
using ApocMinimal.Models.GameActions;

namespace ApocMinimal.Database;

public partial class DatabaseManager
{
    public List<PlayerActionGroup> GetPlayerActionGroups()
    {
        var groups = new List<PlayerActionGroup>();
        try
        {
            using var cmd = new SQLiteCommand(
                "SELECT Id, Name, Icon, DisplayOrder, IsActive FROM ActionGroups WHERE IsActive = 1 ORDER BY DisplayOrder", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                groups.Add(new PlayerActionGroup
                {
                    Id           = Convert.ToInt32(reader["Id"]),
                    Name         = reader["Name"]?.ToString() ?? "",
                    Icon         = reader["Icon"]?.ToString() ?? "",
                    DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]),
                    IsActive     = Convert.ToBoolean(reader["IsActive"]),
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerActionGroups error: {ex.Message}"); }
        return groups;
    }

    public List<PlayerGameAction> GetAllPlayerGameActions()
    {
        var actions = new List<PlayerGameAction>();
        try
        {
            using var cmd = new SQLiteCommand(@"SELECT Id, GroupId, ActionKey, DisplayName, Description,
    HandlerMethod, ConsumesAction, DisplayOrder, IsActive
    FROM GameActions WHERE IsActive = 1 ORDER BY GroupId, DisplayOrder", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                actions.Add(new PlayerGameAction
                {
                    Id            = Convert.ToInt32(reader["Id"]),
                    GroupId       = Convert.ToInt32(reader["GroupId"]),
                    ActionKey     = reader["ActionKey"]?.ToString() ?? "",
                    DisplayName   = reader["DisplayName"]?.ToString() ?? "",
                    Description   = reader["Description"]?.ToString() ?? "",
                    HandlerMethod = reader["HandlerMethod"]?.ToString() ?? "",
                    ConsumesAction = Convert.ToBoolean(reader["ConsumesAction"]),
                    DisplayOrder  = Convert.ToInt32(reader["DisplayOrder"]),
                    IsActive      = Convert.ToBoolean(reader["IsActive"]),
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetAllPlayerGameActions error: {ex.Message}"); }
        return actions;
    }

    public List<PlayerActionParam> GetPlayerActionParams(int actionId)
    {
        var parameters = new List<PlayerActionParam>();
        try
        {
            using var cmd = new SQLiteCommand(@"SELECT Id, ActionId, ParamTypeId, ParamKey, DisplayName,
    OrderIndex, IsRequired, FilterCondition, DataSource, ValidationRules, DefaultValue
    FROM ActionParams WHERE ActionId = @actionId ORDER BY OrderIndex", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                parameters.Add(new PlayerActionParam
                {
                    Id               = Convert.ToInt32(reader["Id"]),
                    ActionId         = Convert.ToInt32(reader["ActionId"]),
                    ParamTypeId      = Convert.ToInt32(reader["ParamTypeId"]),
                    ParamKey         = reader["ParamKey"]?.ToString() ?? "",
                    DisplayName      = reader["DisplayName"]?.ToString() ?? "",
                    OrderIndex       = Convert.ToInt32(reader["OrderIndex"]),
                    IsRequired       = Convert.ToBoolean(reader["IsRequired"]),
                    FilterCondition  = reader["FilterCondition"]?.ToString() ?? "",
                    DataSource       = reader["DataSource"]?.ToString() ?? "",
                    ValidationRules  = reader["ValidationRules"]?.ToString() ?? "",
                    DefaultValue     = reader["DefaultValue"]?.ToString() ?? "",
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerActionParams error: {ex.Message}"); }
        return parameters;
    }

    public List<PlayerHandlerParamMapping> GetPlayerHandlerParamMappings(int actionId)
    {
        var mappings = new List<PlayerHandlerParamMapping>();
        try
        {
            using var cmd = new SQLiteCommand(
                "SELECT Id, ActionId, HandlerId, HandlerParamName, ActionParamKey FROM HandlerParamMapping WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                mappings.Add(new PlayerHandlerParamMapping
                {
                    Id              = Convert.ToInt32(reader["Id"]),
                    ActionId        = Convert.ToInt32(reader["ActionId"]),
                    HandlerId       = Convert.ToInt32(reader["HandlerId"]),
                    HandlerParamName = reader["HandlerParamName"]?.ToString() ?? "",
                    ActionParamKey  = reader["ActionParamKey"]?.ToString() ?? "",
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerHandlerParamMappings error: {ex.Message}"); }
        return mappings;
    }

    public PlayerResultTemplate? GetPlayerResultTemplate(int actionId)
    {
        try
        {
            using var cmd = new SQLiteCommand(
                "SELECT Id, ActionId, SuccessTemplate, FailTemplate, Color FROM ResultTemplates WHERE ActionId = @actionId", _conn);
            cmd.Parameters.AddWithValue("@actionId", actionId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new PlayerResultTemplate
                {
                    Id              = Convert.ToInt32(reader["Id"]),
                    ActionId        = Convert.ToInt32(reader["ActionId"]),
                    SuccessTemplate = reader["SuccessTemplate"]?.ToString() ?? "",
                    FailTemplate    = reader["FailTemplate"]?.ToString() ?? "",
                    Color           = reader["Color"]?.ToString() ?? "normal",
                };
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerResultTemplate error: {ex.Message}"); }
        return null;
    }

    public List<PlayerParamType> GetPlayerParamTypes()
    {
        var types = new List<PlayerParamType>();
        try
        {
            using var cmd = new SQLiteCommand("SELECT Id, Name, ControlType, ValueType, IsList FROM ParamTypes", _conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                types.Add(new PlayerParamType
                {
                    Id          = Convert.ToInt32(reader["Id"]),
                    Name        = reader["Name"]?.ToString() ?? "",
                    ControlType = reader["ControlType"]?.ToString() ?? "",
                    ValueType   = reader["ValueType"]?.ToString() ?? "",
                    IsList      = Convert.ToBoolean(reader["IsList"]),
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetPlayerParamTypes error: {ex.Message}"); }
        return types;
    }
}
