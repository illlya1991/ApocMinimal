using System;
using System.Collections.Generic;
using System.Linq;

namespace ApocalypseSimulation.Models.StatisticsData
{
    /// <summary>
    /// Контейнер всех 30 характеристик NPC
    /// </summary>
    public class Statistics
    {
        // === ФИЗИЧЕСКИЕ (10) ===
        public Characteristic Endurance { get; private set; }      // Выносливость
        public Characteristic Toughness { get; private set; }      // Стойкость
        public Characteristic Strength { get; private set; }       // Сила
        public Characteristic RecoveryPhys { get; private set; }   // Восстановление (физ)
        public Characteristic Reflexes { get; private set; }       // Рефлексы
        public Characteristic Agility { get; private set; }        // Ловкость
        public Characteristic Adaptation { get; private set; }     // Адаптация
        public Characteristic Regeneration { get; private set; }   // Регенерация
        public Characteristic Sensorics { get; private set; }      // Сенсорика
        public Characteristic Longevity { get; private set; }      // Долголетие

        // === МЕНТАЛЬНЫЕ (12) ===
        public Characteristic Focus { get; private set; }          // Фокус
        public Characteristic Memory { get; private set; }         // Память
        public Characteristic Logic { get; private set; }          // Логика
        public Characteristic Deduction { get; private set; }      // Дедукция
        public Characteristic Intelligence { get; private set; }   // Интеллект
        public Characteristic Will { get; private set; }           // Воля
        public Characteristic Learning { get; private set; }       // Обучение
        public Characteristic Flexibility { get; private set; }    // Гибкость
        public Characteristic Intuition { get; private set; }      // Интуиция
        public Characteristic SocialIntel { get; private set; }    // Социальный интеллект
        public Characteristic Creativity { get; private set; }     // Творчество
        public Characteristic Mathematics { get; private set; }    // Математика

        // === ЭНЕРГЕТИЧЕСКИЕ (8) ===
        // Базовое значение = 100, отклонение от -100 до 100
        public Characteristic EnergyReserve { get; private set; }  // Запас энергии
        public Characteristic EnergyRecovery { get; private set; } // Восстановление (энерг)
        public Characteristic Control { get; private set; }        // Контроль
        public Characteristic Concentration { get; private set; }  // Концентрация
        public Characteristic Output { get; private set; }         // Выход
        public Characteristic Precision { get; private set; }      // Тонкость
        public Characteristic EnergyResist { get; private set; }   // Устойчивость
        public Characteristic EnergySense { get; private set; }    // Восприятие (энерг)

        // === СПИСОК ВСЕХ ХАРАКТЕРИСТИК ===
        public List<Characteristic> AllStats { get; private set; }

        // === КОНСТРУКТОР ===
        public Statistics(int defaultBaseValue = 100)
        {
            // Физические (база может варьироваться 70-120 для взрослых)
            Endurance = new Characteristic("endurance", "Выносливость", StatType.Physical, defaultBaseValue);
            Toughness = new Characteristic("toughness", "Стойкость", StatType.Physical, defaultBaseValue);
            Strength = new Characteristic("strength", "Сила", StatType.Physical, defaultBaseValue);
            RecoveryPhys = new Characteristic("recovery_phys", "Восстановление (физ)", StatType.Physical, defaultBaseValue);
            Reflexes = new Characteristic("reflexes", "Рефлексы", StatType.Physical, defaultBaseValue);
            Agility = new Characteristic("agility", "Ловкость", StatType.Physical, defaultBaseValue);
            Adaptation = new Characteristic("adaptation", "Адаптация", StatType.Physical, defaultBaseValue);
            Regeneration = new Characteristic("regeneration", "Регенерация", StatType.Physical, defaultBaseValue);
            Sensorics = new Characteristic("sensorics", "Сенсорика", StatType.Physical, defaultBaseValue);
            Longevity = new Characteristic("longevity", "Долголетие", StatType.Physical, defaultBaseValue);

            // Ментальные (база может варьироваться 70-120 для взрослых)
            Focus = new Characteristic("focus", "Фокус", StatType.Mental, defaultBaseValue);
            Memory = new Characteristic("memory", "Память", StatType.Mental, defaultBaseValue);
            Logic = new Characteristic("logic", "Логика", StatType.Mental, defaultBaseValue);
            Deduction = new Characteristic("deduction", "Дедукция", StatType.Mental, defaultBaseValue);
            Intelligence = new Characteristic("intelligence", "Интеллект", StatType.Mental, defaultBaseValue);
            Will = new Characteristic("will", "Воля", StatType.Mental, defaultBaseValue);
            Learning = new Characteristic("learning", "Обучение", StatType.Mental, defaultBaseValue);
            Flexibility = new Characteristic("flexibility", "Гибкость", StatType.Mental, defaultBaseValue);
            Intuition = new Characteristic("intuition", "Интуиция", StatType.Mental, defaultBaseValue);
            SocialIntel = new Characteristic("social_intel", "Социальный интеллект", StatType.Mental, defaultBaseValue);
            Creativity = new Characteristic("creativity", "Творчество", StatType.Mental, defaultBaseValue);
            Mathematics = new Characteristic("mathematics", "Математика", StatType.Mental, defaultBaseValue);

            // Энергетические (база ВСЕГДА 100, отклонение -100..100)
            EnergyReserve = new Characteristic("energy_reserve", "Запас энергии", StatType.Energy, 100);
            EnergyRecovery = new Characteristic("energy_recovery", "Восстановление энергии", StatType.Energy, 100);
            Control = new Characteristic("control", "Контроль", StatType.Energy, 100);
            Concentration = new Characteristic("concentration", "Концентрация", StatType.Energy, 100);
            Output = new Characteristic("output", "Выход", StatType.Energy, 100);
            Precision = new Characteristic("precision", "Тонкость", StatType.Energy, 100);
            EnergyResist = new Characteristic("energy_resist", "Устойчивость", StatType.Energy, 100);
            EnergySense = new Characteristic("energy_sense", "Восприятие энергии", StatType.Energy, 100);

            // Собираем все в список
            AllStats = new List<Characteristic>
            {
                Endurance, Toughness, Strength, RecoveryPhys, Reflexes, Agility, Adaptation, Regeneration, Sensorics, Longevity,
                Focus, Memory, Logic, Deduction, Intelligence, Will, Learning, Flexibility, Intuition, SocialIntel, Creativity, Mathematics,
                EnergyReserve, EnergyRecovery, Control, Concentration, Output, Precision, EnergyResist, EnergySense
            };
        }

        // === ПОЛУЧЕНИЕ ХАРАКТЕРИСТИКИ ПО ID ===
        public Characteristic GetById(string id)
        {
            return AllStats.FirstOrDefault(s => s.Id == id);
        }

        // === ПОЛУЧЕНИЕ СЛОВАРЯ ФИНАЛЬНЫХ ЗНАЧЕНИЙ ДЛЯ ПРОВЕРКИ УСЛОВИЙ ===
        public Dictionary<string, int> GetFinalValuesDictionary()
        {
            return AllStats.ToDictionary(s => s.Id, s => s.FinalValue);
        }

        // === ПРОВЕРКА УСЛОВИЯ (только по финальным значениям) ===
        public bool CheckCondition(Condition condition)
        {
            var values = GetFinalValuesDictionary();
            return condition.Check(values);
        }

        // === ПРОВЕРКА НЕСКОЛЬКИХ УСЛОВИЙ ===
        public Dictionary<string, bool> CheckConditions(List<Condition> conditions)
        {
            var values = GetFinalValuesDictionary();
            return conditions.ToDictionary(c => c.Id, c => c.Check(values));
        }

        // === ОБНОВЛЕНИЕ ЗАВИСИМЫХ МОДИФИКАТОРОВ ДЛЯ ВСЕХ ХАРАКТЕРИСТИК ===
        public void UpdateAllDependentModifiers()
        {
            var finalValues = GetFinalValuesDictionary();
            foreach (var stat in AllStats)
            {
                stat.UpdateDependentModifiers(finalValues);
            }
        }

        // === ОБНОВЛЕНИЕ ВРЕМЕНИ (ДЛЯ ВСЕХ ХАРАКТЕРИСТИК) ===
        public void TickHour()
        {
            foreach (var stat in AllStats)
            {
                stat.TickHour();
            }
            UpdateAllDependentModifiers();
        }

        public void TickDay()
        {
            foreach (var stat in AllStats)
            {
                stat.TickDay();
            }
            UpdateAllDependentModifiers();
        }

        public void TickCombat()
        {
            foreach (var stat in AllStats)
            {
                stat.TickCombat();
            }
            UpdateAllDependentModifiers();
        }

        // === ПРИМЕНЕНИЕ ГЛОБАЛЬНОГО МОДИФИКАТОРА КО ВСЕМ ===
        public void ApplyGlobalModifier(Modifier modifier)
        {
            foreach (var stat in AllStats)
            {
                var copy = CloneModifier(modifier, stat.Id);
                if (copy != null)
                    stat.AddModifier(copy);
            }
        }

        private Modifier CloneModifier(Modifier original, string statId)
        {
            if (original is PermanentModifier perm)
            {
                return new PermanentModifier(
                    $"{perm.Id}_{statId}", perm.Name, perm.Source, perm.Type, perm.Value);
            }
            else if (original is DependentModifier dep)
            {
                return new DependentModifier(
                    $"{dep.Id}_{statId}", dep.Name, dep.Source, dep.Type, dep.Value, dep.Condition);
            }
            else if (original is IndependentModifier ind)
            {
                return new IndependentModifier(
                    $"{ind.Id}_{statId}", ind.Name, ind.Source, ind.Type, ind.Value, ind.TimeUnit, ind.Duration);
            }
            return null;
        }

        // === УДАЛЕНИЕ МОДИФИКАТОРОВ ПО ИСТОЧНИКУ ===
        public void RemoveModifiersFromSource(string source)
        {
            foreach (var stat in AllStats)
            {
                stat.RemoveModifiersFromSource(source);
            }
        }

        // === РАСЧЁТ РОСТА ХАРАКТЕРИСТИКИ ===
        public double CalculateGrowth(Characteristic stat, double baseGrowth, double learningBonus, double conditionsBonus)
        {
            // Gain = base × (100/(stat+100)) × обучение × условия × рандом
            double growthFactor = stat.GetGrowthFactor();
            double random = new Random().NextDouble() * 0.4 + 0.8; // 0.8-1.2
            return baseGrowth * growthFactor * learningBonus * conditionsBonus * random;
        }

        // === ДЛЯ БД ===
        public int[] GetBaseValuesArray()
        {
            return AllStats.Select(s => s.BaseValue).ToArray();
        }

        public int[] GetDeviationsArray()
        {
            return AllStats.Select(s => s.Deviation).ToArray();
        }

        public void LoadFromArrays(int[] baseValues, int[] deviations)
        {
            for (int i = 0; i < Math.Min(baseValues.Length, AllStats.Count); i++)
            {
                AllStats[i].BaseValue = baseValues[i];
            }
            for (int i = 0; i < Math.Min(deviations.Length, AllStats.Count); i++)
            {
                AllStats[i].SetDeviation(deviations[i]);
            }
        }

        // === СБРОС ВСЕХ ОТКЛОНЕНИЙ ===
        public void ResetAllDeviations()
        {
            foreach (var stat in AllStats)
            {
                stat.ResetDeviation();
            }
        }

        // === ДЛЯ ОТЛАДКИ ===
        public override string ToString()
        {
            return string.Join("\n", AllStats.Select(s => s.ToString()));
        }

        // В Statistics.cs добавить:
        public int GetStatValue(int statId)
        {
            return statId switch
            {
                // Физические (1-10)
                1 => Endurance.FinalValue,
                2 => Toughness.FinalValue,
                3 => Strength.FinalValue,
                4 => RecoveryPhys.FinalValue,
                5 => Reflexes.FinalValue,
                6 => Agility.FinalValue,
                7 => Adaptation.FinalValue,
                8 => Regeneration.FinalValue,
                9 => Sensorics.FinalValue,
                10 => Longevity.FinalValue,
                // Ментальные (11-22)
                11 => Focus.FinalValue,
                12 => Memory.FinalValue,
                13 => Logic.FinalValue,
                14 => Deduction.FinalValue,
                15 => Intelligence.FinalValue,
                16 => Will.FinalValue,
                17 => Learning.FinalValue,
                18 => Flexibility.FinalValue,
                19 => Intuition.FinalValue,
                20 => SocialIntel.FinalValue,
                21 => Creativity.FinalValue,
                22 => Mathematics.FinalValue,
                // Энергетические (23-30)
                23 => EnergyReserve.FinalValue,
                24 => EnergyRecovery.FinalValue,
                25 => Control.FinalValue,
                26 => Concentration.FinalValue,
                27 => Output.FinalValue,
                28 => Precision.FinalValue,
                29 => EnergyResist.FinalValue,
                30 => EnergySense.FinalValue,
                _ => 0
            };
        }
    }
}