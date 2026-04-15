using System;

namespace ApocMinimal.Models.StatisticsData
{
    /// <summary>
    /// Базовый класс модификатора
    /// </summary>
    public abstract class Modifier
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }      // Источник (алтарь, техника, предмет)
        public ModifierType Type { get; set; }   // Аддитивный или мультипликативный
        public double Value { get; set; }        // Значение (+20 или ×1.5)

        protected Modifier(string id, string name, string source, ModifierType type, double value)
        {
            Id = id;
            Name = name;
            Source = source;
            Type = type;
            Value = value;
        }

        public abstract bool IsActive();
    }

    /// <summary>
    /// Постоянный модификатор (удаляется только игроком)
    /// </summary>
    public class PermanentModifier : Modifier
    {
        public bool IsActiveFlag { get; set; } = true;

        public PermanentModifier(string id, string name, string source, ModifierType type, double value)
            : base(id, name, source, type, value) { }

        public override bool IsActive() => IsActiveFlag;

        public void Deactivate() => IsActiveFlag = false;
        public void Activate() => IsActiveFlag = true;
    }

    /// <summary>
    /// Временный модификатор
    /// </summary>
    public abstract class TemporaryModifier : Modifier
    {
        public TemporaryModifierType TemporaryType { get; set; }

        protected TemporaryModifier(string id, string name, string source, ModifierType type, double value, TemporaryModifierType tempType)
            : base(id, name, source, type, value)
        {
            TemporaryType = tempType;
        }
    }

    /// <summary>
    /// Зависимый временный модификатор (работает пока есть условие)
    /// </summary>
    public class DependentModifier : TemporaryModifier
    {
        public Condition Condition { get; set; }  // Условие активации (проверяется по финальным значениям)
        private bool _isConditionMet = false;

        public DependentModifier(string id, string name, string source, ModifierType type, double value, Condition condition)
            : base(id, name, source, type, value, TemporaryModifierType.Dependent)
        {
            Condition = condition;
        }

        public override bool IsActive() => _isConditionMet;

        public void UpdateCondition(Dictionary<string, int> statFinalValues)
        {
            _isConditionMet = Condition?.Check(statFinalValues) ?? true;
        }
    }

    /// <summary>
    /// Независимый временный модификатор (работает определённое время)
    /// </summary>
    public class IndependentModifier : TemporaryModifier
    {
        public TimeUnit TimeUnit { get; set; }
        public int Duration { get; set; }        // Общая длительность
        public int Remaining { get; set; }       // Оставшееся время

        public IndependentModifier(string id, string name, string source, ModifierType type, double value,
                                   TimeUnit timeUnit, int duration)
            : base(id, name, source, type, value, TemporaryModifierType.Independent)
        {
            TimeUnit = timeUnit;
            Duration = duration;
            Remaining = duration;
        }

        public override bool IsActive() => Remaining > 0;

        public void Tick(TimeUnit unit)
        {
            if (TimeUnit == unit && Remaining > 0)
            {
                Remaining--;
            }
        }

        public void TickCombat() => Tick(TimeUnit.CombatTurns);
        public void TickHour() => Tick(TimeUnit.Hours);
        public void TickDay() => Tick(TimeUnit.Days);
    }
}