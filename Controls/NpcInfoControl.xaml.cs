using System;
using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Services;

namespace ApocMinimal.Controls;

public partial class NpcInfoControl : UserControl
{
    private Npc? _currentNpc;
    private string _currentMode = "full";
    private bool _showHeader = false;
    private bool _showCloseButton = false;

    public event Action? Closed;
    public event Action<string>? ModeChanged;

    public NpcInfoControl()
    {
        InitializeComponent();
        ModeFull.IsChecked = true;
    }

    /// <summary>
    /// Показать или скрыть заголовок с кнопкой закрытия
    /// </summary>
    public void SetHeaderVisible(bool visible, string? title = null)
    {
        _showHeader = visible;
        HeaderBorder.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (title != null && visible)
        {
            TitleText.Text = title;
        }
    }

    /// <summary>
    /// Показать или скрыть кнопку закрытия
    /// </summary>
    public void SetCloseButtonVisible(bool visible)
    {
        _showCloseButton = visible;
        CloseButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Установить режим отображения
    /// </summary>
    public void SetMode(string mode)
    {
        _currentMode = mode;

        // Обновляем состояние радио-кнопок
        ModeFull.IsChecked = (mode == "full");
        ModeDetailed.IsChecked = (mode == "detailed");
        ModeCompact.IsChecked = (mode == "compact");
        ModeCombat.IsChecked = (mode == "combat");
        ModeSocial.IsChecked = (mode == "social");

        UpdateContent();
    }

    /// <summary>
    /// Показать информацию об NPC
    /// </summary>
    public void ShowNpc(Npc npc, string? mode = null)
    {
        _currentNpc = npc;
        if (mode != null)
        {
            SetMode(mode);
        }
        else
        {
            UpdateContent();
        }
    }

    /// <summary>
    /// Очистить контент
    /// </summary>
    public void Clear()
    {
        _currentNpc = null;
        ContentPanel.Children.Clear();
    }

    /// <summary>
    /// Скрыть контрол (вызвать событие закрытия)
    /// </summary>
    public void Hide()
    {
        Closed?.Invoke();
    }

    private void Mode_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string mode)
        {
            _currentMode = mode;
            UpdateContent();
            ModeChanged?.Invoke(mode);
        }
    }

    private void UpdateContent()
    {
        if (_currentNpc == null) return;

        ContentPanel.Children.Clear();
        StackPanel infoPanel = BuildInfoPanel(_currentNpc, _currentMode);
        ContentPanel.Children.Add(infoPanel);

        // Прокрутка вверх при смене NPC
        ContentScrollViewer.ScrollToTop();
    }

    private StackPanel BuildInfoPanel(Npc npc, string mode)
    {
        switch (mode)
        {
            case "full":
                return NpcInfoBuilder.BuildFullInfoPanel(npc, false);
            case "detailed":
                return NpcInfoBuilder.BuildFullInfoPanel(npc, true);
            case "compact":
                return NpcInfoBuilder.BuildCompactInfoPanel(npc);
            case "combat":
                return NpcInfoBuilder.BuildCombatInfoPanel(npc);
            case "social":
                return NpcInfoBuilder.BuildSocialInfoPanel(npc);
            default:
                return NpcInfoBuilder.BuildFullInfoPanel(npc, false);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}