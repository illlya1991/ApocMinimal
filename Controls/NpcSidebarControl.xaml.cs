using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

namespace ApocMinimal.Controls;

public partial class NpcSidebarControl : UserControl
{
    private RadioButton? _rbFull;
    private RadioButton? _rbDetailed;
    private RadioButton? _rbCompact;
    private RadioButton? _rbCombat;
    private RadioButton? _rbSocial;
    private Npc? _currentNpc;

    public event Action? Closed;
    public bool IsVisible => SidebarRoot.Visibility == Visibility.Visible;
    public Npc? CurrentNpc => _currentNpc;

    public NpcSidebarControl()
    {
        InitializeComponent();
        BuildModeSelector();
    }

    private void BuildModeSelector()
    {
        var modePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        modePanel.Children.Add(new TextBlock
        {
            Text = "Режим отображения:",
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8b949e")!,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 5)
        });

        var radioPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };

        _rbFull = CreateModeRadioButton("📋 Все", "full");
        _rbDetailed = CreateModeRadioButton("📊 Детально", "detailed");
        _rbCompact = CreateModeRadioButton("📄 Сжато", "compact");
        _rbCombat = CreateModeRadioButton("⚔ Боевое", "combat");
        _rbSocial = CreateModeRadioButton("💬 Социальное", "social");

        _rbFull.IsChecked = true;

        radioPanel.Children.Add(_rbFull);
        radioPanel.Children.Add(_rbDetailed);
        radioPanel.Children.Add(_rbCompact);
        radioPanel.Children.Add(_rbCombat);
        radioPanel.Children.Add(_rbSocial);

        modePanel.Children.Add(radioPanel);
        SidebarContent.Children.Add(modePanel);
    }

    private RadioButton CreateModeRadioButton(string text, string mode)
    {
        var rb = new RadioButton
        {
            Content = text,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#c9d1d9")!,
            Margin = new Thickness(0, 0, 15, 0),
            Tag = mode
        };
        rb.Checked += (s, e) => UpdateContent();
        return rb;
    }

    public void ShowNpc(Npc npc)
    {
        _currentNpc = npc;
        SidebarTitle.Text = $"ИНФОРМАЦИЯ: {npc.Name}";
        SidebarRoot.Visibility = Visibility.Visible;
        UpdateContent();
    }

    public void Hide()
    {
        SidebarRoot.Visibility = Visibility.Collapsed;
        _currentNpc = null;
        Closed?.Invoke();
    }

    public void Toggle(Npc npc)
    {
        if (_currentNpc == npc && IsVisible)
            Hide();
        else
            ShowNpc(npc);
    }

    private void UpdateContent()
    {
        if (_currentNpc == null) return;

        // Удаляем старый контент (кроме радиобоксов)
        while (SidebarContent.Children.Count > 1)
            SidebarContent.Children.RemoveAt(1);

        string mode = GetSelectedMode();
        var infoPanel = BuildInfoPanel(_currentNpc, mode);
        SidebarContent.Children.Add(infoPanel);
    }

    private string GetSelectedMode()
    {
        if (_rbFull?.IsChecked == true) return "full";
        if (_rbDetailed?.IsChecked == true) return "detailed";
        if (_rbCompact?.IsChecked == true) return "compact";
        if (_rbCombat?.IsChecked == true) return "combat";
        if (_rbSocial?.IsChecked == true) return "social";
        return "full";
    }

    private StackPanel BuildInfoPanel(Npc npc, string mode)
    {
        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = $"{npc.Name} [{npc.GenderLabel}] {npc.Age} лет",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
            Margin = new Thickness(0, 0, 0, 10)
        });

        switch (mode)
        {
            case "full": BuildFullInfo(panel, npc); break;
            case "detailed": BuildDetailedInfo(panel, npc); break;
            case "compact": BuildCompactInfo(panel, npc); break;
            case "combat": BuildCombatInfo(panel, npc); break;
            case "social": BuildSocialInfo(panel, npc); break;
        }

        return panel;
    }

    private void BuildFullInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(CreateSectionHeader("ОСНОВНЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(CreateInfoRow("HP:", $"{npc.Health:F0}", npc.Health < 30 ? "#f87171" : "#4ade80"));
        panel.Children.Add(CreateInfoRow("Выносливость:", $"{npc.Stamina:F0}", npc.Stamina < 30 ? "#f87171" : "#60a5fa"));
        panel.Children.Add(CreateInfoRow("Чакра:", $"{npc.Chakra:F0}", "#e879f9"));
        panel.Children.Add(CreateInfoRow("Вера:", $"{npc.Faith:F0}", "#facc15"));
        panel.Children.Add(CreateInfoRow("Страх:", $"{npc.Fear:F0}", npc.Fear > 70 ? "#f87171" : "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Инициатива:", $"{npc.Initiative:F0}", "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Уровень:", npc.FollowerLabel, "#d29922"));

        panel.Children.Add(CreateSectionHeader("ЛИЧНОСТЬ"));
        panel.Children.Add(CreateInfoRow("Черты:", string.Join(", ", npc.CharTraits.Select(c => c.ToLabel())), "#d29922"));
        panel.Children.Add(CreateInfoRow("Эмоции:", string.Join(" | ", npc.Emotions.Select(e => $"{e.Name} {e.Percentage:F0}%")), "#e879f9"));

        panel.Children.Add(CreateSectionHeader("ЦЕЛИ"));
        panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Желание:", npc.Desire, "#c9d1d9"));

        if (npc.Specializations.Any())
            panel.Children.Add(CreateInfoRow("Специализации:", string.Join(", ", npc.Specializations), "#56d364"));

        panel.Children.Add(CreateSectionHeader("ПОТРЕБНОСТИ"));
        foreach (var need in npc.Needs.Where(n => n.IsUrgent || n.IsCritical))
        {
            panel.Children.Add(CreateInfoRow($"  {need.Name}:", $"{need.Value:F0}%", need.IsCritical ? "#f87171" : "#fbbf24"));
        }

        panel.Children.Add(CreateSectionHeader("ХАРАКТЕРИСТИКИ", true));
        var statsPanel = new StackPanel { Margin = new Thickness(15, 0, 0, 0), Visibility = Visibility.Collapsed };
        AddAllStats(statsPanel, npc);
        panel.Children.Add(statsPanel);
    }

    private void BuildDetailedInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ"));
        AddPhysicalStats(panel, npc);
        panel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ"));
        AddMentalStats(panel, npc);
        panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
        AddEnergyStats(panel, npc);
    }

    private void BuildCompactInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(new TextBlock
        {
            Text = $"❤{npc.Health:F0} ✦{npc.Faith:F0} 😨{npc.Fear:F0} 🤝{npc.Trust:F0} 💪{npc.Stats.Strength.FinalValue} 🧠{npc.Stats.Intelligence.FinalValue}",
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#c9d1d9")!,
            FontSize = 12,
            Margin = new Thickness(0, 5, 0, 5)
        });

        var criticalNeeds = npc.Needs.Where(n => n.IsCritical).ToList();
        if (criticalNeeds.Any())
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"⚠ КРИТИЧНО: {string.Join(", ", criticalNeeds.Select(n => n.Name))}",
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#f87171")!,
                FontSize = 11
            });
        }
    }

    private void BuildCombatInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(CreateSectionHeader("БОЕВЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(CreateInfoRow("Сила:", $"{npc.Stats.Strength.FinalValue}", GetStatColor(npc.Stats.Strength.FinalValue)));
        panel.Children.Add(CreateInfoRow("Ловкость:", $"{npc.Stats.Agility.FinalValue}", GetStatColor(npc.Stats.Agility.FinalValue)));
        panel.Children.Add(CreateInfoRow("Выносливость:", $"{npc.Stats.Endurance.FinalValue}", GetStatColor(npc.Stats.Endurance.FinalValue)));
        panel.Children.Add(CreateInfoRow("Стойкость:", $"{npc.Stats.Toughness.FinalValue}", GetStatColor(npc.Stats.Toughness.FinalValue)));
        panel.Children.Add(CreateInfoRow("Рефлексы:", $"{npc.Stats.Reflexes.FinalValue}", GetStatColor(npc.Stats.Reflexes.FinalValue)));
        panel.Children.Add(CreateInfoRow("Боевая инициатива:", $"{npc.CombatInitiative:F0}", "#c9d1d9"));

        panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
        panel.Children.Add(CreateInfoRow("Запас энергии:", $"{npc.Stats.EnergyReserve.FinalValue}", GetStatColor(npc.Stats.EnergyReserve.FinalValue)));
        panel.Children.Add(CreateInfoRow("Контроль:", $"{npc.Stats.Control.FinalValue}", GetStatColor(npc.Stats.Control.FinalValue)));
    }

    private void BuildSocialInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(CreateSectionHeader("СОЦИАЛЬНЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Вера:", $"{npc.Faith:F0}", "#facc15"));
        panel.Children.Add(CreateInfoRow("Уровень последователя:", npc.FollowerLabel, "#d29922"));
        panel.Children.Add(CreateInfoRow("Черты:", string.Join(", ", npc.CharTraits.Select(c => c.ToLabel())), "#d29922"));
        panel.Children.Add(CreateInfoRow("Эмоции:", string.Join(" | ", npc.Emotions.Select(e => $"{e.Name} {e.Percentage:F0}%")), "#e879f9"));
        panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9"));
        panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9"));

        if (npc.Specializations.Any())
            panel.Children.Add(CreateInfoRow("Специализации:", string.Join(", ", npc.Specializations), "#56d364"));
    }

    private UIElement CreateSectionHeader(string title, bool isCollapsible = false)
    {
        if (!isCollapsible)
        {
            return new TextBlock
            {
                Text = title,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 5)
            };
        }

        var expander = new Expander
        {
            Header = title,
            IsExpanded = false,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        };
        return expander;
    }

    private Grid CreateInfoRow(string label, string value, string colorHex)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8b949e")!,
            FontSize = 12,
            Margin = new Thickness(0, 2, 0, 2)
        });

        grid.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex)!,
            FontSize = 12,
            Margin = new Thickness(5, 2, 0, 2)
        });

        Grid.SetColumn(grid.Children[1], 1);
        return grid;
    }

    private string GetStatColor(int value) => value >= 75 ? "#4ade80" : value >= 50 ? "#c9d1d9" : "#fbbf24";

    private void AddAllStats(StackPanel panel, Npc npc)
    {
        AddPhysicalStats(panel, npc);
        AddMentalStats(panel, npc);
        AddEnergyStats(panel, npc);
    }

    private void AddPhysicalStats(StackPanel panel, Npc npc)
    {
        panel.Children.Add(new TextBlock { Text = "ФИЗИЧЕСКИЕ:", Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")! });
        panel.Children.Add(CreateInfoRow("  Выносливость:", $"{npc.Stats.Endurance.FinalValue}", GetStatColor(npc.Stats.Endurance.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Стойкость:", $"{npc.Stats.Toughness.FinalValue}", GetStatColor(npc.Stats.Toughness.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Сила:", $"{npc.Stats.Strength.FinalValue}", GetStatColor(npc.Stats.Strength.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Ловкость:", $"{npc.Stats.Agility.FinalValue}", GetStatColor(npc.Stats.Agility.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Рефлексы:", $"{npc.Stats.Reflexes.FinalValue}", GetStatColor(npc.Stats.Reflexes.FinalValue)));
    }

    private void AddMentalStats(StackPanel panel, Npc npc)
    {
        panel.Children.Add(new TextBlock { Text = "МЕНТАЛЬНЫЕ:", Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!, Margin = new Thickness(0, 10, 0, 0) });
        panel.Children.Add(CreateInfoRow("  Интеллект:", $"{npc.Stats.Intelligence.FinalValue}", GetStatColor(npc.Stats.Intelligence.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Память:", $"{npc.Stats.Memory.FinalValue}", GetStatColor(npc.Stats.Memory.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Логика:", $"{npc.Stats.Logic.FinalValue}", GetStatColor(npc.Stats.Logic.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Воля:", $"{npc.Stats.Will.FinalValue}", GetStatColor(npc.Stats.Will.FinalValue)));
    }

    private void AddEnergyStats(StackPanel panel, Npc npc)
    {
        panel.Children.Add(new TextBlock { Text = "ЭНЕРГЕТИЧЕСКИЕ:", Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!, Margin = new Thickness(0, 10, 0, 0) });
        panel.Children.Add(CreateInfoRow("  Запас энергии:", $"{npc.Stats.EnergyReserve.FinalValue}", GetStatColor(npc.Stats.EnergyReserve.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Контроль:", $"{npc.Stats.Control.FinalValue}", GetStatColor(npc.Stats.Control.FinalValue)));
        panel.Children.Add(CreateInfoRow("  Концентрация:", $"{npc.Stats.Concentration.FinalValue}", GetStatColor(npc.Stats.Concentration.FinalValue)));
    }

    private void CloseSidebar_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}