using ApocMinimal.Services;

namespace ApocMinimal;

/// <summary>Pre-built services passed from LoadingWindow to GameWindow to avoid double-init.</summary>
public class GameInitState
{
    public LocationService?  LocationService  { get; set; }
    public TechniqueService? TechniqueService { get; set; }
}
