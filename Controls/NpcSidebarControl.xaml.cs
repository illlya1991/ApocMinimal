using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Controls;

namespace ApocMinimal.Controls;

public partial class NpcSidebarControl : UserControl
{
    private Npc? _currentNpc;

    public event Action? Closed;
    public new bool IsVisible => SidebarRoot.Visibility == Visibility.Visible;
    public Npc? CurrentNpc => _currentNpc;

    public NpcSidebarControl()
    {
        InitializeComponent();

        NpcInfo.SetHeaderVisible(true, "ИНФОРМАЦИЯ О NPC");
        NpcInfo.SetCloseButtonVisible(true);
        NpcInfo.Closed += () => Hide();
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
        anim.Completed += (s, e) => SidebarRoot.Visibility = Visibility.Collapsed;
        SidebarRoot.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public void ShowNpc(Npc npc, string? mode = null)
    {
        _currentNpc = npc;
        NpcInfo.ShowNpc(npc, mode);
        SidebarRoot.Visibility = Visibility.Visible;
        SidebarRoot.Opacity = 0;
        AnimateShow();
    }

    public void Hide()
    {
        if (!IsVisible) return;
        _currentNpc = null;
        NpcInfo.Clear();
        AnimateHide();
        Closed?.Invoke();
    }

    public void Toggle(Npc npc, string? mode = null)
    {
        if (_currentNpc == npc && IsVisible)
            Hide();
        else
            ShowNpc(npc, mode);
    }
}