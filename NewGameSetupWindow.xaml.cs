using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Services;

namespace ApocMinimal;

public partial class NewGameSetupWindow : Window
{
    public string ChosenName { get; private set; } = "Игрок";
    public PlayerFaction ChosenFaction { get; private set; } = PlayerFaction.ElementMages;
    public bool Confirmed { get; private set; }

    ListPlayerFactions listPlayerFactions;

    public NewGameSetupWindow()
    {
        InitializeComponent();
        NameBox.Text = "Игрок";
        NameBox.SelectAll();
        NameBox.Focus();
        listPlayerFactions = new ListPlayerFactions();
        BuildFactionList();
    }

    private void BuildFactionList()
    {
        foreach (OnePlayerFaction itemFaction in listPlayerFactions.factions)
        {
            var item = new ListBoxItem
            {
                Content = itemFaction.Label,
                Tag = itemFaction.Faction,
                Foreground = BrushCache.GetBrush(itemFaction.Color)!,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(10, 6, 10, 6),
                Background = Brushes.Transparent,
            };
            FactionList.Items.Add(item);
        }
        FactionList.SelectedIndex = 0;
    }

    private void FactionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FactionList.SelectedItem is not ListBoxItem item) return;

        var faction = (PlayerFaction)item.Tag;
        ChosenFaction = faction;

        OnePlayerFaction onePlayerFaction = listPlayerFactions.factions.FirstOrDefault(pf => pf.Faction == faction);
        FactionDescText.Text = onePlayerFaction.Description;
        if (onePlayerFaction != default)
            FactionDescText.Foreground = BrushCache.GetBrush(onePlayerFaction.Color)!;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        string name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = "Игрок";
        ChosenName = name;

        if (FactionList.SelectedItem is ListBoxItem item)
            ChosenFaction = (PlayerFaction)item.Tag;

        Confirmed = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }
}
