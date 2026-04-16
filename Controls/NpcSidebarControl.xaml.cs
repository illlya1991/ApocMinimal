using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Services;

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
    public new bool IsVisible => SidebarRoot.Visibility == Visibility.Visible;
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
        rb.Checked += RadioButton_Checked;
        return rb;
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        UpdateContent();
    }

    private void AnimateShow()
    {
        var anim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        SidebarRoot.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private void AnimateHide()
    {
        var anim = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += HideAnimation_Completed;
        SidebarRoot.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private void HideAnimation_Completed(object? sender, EventArgs e)
    {
        SidebarRoot.Visibility = Visibility.Collapsed;
    }

    public void ShowNpc(Npc npc)
    {
        _currentNpc = npc;
        SidebarTitle.Text = $"ИНФОРМАЦИЯ: {npc.Name}";
        SidebarRoot.Visibility = Visibility.Visible;
        SidebarRoot.Opacity = 0;
        UpdateContent();
        AnimateShow();
    }

    public void Hide()
    {
        if (!IsVisible) return;
        _currentNpc = null;
        AnimateHide();
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

        if (mode == "full") BuildFullInfo(panel, npc);
        else if (mode == "detailed") BuildDetailedInfo(panel, npc);
        else if (mode == "compact") BuildCompactInfo(panel, npc);
        else if (mode == "combat") BuildCombatInfo(panel, npc);
        else if (mode == "social") BuildSocialInfo(panel, npc);

        return panel;
    }

    private void BuildFullInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("ОСНОВНЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("HP:", $"{npc.Health:F0}", npc.Health < 30 ? "#f87171" : "#4ade80"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Выносливость:", $"{npc.Stamina:F0}", npc.Stamina < 30 ? "#f87171" : "#60a5fa"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Чакра:", $"{npc.Chakra:F0}", "#e879f9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Вера:", $"{npc.Faith:F0}", "#facc15"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Страх:", $"{npc.Fear:F0}", npc.Fear > 70 ? "#f87171" : "#c9d1d9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Инициатива:", $"{npc.Initiative:F0}", "#c9d1d9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Уровень:", npc.FollowerLabel, "#d29922"));

        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("ЛИЧНОСТЬ"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Черты:", NpcInfoBuilder.BuildTraitsString(npc.CharTraits), "#d29922"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Эмоции:", NpcInfoBuilder.BuildEmotionsString(npc.Emotions), "#e879f9"));

        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("ЦЕЛИ"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Цель:", npc.Goal, "#c9d1d9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Желание:", npc.Desire, "#c9d1d9"));

        if (npc.Specializations.Count > 0)
            panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Специализации:", string.Join(", ", npc.Specializations), "#56d364"));

        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("ПОТРЕБНОСТИ (срочные)"));
        for (int i = 0; i < npc.Needs.Count; i++)
        {
            var need = npc.Needs[i];
            if (!need.IsUrgent && !need.IsCritical) continue;
            panel.Children.Add(NpcInfoBuilder.CreateInfoRow(
                $"  {need.Name}:",
                $"{need.Value:F0}%",
                need.IsCritical ? "#f87171" : "#fbbf24"));
        }

        var statsExpander = new Expander
        {
            Header = "ХАРАКТЕРИСТИКИ",
            IsExpanded = false,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 5)
        };
        var statsInner = new StackPanel { Margin = new Thickness(15, 0, 0, 0) };
        NpcInfoBuilder.AddAllStats(statsInner, npc);
        statsExpander.Content = statsInner;
        panel.Children.Add(statsExpander);
    }

    private void BuildDetailedInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("ФИЗИЧЕСКИЕ"));
        NpcInfoBuilder.AddPhysicalStats(panel, npc);
        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("МЕНТАЛЬНЫЕ"));
        NpcInfoBuilder.AddMentalStats(panel, npc);
        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
        NpcInfoBuilder.AddEnergyStats(panel, npc);
    }

    private void BuildCompactInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(new TextBlock
        {
            Text = $"❤{npc.Health:F0} ✦{npc.Faith:F0} 😨{npc.Fear:F0} 🤝{npc.Trust:F0} 💪{npc.Stats.Strength} 🧠{npc.Stats.Intelligence}",
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#c9d1d9")!,
            FontSize = 12,
            Margin = new Thickness(0, 5, 0, 5)
        });

        var criticalNeeds = new List<Need>();
        for (int i = 0; i < npc.Needs.Count; i++)
            if (npc.Needs[i].IsCritical) criticalNeeds.Add(npc.Needs[i]);

        if (criticalNeeds.Count > 0)
        {
            var sb = new System.Text.StringBuilder("⚠ КРИТИЧНО: ");
            for (int i = 0; i < criticalNeeds.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(criticalNeeds[i].Name);
            }
            panel.Children.Add(new TextBlock
            {
                Text = sb.ToString(),
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#f87171")!,
                FontSize = 11
            });
        }
    }

    private void BuildCombatInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("БОЕВЫЕ ХАРАКТЕРИСТИКИ"));
        NpcInfoBuilder.AddCombatStats(panel, npc);
        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
        NpcInfoBuilder.AddEnergyStats(panel, npc);
    }

    private void BuildSocialInfo(StackPanel panel, Npc npc)
    {
        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("СОЦИАЛЬНЫЕ ХАРАКТЕРИСТИКИ"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Вера:", $"{npc.Faith:F0}", "#facc15"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Уровень последователя:", npc.FollowerLabel, "#d29922"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Черты:", NpcInfoBuilder.BuildTraitsString(npc.CharTraits), "#d29922"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Эмоции:", NpcInfoBuilder.BuildEmotionsString(npc.Emotions), "#e879f9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Цель:", npc.Goal, "#c9d1d9"));
        panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9"));

        if (npc.Specializations.Count > 0)
            panel.Children.Add(NpcInfoBuilder.CreateInfoRow("Специализации:", string.Join(", ", npc.Specializations), "#56d364"));

        panel.Children.Add(NpcInfoBuilder.CreateSectionHeader("СОЦИАЛЬНЫЕ СТАТЫ"));
        NpcInfoBuilder.AddSocialStats(panel, npc);
    }

    private void CloseSidebar_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
