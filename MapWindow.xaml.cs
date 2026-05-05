using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Controls;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Services;
using ApocMinimal.ViewModels;

namespace ApocMinimal;

public partial class MapWindow : Window
{
    private readonly GameViewModel _vm;

    public MapWindow(GameViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Build();
    }

    private void Build()
    {
        MapPanel.Children.Clear();
        var locs       = _vm.Locations;
        var controlled = _vm.ControlledZoneIds;
        var allNpcs    = _vm.AllNpcs;

        foreach (var city in locs.Where(l => l.Type == LocationType.City))
        {
            MapPanel.Children.Add(Row("🌆 " + city.Name, "#58a6ff", "#0d1f35", controlled.Contains(city.Id)));

            foreach (var district in locs.Where(l => l.ParentId == city.Id).OrderBy(l => l.Name))
            {
                var distStreets = locs.Where(l => l.ParentId == district.Id).ToList();
                int totalBld = distStreets.SelectMany(s => locs.Where(l => l.ParentId == s.Id && l.Type == LocationType.Building)).Count();
                int expBld   = distStreets.SelectMany(s => locs.Where(l => l.ParentId == s.Id && l.Type == LocationType.Building && l.IsExplored)).Count();

                if (!district.IsExplored)
                {
                    MapPanel.Children.Add(Row($"  ? Район: {district.Name}  ({totalBld} зданий, не исследован)", "#4b5563", "#0d1117", false));
                    continue;
                }

                MapPanel.Children.Add(Row($"  🏘 {district.Name}  [{expBld}/{totalBld} зд.]", "#a5b4fc", "#0f1535", controlled.Contains(district.Id)));

                foreach (var street in distStreets.Where(s => s.IsExplored).OrderBy(s => s.Name))
                {
                    var buildings  = locs.Where(l => l.ParentId == street.Id && l.Type == LocationType.Building).ToList();
                    var commercials = locs.Where(l => l.ParentId == street.Id && l.Type == LocationType.Commercial).ToList();
                    int expB = buildings.Count(b => b.IsExplored);
                    int unkB = buildings.Count - expB;

                    MapPanel.Children.Add(Row($"    🛣 {street.Name}  [{expB} иссл.]", "#7dd3fc", "#0a1a28", controlled.Contains(street.Id)));

                    foreach (var bld in buildings.Where(b => b.IsExplored).OrderBy(b => b.Name))
                    {
                        var npcsB = allNpcs.Where(n => n.IsAlive && n.LocationId == bld.Id).ToList();
                        string status = bld.Status == LocationStatus.Cleared ? "✓" : "⚠";
                        string bldColor = bld.Status == LocationStatus.Cleared ? "#56d364" : "#fbbf24";
                        string npcTag  = npcsB.Count > 0 ? $"  👤{npcsB.Count}" : "";
                        MapPanel.Children.Add(Row($"      🏢 {bld.Name}  {status}{npcTag}", bldColor, "#111820", controlled.Contains(bld.Id)));

                        foreach (var floor in locs.Where(l => l.ParentId == bld.Id && l.IsExplored).OrderBy(l => l.Name))
                        {
                            var npcsF  = allNpcs.Where(n => n.IsAlive && n.LocationId == floor.Id).ToList();
                            var apts   = locs.Where(l => l.ParentId == floor.Id && l.IsExplored).ToList();
                            var npcsApt = allNpcs.Where(n => n.IsAlive && apts.Any(a => a.Id == n.LocationId)).ToList();
                            int npcCount = npcsF.Count + npcsApt.Count;
                            string resLine = floor.ResourceNodes.Count > 0
                                ? "  " + string.Join(" | ", floor.ResourceNodes.Where(kv => kv.Value > 0).Select(kv => $"{kv.Key}:{kv.Value:F0}"))
                                : "";
                            string npcF = npcCount > 0 ? $"  👤{npcCount}" : "";
                            string floorColor = floor.Status == LocationStatus.Cleared ? "#56d364" : "#f87171";
                            MapPanel.Children.Add(Row($"        ▸ {floor.Name}{npcF}{resLine}", floorColor, "#0d1117", controlled.Contains(floor.Id)));
                        }
                    }

                    foreach (var comm in commercials.Where(c => c.IsExplored).OrderBy(c => c.Name))
                    {
                        string icon = comm.CommercialType switch
                        {
                            CommercialType.Shop        => "🛒",
                            CommercialType.Supermarket => "🏪",
                            CommercialType.Mall        => "🏬",
                            CommercialType.Market      => "🛍️",
                            CommercialType.Hairdresser => "💈",
                            CommercialType.BeautySalon => "💅",
                            CommercialType.Pharmacy    => "💊",
                            CommercialType.Hospital    => "🏥",
                            CommercialType.Factory     => "🏭",
                            CommercialType.Hotel       => "🏨",
                            _                          => "🏢"
                        };
                        string cstatus = comm.Status == LocationStatus.Cleared ? "✓" : "⚠";
                        string ccolor  = comm.Status == LocationStatus.Cleared ? "#56d364" : "#fbbf24";
                        MapPanel.Children.Add(Row($"      {icon} {comm.Name}  {cstatus}", ccolor, "#111820", controlled.Contains(comm.Id)));
                    }

                    if (unkB > 0)
                        MapPanel.Children.Add(Row($"      + ещё {unkB} зд. (не исследовано)", "#4b5563", "#0d1117", false));
                }
            }
        }

        if (!locs.Any())
            MapPanel.Children.Add(new TextBlock { Text = "Карта не загружена.", Foreground = Brush("#8b949e"), FontSize = 10 });
    }

    private static Border Row(string text, string textColor, string bgColor, bool isProtected)
    {
        string stripColor = isProtected ? "#60a5fa" : textColor;
        var border = new Border
        {
            Background      = Brush(bgColor),
            BorderBrush     = Brush(stripColor),
            BorderThickness = new Thickness(2, 0, 0, 0),
            Margin          = new Thickness(0, 1, 0, 0),
            Padding         = new Thickness(4, 2, 2, 2),
        };
        border.Child = new TextBlock
        {
            Text        = text,
            Foreground  = Brush(textColor),
            FontSize    = 10,
            TextWrapping = TextWrapping.NoWrap,
        };
        return border;
    }

    private static SolidColorBrush Brush(string hex) => BrushCache.GetBrush(hex);
}
