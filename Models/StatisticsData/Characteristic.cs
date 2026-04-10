using System;
using System.Collections.Generic;
using System.Linq;

namespace ApocalypseSimulation.Models.StatisticsData
{
    /// <summary>
    /// Характеристика NPC
    /// Значения — целые числа
    /// </summary>
    public class Characteristic
    {
        // === ИДЕНТИФИКАЦИЯ ===
        public string Id { get; set; }
        public string Name { get; set; }
        public StatType Type { get; set; }

        // === БАЗОВЫЕ ЗНАЧЕНИЯ (до апокалипсиса) ===
        // Меняется только алтарём, очень редко
        private int _baseValue;
        public int BaseValue
        {
            get => _baseValue;
            set => _baseValue = Math.Max(0, value);
        }

        // === ТЕКУЩЕЕ ОТКЛОНЕНИЕ ===
        // Для физических и ментальных: от -BaseValue до +BaseValue
        // Для энергетических: от -100 до +100 (база всегда 100)
        private int _deviation;
        public int Deviation
        {
            get => _deviation;
            set
            {
                if (Type == StatType.Energy)
                {
                    // Энергетические: отклонение от -100 до +100
                    _deviation = Math.Clamp(value, -100, 100);
                }
                else
                {
                    // Физические и ментальные: отклонение от -BaseValue до +BaseValue
                    _deviation = Math.Clamp(value, -BaseValue, BaseValue);
                }
            }
        }

        // === ПОЛНАЯ БАЗА (BaseValue + Deviation) ===
        public int FullBase => BaseValue + Deviation;

        // === МОДИФИКАТОРЫ ===
        private List<Modifier> _modifiers = new List<Modifier>();

        // === ФИНАЛЬНОЕ ЗНАЧЕНИЕ (целое число) ===
        public int FinalValue
        {
            get
            {
                // Сначала применяем все мультипликаторы к полной базе
                double multiplicative = 1.0;
                double additive = 0.0;

                foreach (var mod in _modifiers.Where(m => m.IsActive()))
                {
                    if (mod.Type == ModifierType.Multiplicative)
                        multiplicative *= mod.Value;
                }

                double result = FullBase * multiplicative;

                // Потом применяем аддитивные
                foreach (var mod in _modifiers.Where(m => m.IsActive()))
                {
                    if (mod.Type == ModifierType.Additive)
                        additive += mod.Value;
                }

                result += additive;
                return (int)Math.Max(0, Math.Round(result));
            }
        }

        // === КОНСТРУКТОР ===
        public Characteristic(string id, string name, StatType type, int baseValue = 100)
        {
            Id = id;
            Name = name;
            Type = type;
            _baseValue = baseValue;
            _deviation = 0;
        }

        // === РАБОТА С ОТКЛОНЕНИЯМИ ===
        public void AddDeviation(int delta)
        {
            Deviation += delta;
        }

        public void SetDeviation(int newDeviation)
        {
            Deviation = newDeviation;
        }

        public void ResetDeviation()
        {
            _deviation = 0;
        }

        // === РАБОТА С МОДИФИКАТОРАМИ ===
        public void AddModifier(Modifier modifier)
        {
            _modifiers.Add(modifier);
        }

        public bool RemoveModifier(string modifierId)
        {
            return _modifiers.RemoveAll(m => m.Id == modifierId) > 0;
        }

        public void RemoveModifiersFromSource(string source)
        {
            _modifiers.RemoveAll(m => m.Source == source);
        }

        public List<T> GetModifiersByType<T>() where T : Modifier
        {
            return _modifiers.OfType<T>().ToList();
        }

        public void ClearModifiers()
        {
            _modifiers.Clear();
        }

        // === ОБНОВЛЕНИЕ ЗАВИСИМЫХ МОДИФИКАТОРОВ ===
        public void UpdateDependentModifiers(Dictionary<string, int> statFinalValues)
        {
            foreach (var mod in _modifiers.OfType<DependentModifier>())
            {
                mod.UpdateCondition(statFinalValues);
            }
        }

        // === ОБНОВЛЕНИЕ ВРЕМЕННЫХ НЕЗАВИСИМЫХ МОДИФИКАТОРОВ ===
        public void TickHour()
        {
            foreach (var mod in _modifiers.OfType<IndependentModifier>())
            {
                mod.TickHour();
            }
            _modifiers.RemoveAll(m => m is IndependentModifier ind && !ind.IsActive());
        }

        public void TickDay()
        {
            foreach (var mod in _modifiers.OfType<IndependentModifier>())
            {
                mod.TickDay();
            }
            _modifiers.RemoveAll(m => m is IndependentModifier ind && !ind.IsActive());
        }

        public void TickCombat()
        {
            foreach (var mod in _modifiers.OfType<IndependentModifier>())
            {
                mod.TickCombat();
            }
            _modifiers.RemoveAll(m => m is IndependentModifier ind && !ind.IsActive());
        }

        // === ФОРМУЛА РОСТА ===
        public double GetGrowthFactor()
        {
            // Для формулы роста: base * (100/(stat+100))
            return 100.0 / (FinalValue + 100);
        }

        // === ДЛЯ ОТЛАДКИ ===
        public override string ToString()
        {
            return $"{Name}: Base={BaseValue}, Dev={Deviation:+0;-0;0}, FullBase={FullBase}, Final={FinalValue}";
        }
    }
}