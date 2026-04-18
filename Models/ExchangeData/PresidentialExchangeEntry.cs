namespace ApocMinimal.Models.ExchangeData;

public record ExchangeStatEffect(int StatNumber, double Multiplier);
public record ExchangeNeedEffect(int NeedId, int LevelDelta);
public record ExchangeResourceEffect(string ResourceName, double Amount);

public class PresidentialExchangeEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string GiveText { get; set; } = "";
    public string GetText { get; set; } = "";
    public bool IsDay1Only { get; set; }
    public ExchangeStatEffect[] StatEffects { get; set; } = [];
    public ExchangeNeedEffect[] NeedEffects { get; set; } = [];
    public ExchangeResourceEffect[] ResourceEffects { get; set; } = [];
}
