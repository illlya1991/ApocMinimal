namespace ApocalypseSimulation.Models.StatisticsData
{
    /// <summary>
    /// Тип характеристики
    /// </summary>
    public enum StatType
    {
        Physical,   // Физические (10 шт.)
        Mental,     // Ментальные (12 шт.)
        Energy      // Энергетические (8 шт.)
    }

    /// <summary>
    /// Тип модификатора
    /// </summary>
    public enum ModifierType
    {
        Additive,       // Аддитивный (+X)
        Multiplicative  // Мультипликативный (×X)
    }

    /// <summary>
    /// Тип временного модификатора
    /// </summary>
    public enum TemporaryModifierType
    {
        Dependent,      // Зависимый (работает пока есть условие)
        Independent     // Независимый (работает определённое время)
    }

    /// <summary>
    /// Единица измерения времени для независимых модификаторов
    /// </summary>
    public enum TimeUnit
    {
        Hours,
        Days,
        CombatTurns
    }

    /// <summary>
    /// Оператор для условий
    /// </summary>
    public enum ConditionOperator
    {
        AND,
        OR
    }
}