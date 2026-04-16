using ApocMinimal.Models.StatisticsData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApocMinimal.Models.StatisticsData
{
    public class Characteristic
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public StatType Type { get; set; }
        public int StatNumber { get; set; } // 1-30
        public bool IsCombat { get; set; }
        public bool IsSocial { get; set; }
        public int SortOrder { get; set; }  // порядок внутри типа (Physical 1-10, Mental 1-12, Energy 1-8)

        private int _baseValue;
        public int BaseValue
        {
            get => _baseValue;
            set => _baseValue = Math.Max(0, value);
        }

        private int _deviation;
        public int Deviation
        {
            get => _deviation;
            set
            {
                if (Type == StatType.Energy)
                    _deviation = Math.Clamp(value, -100, 100);
                else
                    _deviation = Math.Clamp(value, -BaseValue, BaseValue);
            }
        }

        public int FullBase => BaseValue + Deviation;

        private List<Modifier> _modifiers = new List<Modifier>();

        public int FinalValue
        {
            get
            {
                double multiplicative = 1.0;
                double additive = 0.0;

                foreach (var mod in _modifiers.Where(m => m.IsActive()))
                {
                    if (mod.Type == ModifierType.Multiplicative)
                        multiplicative *= mod.Value;
                }

                double result = FullBase * multiplicative;

                foreach (var mod in _modifiers.Where(m => m.IsActive()))
                {
                    if (mod.Type == ModifierType.Additive)
                        additive += mod.Value;
                }

                result += additive;
                return (int)Math.Max(0, Math.Round(result));
            }
        }

        public Characteristic(string id, string name, StatType type, int statNumber, int baseValue = 100)
        {
            Id = id;
            Name = name;
            Type = type;
            StatNumber = statNumber;
            _baseValue = baseValue;
            _deviation = 0;
        }

        public void AddDeviation(int delta) => Deviation += delta;
        public void SetDeviation(int newDeviation) => Deviation = newDeviation;
        public void ResetDeviation() => _deviation = 0;

        public void AddModifier(Modifier modifier) => _modifiers.Add(modifier);
        public bool RemoveModifier(string modifierId) => _modifiers.RemoveAll(m => m.Id == modifierId) > 0;
        public void RemoveModifiersFromSource(string source) => _modifiers.RemoveAll(m => m.Source == source);
        public List<T> GetModifiersByType<T>() where T : Modifier
        {
            List<T> result = new List<T>();
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i] is T typedModifier)
                {
                    result.Add(typedModifier);
                }
            }
            return result;
        }
        public void ClearModifiers() => _modifiers.Clear();

        public void UpdateDependentModifiers(Dictionary<string, int> statFinalValues)
        {
            foreach (var mod in _modifiers.OfType<DependentModifier>())
                mod.UpdateCondition(statFinalValues);
        }

        public void TickHour()
        {
            foreach (var mod in _modifiers.OfType<IndependentModifier>()) mod.TickHour();
            _modifiers.RemoveAll(m => m is IndependentModifier ind && !ind.IsActive());
        }

        public void TickDay()
        {
            foreach (var mod in _modifiers.OfType<IndependentModifier>()) mod.TickDay();
            _modifiers.RemoveAll(m => m is IndependentModifier ind && !ind.IsActive());
        }

        public void TickCombat()
        {
            foreach (var mod in _modifiers.OfType<IndependentModifier>()) mod.TickCombat();
            _modifiers.RemoveAll(m => m is IndependentModifier ind && !ind.IsActive());
        }

        public double GetGrowthFactor() => 100.0 / (FinalValue + 100);

        public override string ToString() => $"{Name}: Base={BaseValue}, Dev={Deviation:+0;-0;0}, FullBase={FullBase}, Final={FinalValue}";
    }
}
