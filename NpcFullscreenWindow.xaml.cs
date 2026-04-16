using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;
using ApocMinimal.Models.StatisticsData;
using ApocMinimal.Services;

namespace ApocMinimal
{
    public partial class NpcFullscreenWindow : Window
    {
        private readonly List<Npc> _npcs;
        private Npc? _currentNpc;
        private Npc? _leftNpc;
        private Npc? _rightNpc;
        private string _currentDisplayMode = "full";
        private bool _highlightSame = false;
        private bool _highlightDifferent = false;

        public NpcFullscreenWindow(List<Npc> npcs)
        {
            InitializeComponent();
            _npcs = npcs.Where(n => n.IsAlive).ToList();

            foreach (var npc in _npcs)
            {
                NpcSelector.Items.Add(npc);
                NpcSelectorLeft.Items.Add(npc);
                NpcSelectorRight.Items.Add(npc);
            }

            if (_npcs.Any())
            {
                NpcSelector.SelectedIndex = 0;
                NpcSelectorLeft.SelectedIndex = 0;
                NpcSelectorRight.SelectedIndex = _npcs.Count > 1 ? 1 : 0;
            }

            TabView.Checked += (s, e) => SwitchMode("view");
            TabCompare.Checked += (s, e) => SwitchMode("compare");
        }

        private void SwitchMode(string mode)
        {
            if (mode == "view")
            {
                ViewSelectorPanel.Visibility = Visibility.Visible;
                CompareSelectorPanel.Visibility = Visibility.Collapsed;
                CompareOptions.Visibility = Visibility.Collapsed;
                ViewContent.Visibility = Visibility.Visible;
                CompareContent.Visibility = Visibility.Collapsed;
                UpdateViewContent();
            }
            else
            {
                ViewSelectorPanel.Visibility = Visibility.Collapsed;
                CompareSelectorPanel.Visibility = Visibility.Visible;
                CompareOptions.Visibility = Visibility.Visible;
                ViewContent.Visibility = Visibility.Collapsed;
                CompareContent.Visibility = Visibility.Visible;
                UpdateCompareContent();
            }
        }

        private void NpcSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentNpc = NpcSelector.SelectedItem as Npc;
            UpdateViewContent();
        }

        private void CompareSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _leftNpc = NpcSelectorLeft.SelectedItem as Npc;
            _rightNpc = NpcSelectorRight.SelectedItem as Npc;
            UpdateCompareContent();
        }

        private void DisplayMode_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string mode)
            {
                _currentDisplayMode = mode;
                if (TabView.IsChecked == true)
                    UpdateViewContent();
                else
                    UpdateCompareContent();
            }
        }

        private void CompareOption_Changed(object sender, RoutedEventArgs e)
        {
            _highlightSame = HighlightSame.IsChecked == true;
            _highlightDifferent = HighlightDifferent.IsChecked == true;
            UpdateCompareContent();
        }

        private void UpdateViewContent()
        {
            if (_currentNpc == null) return;
            ViewInfoPanel.Children.Clear();
            var infoPanel = BuildNpcInfoPanel(_currentNpc, _currentDisplayMode, false);
            ViewInfoPanel.Children.Add(infoPanel);
        }

        private void UpdateCompareContent()
        {
            if (_leftNpc == null || _rightNpc == null) return;
            CompareLeftPanel.Children.Clear();
            CompareRightPanel.Children.Clear();
            CompareLeftPanel.Children.Add(BuildNpcInfoPanel(_leftNpc, _currentDisplayMode, true, _rightNpc));
            CompareRightPanel.Children.Add(BuildNpcInfoPanel(_rightNpc, _currentDisplayMode, true, _leftNpc));
        }

        private StackPanel BuildNpcInfoPanel(Npc npc, string mode, bool isCompare = false, Npc? otherNpc = null)
        {
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = $"{npc.Name} [{npc.GenderLabel}] {npc.Age} лет  {npc.Profession}",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
                Margin = new Thickness(0, 0, 0, 15)
            });

            switch (mode)
            {
                case "full": BuildFullInfo(panel, npc, isCompare, otherNpc); break;
                case "detailed": BuildDetailedInfo(panel, npc, isCompare, otherNpc); break;
                case "compact": BuildCompactInfo(panel, npc, isCompare, otherNpc); break;
                case "combat": BuildCombatInfo(panel, npc, isCompare, otherNpc); break;
                case "social": BuildSocialInfo(panel, npc, isCompare, otherNpc); break;
            }

            return panel;
        }

        private void BuildFullInfo(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateSectionHeader("ОСНОВНЫЕ ХАРАКТЕРИСТИКИ"));
            panel.Children.Add(CreateInfoRow("HP:", $"{npc.Health:F0}", npc.Health < 30 ? "#f87171" : "#4ade80", isCompare, otherNpc?.Health, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Выносливость:", $"{npc.Stamina:F0}", npc.Stamina < 30 ? "#f87171" : "#60a5fa", isCompare, otherNpc?.Stamina, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Чакра:", $"{npc.Chakra:F0}", "#e879f9", isCompare, otherNpc?.Chakra, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Вера:", $"{npc.Faith:F0}", "#facc15", isCompare, otherNpc?.Faith, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Страх:", $"{npc.Fear:F0}", npc.Fear > 70 ? "#f87171" : "#c9d1d9", isCompare, otherNpc?.Fear, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9", isCompare, otherNpc?.Trust, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Инициатива:", $"{npc.Initiative:F0}", "#c9d1d9", isCompare, otherNpc?.Initiative, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Уровень:", npc.FollowerLabel, "#d29922", isCompare, otherNpc?.FollowerLabel, (a, b) => a == b));

            panel.Children.Add(CreateSectionHeader("ЛИЧНОСТЬ"));
            panel.Children.Add(CreateInfoRow("Черты:", string.Join(", ", npc.CharTraits.Select(c => c.ToLabel())), "#d29922", isCompare, otherNpc != null ? string.Join(", ", otherNpc.CharTraits.Select(c => c.ToLabel())) : null, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Эмоции:", string.Join(" | ", npc.Emotions.Select(e => $"{e.Name} {e.Percentage:F0}%")), "#e879f9", isCompare, otherNpc != null ? string.Join(" | ", otherNpc.Emotions.Select(e => $"{e.Name} {e.Percentage:F0}%")) : null, (a, b) => a == b));

            panel.Children.Add(CreateSectionHeader("ЦЕЛИ"));
            panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9", isCompare, otherNpc?.Goal, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9", isCompare, otherNpc?.Dream, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Желание:", npc.Desire, "#c9d1d9", isCompare, otherNpc?.Desire, (a, b) => a == b));

            if (npc.Specializations.Any())
                panel.Children.Add(CreateInfoRow("Специализации:", string.Join(", ", npc.Specializations), "#56d364", isCompare, otherNpc != null ? string.Join(", ", otherNpc.Specializations) : null, (a, b) => a == b));

            panel.Children.Add(CreateSectionHeader("ПОТРЕБНОСТИ"));
            foreach (var need in npc.Needs.Where(n => n.IsUrgent || n.IsCritical))
            {
                var otherNeed = otherNpc?.Needs.FirstOrDefault(n => n.Name == need.Name);
                panel.Children.Add(CreateInfoRow($"  {need.Name}:", $"{need.Value:F0}%", need.IsCritical ? "#f87171" : "#fbbf24", isCompare, otherNeed?.Value, (a, b) => a == b));
            }

            panel.Children.Add(CreateSectionHeader("ХАРАКТЕРИСТИКИ", true));
            var statsPanel = new StackPanel { Margin = new Thickness(15, 0, 0, 0) };
            // Используем правильные методы без лишних заголовков
            AddPhysicalStats(statsPanel, npc, isCompare, otherNpc);
            AddMentalStats(statsPanel, npc, isCompare, otherNpc);
            AddEnergyStats(statsPanel, npc, isCompare, otherNpc);
            panel.Children.Add(statsPanel);
        }

        // NpcFullscreenWindow.xaml.cs - исправленный метод BuildDetailedInfo

        private void BuildDetailedInfo(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            // Для режима сравнения показываем обычные статы (без модификаторов)
            if (isCompare)
            {
                panel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ"));
                AddPhysicalStats(panel, npc, isCompare, otherNpc);

                panel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ"));
                AddMentalStats(panel, npc, isCompare, otherNpc);

                panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
                AddEnergyStats(panel, npc, isCompare, otherNpc);
            }
            else
            {
                // Для одиночного просмотра - детальный режим с модификаторами
                AddDetailedStatsToPanel(panel, npc);
            }
        }

        // Добавить этот метод в NpcFullscreenWindow.xaml.cs
        private void AddDetailedStatsToPanel(StackPanel panel, Npc npc)
        {
            // Физические
            panel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ"));
            List<Characteristic> physicalStats = npc.Stats.GetPhysicalStats();
            for (int i = 0; i < physicalStats.Count; i++)
            {
                Characteristic stat = physicalStats[i];
                string combatIcon = stat.IsCombat ? "⚔ " : "  ";
                string socialIcon = stat.IsSocial ? "💬 " : "  ";

                panel.Children.Add(new TextBlock
                {
                    Text = "  " + combatIcon + socialIcon + stat.Name + ":",
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8b949e"),
                    FontSize = 11,
                    Margin = new Thickness(0, 4, 0, 2)
                });

                // Форматируем отклонение со знаком
                string deviationText;
                if (stat.Deviation > 0)
                    deviationText = "+" + stat.Deviation.ToString();
                else if (stat.Deviation < 0)
                    deviationText = stat.Deviation.ToString();
                else
                    deviationText = " 0";

                panel.Children.Add(new TextBlock
                {
                    Text = "      База: " + stat.BaseValue.ToString().PadLeft(3) +
                           " | Откл.: " + deviationText.PadLeft(3) +
                           " | Полн.база: " + stat.FullBase.ToString().PadLeft(3) +
                           " | Итог: " + stat.FinalValue.ToString().PadLeft(3),
                    Foreground = GetStatColorBrush(stat.FinalValue),
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 2)
                });

                List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
                for (int j = 0; j < permMods.Count; j++)
                {
                    PermanentModifier mod = permMods[j];
                    if (mod.IsActive())
                    {
                        string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                        panel.Children.Add(new TextBlock
                        {
                            Text = "        [П] " + mod.Name + ": " + modSign + mod.Value.ToString() + " (" + mod.Source + ")",
                            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#facc15"),
                            FontSize = 10,
                            Margin = new Thickness(0, 0, 0, 1)
                        });
                    }
                }

                List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
                for (int j = 0; j < indMods.Count; j++)
                {
                    IndependentModifier mod = indMods[j];
                    if (mod.IsActive())
                    {
                        string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                        string timeLeft = "";
                        if (mod.TimeUnit == TimeUnit.Days)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " дн.)";
                        }
                        else if (mod.TimeUnit == TimeUnit.Hours)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " ч.)";
                        }
                        else if (mod.TimeUnit == TimeUnit.CombatTurns)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " ходов)";
                        }
                        panel.Children.Add(new TextBlock
                        {
                            Text = "        [В] " + mod.Name + ": " + modSign + mod.Value.ToString() + timeLeft + " (" + mod.Source + ")",
                            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#fbbf24"),
                            FontSize = 10,
                            Margin = new Thickness(0, 0, 0, 1)
                        });
                    }
                }
            }

            // Ментальные
            panel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ"));
            List<Characteristic> mentalStats = npc.Stats.GetMentalStats();
            for (int i = 0; i < mentalStats.Count; i++)
            {
                Characteristic stat = mentalStats[i];
                string combatIcon = stat.IsCombat ? "⚔ " : "  ";
                string socialIcon = stat.IsSocial ? "💬 " : "  ";

                panel.Children.Add(new TextBlock
                {
                    Text = "  " + combatIcon + socialIcon + stat.Name + ":",
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8b949e"),
                    FontSize = 11,
                    Margin = new Thickness(0, 4, 0, 2)
                });

                string deviationText;
                if (stat.Deviation > 0)
                    deviationText = "+" + stat.Deviation.ToString();
                else if (stat.Deviation < 0)
                    deviationText = stat.Deviation.ToString();
                else
                    deviationText = " 0";

                panel.Children.Add(new TextBlock
                {
                    Text = "      База: " + stat.BaseValue.ToString().PadLeft(3) +
                           " | Откл.: " + deviationText.PadLeft(3) +
                           " | Полн.база: " + stat.FullBase.ToString().PadLeft(3) +
                           " | Итог: " + stat.FinalValue.ToString().PadLeft(3),
                    Foreground = GetStatColorBrush(stat.FinalValue),
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 2)
                });

                List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
                for (int j = 0; j < permMods.Count; j++)
                {
                    PermanentModifier mod = permMods[j];
                    if (mod.IsActive())
                    {
                        string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                        panel.Children.Add(new TextBlock
                        {
                            Text = "        [П] " + mod.Name + ": " + modSign + mod.Value.ToString() + " (" + mod.Source + ")",
                            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#facc15"),
                            FontSize = 10,
                            Margin = new Thickness(0, 0, 0, 1)
                        });
                    }
                }

                List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
                for (int j = 0; j < indMods.Count; j++)
                {
                    IndependentModifier mod = indMods[j];
                    if (mod.IsActive())
                    {
                        string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                        string timeLeft = "";
                        if (mod.TimeUnit == TimeUnit.Days)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " дн.)";
                        }
                        else if (mod.TimeUnit == TimeUnit.Hours)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " ч.)";
                        }
                        else if (mod.TimeUnit == TimeUnit.CombatTurns)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " ходов)";
                        }
                        panel.Children.Add(new TextBlock
                        {
                            Text = "        [В] " + mod.Name + ": " + modSign + mod.Value.ToString() + timeLeft + " (" + mod.Source + ")",
                            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#fbbf24"),
                            FontSize = 10,
                            Margin = new Thickness(0, 0, 0, 1)
                        });
                    }
                }
            }

            // Энергетические
            panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
            List<Characteristic> energyStats = npc.Stats.GetEnergyStats();
            for (int i = 0; i < energyStats.Count; i++)
            {
                Characteristic stat = energyStats[i];

                panel.Children.Add(new TextBlock
                {
                    Text = "  " + stat.Name + ":",
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8b949e"),
                    FontSize = 11,
                    Margin = new Thickness(0, 4, 0, 2)
                });

                string deviationText;
                if (stat.Deviation > 0)
                    deviationText = "+" + stat.Deviation.ToString();
                else if (stat.Deviation < 0)
                    deviationText = stat.Deviation.ToString();
                else
                    deviationText = " 0";

                panel.Children.Add(new TextBlock
                {
                    Text = "      База: " + stat.BaseValue.ToString().PadLeft(3) +
                           " | Откл.: " + deviationText.PadLeft(3) +
                           " | Полн.база: " + stat.FullBase.ToString().PadLeft(3) +
                           " | Итог: " + stat.FinalValue.ToString().PadLeft(3),
                    Foreground = GetStatColorBrush(stat.FinalValue),
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 2)
                });

                List<PermanentModifier> permMods = stat.GetModifiersByType<PermanentModifier>();
                for (int j = 0; j < permMods.Count; j++)
                {
                    PermanentModifier mod = permMods[j];
                    if (mod.IsActive())
                    {
                        string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                        panel.Children.Add(new TextBlock
                        {
                            Text = "        [П] " + mod.Name + ": " + modSign + mod.Value.ToString() + " (" + mod.Source + ")",
                            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#facc15"),
                            FontSize = 10,
                            Margin = new Thickness(0, 0, 0, 1)
                        });
                    }
                }

                List<IndependentModifier> indMods = stat.GetModifiersByType<IndependentModifier>();
                for (int j = 0; j < indMods.Count; j++)
                {
                    IndependentModifier mod = indMods[j];
                    if (mod.IsActive())
                    {
                        string modSign = (mod.Type == ModifierType.Additive) ? "+" : "×";
                        string timeLeft = "";
                        if (mod.TimeUnit == TimeUnit.Days)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " дн.)";
                        }
                        else if (mod.TimeUnit == TimeUnit.Hours)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " ч.)";
                        }
                        else if (mod.TimeUnit == TimeUnit.CombatTurns)
                        {
                            timeLeft = " (ост. " + mod.Remaining.ToString() + " ходов)";
                        }
                        panel.Children.Add(new TextBlock
                        {
                            Text = "        [В] " + mod.Name + ": " + modSign + mod.Value.ToString() + timeLeft + " (" + mod.Source + ")",
                            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#fbbf24"),
                            FontSize = 10,
                            Margin = new Thickness(0, 0, 0, 1)
                        });
                    }
                }
            }
        }
        private SolidColorBrush GetStatColorBrush(int value)
        {
            string hex = GetStatColor(value);
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
        }

        private void BuildCompactInfo(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"❤{npc.Health:F0} ✦{npc.Faith:F0} 😨{npc.Fear:F0} 🤝{npc.Trust:F0} 💪{npc.Stats.Strength} 🧠{npc.Stats.Intelligence}",
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#c9d1d9")!,
                FontSize = 14,
                Margin = new Thickness(0, 5, 0, 5)
            });

            var criticalNeeds = npc.Needs.Where(n => n.IsCritical).ToList();
            if (criticalNeeds.Any())
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"⚠ КРИТИЧНО: {string.Join(", ", criticalNeeds.Select(n => n.Name))}",
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#f87171")!,
                    FontSize = 12
                });
            }
        }

        private void BuildCombatInfo(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateSectionHeader("БОЕВЫЕ ХАРАКТЕРИСТИКИ"));
            AddCombatStats(panel, npc, isCompare, otherNpc);
            panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
            AddEnergyCombatStats(panel, npc, isCompare, otherNpc);
        }

        private void BuildSocialInfo(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateSectionHeader("СОЦИАЛЬНЫЕ ХАРАКТЕРИСТИКИ"));
            panel.Children.Add(CreateInfoRow("Доверие:", $"{npc.Trust:F0}", npc.Trust > 70 ? "#4ade80" : "#c9d1d9", isCompare, otherNpc?.Trust, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Вера:", $"{npc.Faith:F0}", "#facc15", isCompare, otherNpc?.Faith, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Уровень последователя:", npc.FollowerLabel, "#d29922", isCompare, otherNpc?.FollowerLabel, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Черты:", string.Join(", ", npc.CharTraits.Select(c => c.ToLabel())), "#d29922", isCompare, otherNpc != null ? string.Join(", ", otherNpc.CharTraits.Select(c => c.ToLabel())) : null, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Эмоции:", string.Join(" | ", npc.Emotions.Select(e => $"{e.Name} {e.Percentage:F0}%")), "#e879f9", isCompare, otherNpc != null ? string.Join(" | ", otherNpc.Emotions.Select(e => $"{e.Name} {e.Percentage:F0}%")) : null, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Цель:", npc.Goal, "#c9d1d9", isCompare, otherNpc?.Goal, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Мечта:", npc.Dream, "#c9d1d9", isCompare, otherNpc?.Dream, (a, b) => a == b));

            if (npc.Specializations.Any())
                panel.Children.Add(CreateInfoRow("Специализации:", string.Join(", ", npc.Specializations), "#56d364", isCompare, otherNpc != null ? string.Join(", ", otherNpc.Specializations) : null, (a, b) => a == b));
        }

        private void AddCombatStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            var stats = npc.Stats.GetCombatStats();
            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                int otherValue = otherNpc?.Stats.GetStatValue(stat.Name) ?? 0;
                panel.Children.Add(CreateInfoRow(
                    $"  {stat.Name}:",
                    $"{stat.FinalValue}",
                    GetStatColor(stat.FinalValue),
                    isCompare,
                    otherValue,
                    (a, b) => a?.ToString() == b?.ToString()));
            }
            panel.Children.Add(CreateInfoRow("Боевая инициатива:", $"{npc.CombatInitiative:F0}", "#c9d1d9", isCompare, otherNpc?.CombatInitiative, (a, b) => a?.ToString() == b?.ToString()));
        }

        private void AddEnergyCombatStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateInfoRow("Запас энергии:", $"{npc.Stats.EnergyReserve}", GetStatColor(npc.Stats.EnergyReserve), isCompare, otherNpc?.Stats.EnergyReserve, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Контроль:", $"{npc.Stats.Control}", GetStatColor(npc.Stats.Control), isCompare, otherNpc?.Stats.Control, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Выход:", $"{npc.Stats.Output}", GetStatColor(npc.Stats.Output), isCompare, otherNpc?.Stats.Output, (a, b) => a == b));
        }

        private UIElement CreateSectionHeader(string title, bool isCollapsible = false)
        {
            if (!isCollapsible)
            {
                return new TextBlock
                {
                    Text = title,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 15, 0, 8)
                };
            }

            return new Expander
            {
                Header = title,
                IsExpanded = false,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 0)
            };
        }

        private Grid CreateInfoRow(string label, string value, string defaultColor, bool isCompare = false, object? otherValue = null, Func<object?, object?, bool>? compareFunc = null)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8b949e")!,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 2)
            });

            string finalColor = defaultColor;
            if (isCompare && otherValue != null && compareFunc != null)
            {
                bool isEqual = compareFunc(value, otherValue);
                if (_highlightSame && isEqual) finalColor = "#4ade80";
                else if (_highlightDifferent && !isEqual) finalColor = "#f87171";
            }

            grid.Children.Add(new TextBlock
            {
                Text = value,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(finalColor)!,
                FontSize = 12,
                Margin = new Thickness(5, 2, 0, 2)
            });

            Grid.SetColumn(grid.Children[1], 1);
            return grid;
        }

        private string GetStatColor(int value) => NpcInfoBuilder.GetStatColor(value);

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void AddAllStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            AddPhysicalStats(panel, npc, isCompare, otherNpc);
            AddMentalStats(panel, npc, isCompare, otherNpc);
            AddEnergyStats(panel, npc, isCompare, otherNpc);
        }

        // NpcFullscreenWindow.xaml.cs - исправленные методы

        private void AddPhysicalStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            var stats = npc.Stats.GetPhysicalStats();
            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                int otherValue = otherNpc?.Stats.GetStatValue(stat.Name) ?? 0;
                // Убираем лишние пробелы перед названием
                panel.Children.Add(CreateInfoRow(stat.Name + ":", stat.FinalValue.ToString(), GetStatColor(stat.FinalValue), isCompare, otherValue, (a, b) => a?.ToString() == b?.ToString()));
            }
        }

        private void AddMentalStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            var stats = npc.Stats.GetMentalStats();
            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                int otherValue = otherNpc?.Stats.GetStatValue(stat.Name) ?? 0;
                panel.Children.Add(CreateInfoRow(stat.Name + ":", stat.FinalValue.ToString(), GetStatColor(stat.FinalValue), isCompare, otherValue, (a, b) => a?.ToString() == b?.ToString()));
            }
        }

        private void AddEnergyStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            var stats = npc.Stats.GetEnergyStats();
            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                int otherValue = otherNpc?.Stats.GetStatValue(stat.Name) ?? 0;
                panel.Children.Add(CreateInfoRow(stat.Name + ":", stat.FinalValue.ToString(), GetStatColor(stat.FinalValue), isCompare, otherValue, (a, b) => a?.ToString() == b?.ToString()));
            }
        }
    }
}
