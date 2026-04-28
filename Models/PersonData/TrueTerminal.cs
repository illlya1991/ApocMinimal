namespace ApocMinimal.Models.PersonData;

/// <summary>
/// Статус Истинного ЦС — финальной цели игры.
/// Создаётся при старте, отслеживает прогресс к победе.
/// </summary>
public class TrueTerminal
{
    /// <summary>Игра уже завершена победой.</summary>
    public bool IsAchieved { get; set; }

    /// <summary>День, когда был достигнут ЦС (0 = не достигнут).</summary>
    public int AchievedDay { get; set; }

    // ── Условия победы ───────────────────────────────────────────────────

    /// <summary>Необходимый уровень Терминала (должен быть 10).</summary>
    public int RequiredTerminalLevel { get; set; } = 10;

    /// <summary>Необходимое количество живых последователей.</summary>
    public int RequiredFollowers { get; set; } = 5;

    /// <summary>Необходимый запас ОР.</summary>
    public double RequiredDevPoints { get; set; } = 200;

    /// <summary>Необходимое количество дней выживания.</summary>
    public int RequiredDays { get; set; } = 30;

    // ── Прогресс ─────────────────────────────────────────────────────────

    public bool CheckAchieved(int terminalLevel, int aliveFollowers, double devPoints, int currentDay)
    {
        return terminalLevel >= RequiredTerminalLevel
            && aliveFollowers >= RequiredFollowers
            && devPoints >= RequiredDevPoints
            && currentDay >= RequiredDays;
    }

    public string GetProgressSummary(int terminalLevel, int aliveFollowers, double devPoints, int currentDay)
    {
        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"Терминал: {terminalLevel}/{RequiredTerminalLevel} {(terminalLevel >= RequiredTerminalLevel ? "✓" : "✗")}");
        lines.AppendLine($"Последователи: {aliveFollowers}/{RequiredFollowers} {(aliveFollowers >= RequiredFollowers ? "✓" : "✗")}");
        lines.AppendLine($"ОР: {devPoints:F0}/{RequiredDevPoints:F0} {(devPoints >= RequiredDevPoints ? "✓" : "✗")}");
        lines.AppendLine($"Дней выжито: {currentDay}/{RequiredDays} {(currentDay >= RequiredDays ? "✓" : "✗")}");
        return lines.ToString().TrimEnd();
    }
}
