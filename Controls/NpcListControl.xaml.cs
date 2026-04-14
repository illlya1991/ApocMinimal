using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Services;

namespace ApocMinimal.Controls;

public partial class NpcListControl : UserControl
{
    public event Action<Npc>? NpcSelected;

    private readonly GameUIService _uiService;
    private List<Npc> _npcs = new();

    public NpcListControl()
    {
        InitializeComponent();
        _uiService = new GameUIService((text, color) => { });
    }

    public void UpdateNpcs(List<Npc> npcs, Npc? currentSelectedNpc = null)
    {
        _npcs = npcs;
        NpcPanel.Children.Clear();

        foreach (var npc in _npcs)
        {
            var card = _uiService.BuildNpcCard(npc);
            var capturedNpc = npc;

            card.MouseLeftButtonUp += (_, e) =>
            {
                NpcSelected?.Invoke(capturedNpc);
            };

            // Подсветка выбранного NPC
            if (currentSelectedNpc != null && currentSelectedNpc.Id == npc.Id)
            {
                card.BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#60a5fa")!;
                card.BorderThickness = new Thickness(2);
            }

            NpcPanel.Children.Add(card);
        }
    }
}