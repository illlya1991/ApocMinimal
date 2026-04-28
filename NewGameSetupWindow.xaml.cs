using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;

namespace ApocMinimal;

public partial class NewGameSetupWindow : Window
{
    public string ChosenName { get; private set; } = "Игрок";
    public PlayerFaction ChosenFaction { get; private set; } = PlayerFaction.ElementMages;
    public bool Confirmed { get; private set; }

    private static readonly (PlayerFaction Faction, string Label, string Color)[] Factions =
    {
        (PlayerFaction.ElementMages,  "Маги Стихий",      "#79c0ff"),
        (PlayerFaction.PathBlades,    "Клинки Пути",      "#f87171"),
        (PlayerFaction.MirrorHealers, "Зеркальные Целители", "#56d364"),
        (PlayerFaction.DeepSmiths,    "Кузнецы Глубин",   "#e3b341"),
        (PlayerFaction.GuardHeralds,  "Герольды Стражи",  "#d2a8ff"),
    };

    public NewGameSetupWindow()
    {
        InitializeComponent();
        NameBox.Text = "Игрок";
        NameBox.SelectAll();
        NameBox.Focus();
        BuildFactionList();
    }

    private void BuildFactionList()
    {
        foreach (var (faction, label, color) in Factions)
        {
            var item = new ListBoxItem
            {
                Content = label,
                Tag = faction,
                Foreground = (Brush)new BrushConverter().ConvertFromString(color)!,
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

        var entry = System.Array.Find(Factions, x => x.Faction == faction);
        FactionDescText.Text = faction.ToDescription();
        if (entry != default)
            FactionDescText.Foreground = (Brush)new BrushConverter().ConvertFromString(entry.Color)!;
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
