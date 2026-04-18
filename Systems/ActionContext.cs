using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.ResourceData;

namespace ApocMinimal.Systems;

public class ActionContext
{
    public List<Resource> Resources { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
}
