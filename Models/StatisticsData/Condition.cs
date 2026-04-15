using System.Collections.Generic;
using System.Linq;

namespace ApocMinimal.Models.StatisticsData
{
    /// <summary>
    /// Условие, состоящее из нескольких базовых условий характеристик
    /// Проверяется только по финальным значениям
    /// </summary>
    public class Condition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<StatCondition> StatConditions { get; set; } = new List<StatCondition>();
        public ConditionOperator Operator { get; set; } = ConditionOperator.AND;

        public Condition(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public void AddCondition(StatCondition condition)
        {
            StatConditions.Add(condition);
        }

        public bool Check(Dictionary<string, int> statFinalValues)
        {
            if (Operator == ConditionOperator.AND)
            {
                return StatConditions.All(c =>
                {
                    if (statFinalValues.ContainsKey(c.StatId))
                        return c.Check(statFinalValues[c.StatId]);
                    return false;
                });
            }
            else // OR
            {
                return StatConditions.Any(c =>
                {
                    if (statFinalValues.ContainsKey(c.StatId))
                        return c.Check(statFinalValues[c.StatId]);
                    return false;
                });
            }
        }

        public override string ToString()
        {
            return $"{Name} ({Id}): {string.Join($" {(Operator == ConditionOperator.AND ? "AND" : "OR")} ", StatConditions)}";
        }
    }

    /// <summary>
    /// Базовое условие для проверки значения характеристики
    /// </summary>
    public class StatCondition
    {
        public string StatId { get; set; }           // ID характеристики
        public int MinValue { get; set; } = 0;       // Минимальное значение (включительно)
        public int MaxValue { get; set; } = int.MaxValue; // Максимальное значение (включительно)

        public StatCondition(string statId, int minValue = 0, int maxValue = int.MaxValue)
        {
            StatId = statId;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public bool Check(int statValue)
        {
            return statValue >= MinValue && statValue <= MaxValue;
        }

        public override string ToString()
        {
            return $"{StatId}: {MinValue} <= value <= {MaxValue}";
        }
    }
}