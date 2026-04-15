using System;
using System.Collections.Generic;
using System.Linq;

namespace ApocMinimal.Models.StatisticsData
{
    /// <summary>
    /// Контейнер всех 30 характеристик NPC с прямым доступом к свойствам
    /// </summary>
    public class Statistics
    {
        // === ФИЗИЧЕСКИЕ (10) ===
        public Characteristic EnduranceChar { get; private set; }      // Выносливость
        public Characteristic ToughnessChar { get; private set; }      // Стойкость
        public Characteristic StrengthChar { get; private set; }       // Сила
        public Characteristic RecoveryPhysChar { get; private set; }   // Восстановление (физ)
        public Characteristic ReflexesChar { get; private set; }       // Рефлексы
        public Characteristic AgilityChar { get; private set; }        // Ловкость
        public Characteristic AdaptationChar { get; private set; }     // Адаптация
        public Characteristic RegenerationChar { get; private set; }   // Регенерация
        public Characteristic SensoricsChar { get; private set; }      // Сенсорика
        public Characteristic LongevityChar { get; private set; }      // Долголетие

        // === МЕНТАЛЬНЫЕ (12) ===
        public Characteristic FocusChar { get; private set; }          // Фокус
        public Characteristic MemoryChar { get; private set; }         // Память
        public Characteristic LogicChar { get; private set; }          // Логика
        public Characteristic DeductionChar { get; private set; }      // Дедукция
        public Characteristic IntelligenceChar { get; private set; }   // Интеллект
        public Characteristic WillChar { get; private set; }           // Воля
        public Characteristic LearningChar { get; private set; }       // Обучение
        public Characteristic FlexibilityChar { get; private set; }    // Гибкость
        public Characteristic IntuitionChar { get; private set; }      // Интуиция
        public Characteristic SocialIntelChar { get; private set; }    // Социальный интеллект
        public Characteristic CreativityChar { get; private set; }     // Творчество
        public Characteristic MathematicsChar { get; private set; }    // Математика

        // === ЭНЕРГЕТИЧЕСКИЕ (8) ===
        public Characteristic EnergyReserveChar { get; private set; }  // Запас энергии
        public Characteristic EnergyRecoveryChar { get; private set; } // Восстановление (энерг)
        public Characteristic ControlChar { get; private set; }        // Контроль
        public Characteristic ConcentrationChar { get; private set; }  // Концентрация
        public Characteristic OutputChar { get; private set; }         // Выход
        public Characteristic PrecisionChar { get; private set; }      // Тонкость
        public Characteristic EnergyResistChar { get; private set; }   // Устойчивость
        public Characteristic EnergySenseChar { get; private set; }    // Восприятие (энерг)

        // === ПРЯМОЙ ДОСТУП К ЗНАЧЕНИЯМ (FinalValue) ===
        public int Endurance => EnduranceChar.FinalValue;
        public int Toughness => ToughnessChar.FinalValue;
        public int Strength => StrengthChar.FinalValue;
        public int RecoveryPhys => RecoveryPhysChar.FinalValue;
        public int Reflexes => ReflexesChar.FinalValue;
        public int Agility => AgilityChar.FinalValue;
        public int Adaptation => AdaptationChar.FinalValue;
        public int Regeneration => RegenerationChar.FinalValue;
        public int Sensorics => SensoricsChar.FinalValue;
        public int Longevity => LongevityChar.FinalValue;

        public int Focus => FocusChar.FinalValue;
        public int Memory => MemoryChar.FinalValue;
        public int Logic => LogicChar.FinalValue;
        public int Deduction => DeductionChar.FinalValue;
        public int Intelligence => IntelligenceChar.FinalValue;
        public int Will => WillChar.FinalValue;
        public int Learning => LearningChar.FinalValue;
        public int Flexibility => FlexibilityChar.FinalValue;
        public int Intuition => IntuitionChar.FinalValue;
        public int SocialIntel => SocialIntelChar.FinalValue;
        public int Creativity => CreativityChar.FinalValue;
        public int Mathematics => MathematicsChar.FinalValue;

        public int EnergyReserve => EnergyReserveChar.FinalValue;
        public int EnergyRecovery => EnergyRecoveryChar.FinalValue;
        public int Control => ControlChar.FinalValue;
        public int Concentration => ConcentrationChar.FinalValue;
        public int Output => OutputChar.FinalValue;
        public int Precision => PrecisionChar.FinalValue;
        public int EnergyResist => EnergyResistChar.FinalValue;
        public int EnergySense => EnergySenseChar.FinalValue;

        // === СПИСОК ВСЕХ ХАРАКТЕРИСТИК ===
        public List<Characteristic> AllStats { get; private set; }

        // === КОНСТРУКТОР ===
        public Statistics(int defaultBaseValue = 100)
        {
            // Физические
            EnduranceChar = new Characteristic("endurance", "Выносливость", StatType.Physical, 1, defaultBaseValue);
            ToughnessChar = new Characteristic("toughness", "Стойкость", StatType.Physical, 2, defaultBaseValue);
            StrengthChar = new Characteristic("strength", "Сила", StatType.Physical, 3, defaultBaseValue);
            RecoveryPhysChar = new Characteristic("recovery_phys", "Восстановление (физ)", StatType.Physical, 4, defaultBaseValue);
            ReflexesChar = new Characteristic("reflexes", "Рефлексы", StatType.Physical, 5, defaultBaseValue);
            AgilityChar = new Characteristic("agility", "Ловкость", StatType.Physical, 6, defaultBaseValue);
            AdaptationChar = new Characteristic("adaptation", "Адаптация", StatType.Physical, 7, defaultBaseValue);
            RegenerationChar = new Characteristic("regeneration", "Регенерация", StatType.Physical, 8, defaultBaseValue);
            SensoricsChar = new Characteristic("sensorics", "Сенсорика", StatType.Physical, 9, defaultBaseValue);
            LongevityChar = new Characteristic("longevity", "Долголетие", StatType.Physical, 10, defaultBaseValue);

            // Ментальные
            FocusChar = new Characteristic("focus", "Фокус", StatType.Mental, 11, defaultBaseValue);
            MemoryChar = new Characteristic("memory", "Память", StatType.Mental, 12, defaultBaseValue);
            LogicChar = new Characteristic("logic", "Логика", StatType.Mental, 13, defaultBaseValue);
            DeductionChar = new Characteristic("deduction", "Дедукция", StatType.Mental, 14, defaultBaseValue);
            IntelligenceChar = new Characteristic("intelligence", "Интеллект", StatType.Mental, 15, defaultBaseValue);
            WillChar = new Characteristic("will", "Воля", StatType.Mental, 16, defaultBaseValue);
            LearningChar = new Characteristic("learning", "Обучение", StatType.Mental, 17, defaultBaseValue);
            FlexibilityChar = new Characteristic("flexibility", "Гибкость", StatType.Mental, 18, defaultBaseValue);
            IntuitionChar = new Characteristic("intuition", "Интуиция", StatType.Mental, 19, defaultBaseValue);
            SocialIntelChar = new Characteristic("social_intel", "Социальный интеллект", StatType.Mental, 20, defaultBaseValue);
            CreativityChar = new Characteristic("creativity", "Творчество", StatType.Mental, 21, defaultBaseValue);
            MathematicsChar = new Characteristic("mathematics", "Математика", StatType.Mental, 22, defaultBaseValue);

            // Энергетические (база ВСЕГДА 100)
            EnergyReserveChar = new Characteristic("energy_reserve", "Запас энергии", StatType.Energy, 23, 100);
            EnergyRecoveryChar = new Characteristic("energy_recovery", "Восстановление энергии", StatType.Energy, 24, 100);
            ControlChar = new Characteristic("control", "Контроль", StatType.Energy, 25, 100);
            ConcentrationChar = new Characteristic("concentration", "Концентрация", StatType.Energy, 26, 100);
            OutputChar = new Characteristic("output", "Выход", StatType.Energy, 27, 100);
            PrecisionChar = new Characteristic("precision", "Тонкость", StatType.Energy, 28, 100);
            EnergyResistChar = new Characteristic("energy_resist", "Устойчивость", StatType.Energy, 29, 100);
            EnergySenseChar = new Characteristic("energy_sense", "Восприятие энергии", StatType.Energy, 30, 100);

            // Собираем все в список
            AllStats = new List<Characteristic>
            {
                EnduranceChar, ToughnessChar, StrengthChar, RecoveryPhysChar, ReflexesChar,
                AgilityChar, AdaptationChar, RegenerationChar, SensoricsChar, LongevityChar,
                FocusChar, MemoryChar, LogicChar, DeductionChar, IntelligenceChar, WillChar,
                LearningChar, FlexibilityChar, IntuitionChar, SocialIntelChar, CreativityChar, MathematicsChar,
                EnergyReserveChar, EnergyRecoveryChar, ControlChar, ConcentrationChar, OutputChar,
                PrecisionChar, EnergyResistChar, EnergySenseChar
            };
        }

        // === ПОЛУЧЕНИЕ ХАРАКТЕРИСТИКИ ПО ID ===
        public Characteristic? GetById(string id)
        {
            return AllStats.FirstOrDefault(s => s.Id == id);
        }

        // === ПОЛУЧЕНИЕ ХАРАКТЕРИСТИКИ ПО НОМЕРУ (1-30) ===
        public Characteristic? GetByNumber(int statNumber)
        {
            return AllStats.FirstOrDefault(s => s.StatNumber == statNumber);
        }

        // === ПОЛУЧЕНИЕ ХАРАКТЕРИСТИКИ ПО ИМЕНИ ===
        public Characteristic? GetByName(string name)
        {
            return AllStats.FirstOrDefault(s => s.Name == name);
        }

        // === ПОЛУЧЕНИЕ ФИНАЛЬНОГО ЗНАЧЕНИЯ ПО НОМЕРУ ===
        public int GetStatValue(int statNumber)
        {
            return GetByNumber(statNumber)?.FinalValue ?? 0;
        }

        // === ПОЛУЧЕНИЕ ФИНАЛЬНОГО ЗНАЧЕНИЯ ПО ИМЕНИ ===
        public int GetStatValue(string name)
        {
            return GetByName(name)?.FinalValue ?? 0;
        }

        // === ПОЛУЧЕНИЕ ХАРАКТЕРИСТИК ПО ТИПУ ===
        public List<Characteristic> GetPhysicalStats() => AllStats.Where(s => s.Type == StatType.Physical).ToList();
        public List<Characteristic> GetMentalStats() => AllStats.Where(s => s.Type == StatType.Mental).ToList();
        public List<Characteristic> GetEnergyStats() => AllStats.Where(s => s.Type == StatType.Energy).ToList();

        // === ПОЛУЧЕНИЕ ХАРАКТЕРИСТИК ДЛЯ БОЯ ===
        public List<Characteristic> GetCombatStats() => new List<Characteristic>
        {
            StrengthChar, AgilityChar, EnduranceChar, ToughnessChar, ReflexesChar
        };

        // === ПОЛУЧЕНИЕ СЛОВАРЯ ФИНАЛЬНЫХ ЗНАЧЕНИЙ ===
        public Dictionary<string, int> GetFinalValuesByName()
        {
            return AllStats.ToDictionary(s => s.Name, s => s.FinalValue);
        }

        public Dictionary<string, int> GetFinalValuesById()
        {
            return AllStats.ToDictionary(s => s.Id, s => s.FinalValue);
        }

        // === ПРОВЕРКА УСЛОВИЯ ===
        public bool CheckCondition(Condition condition)
        {
            var values = GetFinalValuesById();
            return condition.Check(values);
        }

        // === ОБНОВЛЕНИЕ ЗАВИСИМЫХ МОДИФИКАТОРОВ ===
        public void UpdateAllDependentModifiers()
        {
            var finalValues = GetFinalValuesById();
            foreach (var stat in AllStats)
            {
                stat.UpdateDependentModifiers(finalValues);
            }
        }

        // === ОБНОВЛЕНИЕ ВРЕМЕНИ ===
        public void TickHour()
        {
            foreach (var stat in AllStats) stat.TickHour();
            UpdateAllDependentModifiers();
        }

        public void TickDay()
        {
            foreach (var stat in AllStats) stat.TickDay();
            UpdateAllDependentModifiers();
        }

        public void TickCombat()
        {
            foreach (var stat in AllStats) stat.TickCombat();
            UpdateAllDependentModifiers();
        }

        // === ПРИМЕНЕНИЕ ГЛОБАЛЬНОГО МОДИФИКАТОРА ===
        public void ApplyGlobalModifier(Modifier modifier)
        {
            foreach (var stat in AllStats)
            {
                var copy = CloneModifier(modifier, stat.Id);
                if (copy != null) stat.AddModifier(copy);
            }
        }

        private Modifier? CloneModifier(Modifier original, string statId)
        {
            if (original is PermanentModifier perm)
                return new PermanentModifier($"{perm.Id}_{statId}", perm.Name, perm.Source, perm.Type, perm.Value);
            if (original is DependentModifier dep)
                return new DependentModifier($"{dep.Id}_{statId}", dep.Name, dep.Source, dep.Type, dep.Value, dep.Condition);
            if (original is IndependentModifier ind)
                return new IndependentModifier($"{ind.Id}_{statId}", ind.Name, ind.Source, ind.Type, ind.Value, ind.TimeUnit, ind.Duration);
            return null;
        }

        // === УДАЛЕНИЕ МОДИФИКАТОРОВ ===
        public void RemoveModifiersFromSource(string source)
        {
            foreach (var stat in AllStats) stat.RemoveModifiersFromSource(source);
        }

        // === ДЛЯ БД ===
        public int[] GetBaseValuesArray() => AllStats.Select(s => s.BaseValue).ToArray();
        public int[] GetDeviationsArray() => AllStats.Select(s => s.Deviation).ToArray();

        public void LoadFromArrays(int[] baseValues, int[] deviations)
        {
            for (int i = 0; i < Math.Min(baseValues.Length, AllStats.Count); i++)
                AllStats[i].BaseValue = baseValues[i];
            for (int i = 0; i < Math.Min(deviations.Length, AllStats.Count); i++)
                AllStats[i].SetDeviation(deviations[i]);
        }

        // === СБРОС ОТКЛОНЕНИЙ ===
        public void ResetAllDeviations()
        {
            foreach (var stat in AllStats) stat.ResetDeviation();
        }

        public override string ToString() => string.Join("\n", AllStats.Select(s => s.ToString()));
    }
}