using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Models.PersonData.NpcData;

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

            // Заполняем комбобоксы
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

            // Подписываемся на переключение вкладок
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

            var leftPanel = BuildNpcInfoPanel(_leftNpc, _currentDisplayMode, true, _rightNpc);
            var rightPanel = BuildNpcInfoPanel(_rightNpc, _currentDisplayMode, true, _leftNpc);

            CompareLeftPanel.Children.Add(leftPanel);
            CompareRightPanel.Children.Add(rightPanel);
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
                case "full":
                    BuildFullInfo(panel, npc, isCompare, otherNpc);
                    break;
                case "detailed":
                    BuildDetailedInfo(panel, npc, isCompare, otherNpc);
                    break;
                case "compact":
                    BuildCompactInfo(panel, npc, isCompare, otherNpc);
                    break;
                case "combat":
                    BuildCombatInfo(panel, npc, isCompare, otherNpc);
                    break;
                case "social":
                    BuildSocialInfo(panel, npc, isCompare, otherNpc);
                    break;
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
            {
                panel.Children.Add(CreateInfoRow("Специализации:", string.Join(", ", npc.Specializations), "#56d364", isCompare, otherNpc != null ? string.Join(", ", otherNpc.Specializations) : null, (a, b) => a == b));
            }

            panel.Children.Add(CreateSectionHeader("ПОТРЕБНОСТИ"));
            foreach (var need in npc.Needs.Where(n => n.IsUrgent || n.IsCritical))
            {
                var otherNeed = otherNpc?.Needs.FirstOrDefault(n => n.Name == need.Name);
                panel.Children.Add(CreateInfoRow($"  {need.Name}:", $"{need.Value:F0}%", need.IsCritical ? "#f87171" : "#fbbf24", isCompare, otherNeed?.Value, (a, b) => a == b));
            }

            panel.Children.Add(CreateSectionHeader("ХАРАКТЕРИСТИКИ", true));
            var statsPanel = new StackPanel { Margin = new Thickness(15, 0, 0, 0) };
            AddAllStats(statsPanel, npc, isCompare, otherNpc);
            panel.Children.Add(statsPanel);
        }

        private void BuildDetailedInfo(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateSectionHeader("ФИЗИЧЕСКИЕ"));
            AddPhysicalStats(panel, npc, isCompare, otherNpc);

            panel.Children.Add(CreateSectionHeader("МЕНТАЛЬНЫЕ"));
            AddMentalStats(panel, npc, isCompare, otherNpc);

            panel.Children.Add(CreateSectionHeader("ЭНЕРГЕТИЧЕСКИЕ"));
            AddEnergyStats(panel, npc, isCompare, otherNpc);
        }

        private void BuildCompactInfo(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"❤{npc.Health:F0} ✦{npc.Faith:F0} 😨{npc.Fear:F0} 🤝{npc.Trust:F0} 💪{npc.Stats.Strength.FinalValue} 🧠{npc.Stats.Intelligence.FinalValue}",
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

        private void AddPhysicalStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateInfoRow("  Выносливость:", $"{npc.Stats.Endurance.FinalValue}", GetStatColor(npc.Stats.Endurance.FinalValue), isCompare, otherNpc?.Stats.Endurance.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Стойкость:", $"{npc.Stats.Toughness.FinalValue}", GetStatColor(npc.Stats.Toughness.FinalValue), isCompare, otherNpc?.Stats.Toughness.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Сила:", $"{npc.Stats.Strength.FinalValue}", GetStatColor(npc.Stats.Strength.FinalValue), isCompare, otherNpc?.Stats.Strength.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Восст.(физ):", $"{npc.Stats.RecoveryPhys.FinalValue}", GetStatColor(npc.Stats.RecoveryPhys.FinalValue), isCompare, otherNpc?.Stats.RecoveryPhys.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Рефлексы:", $"{npc.Stats.Reflexes.FinalValue}", GetStatColor(npc.Stats.Reflexes.FinalValue), isCompare, otherNpc?.Stats.Reflexes.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Ловкость:", $"{npc.Stats.Agility.FinalValue}", GetStatColor(npc.Stats.Agility.FinalValue), isCompare, otherNpc?.Stats.Agility.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Адаптация:", $"{npc.Stats.Adaptation.FinalValue}", GetStatColor(npc.Stats.Adaptation.FinalValue), isCompare, otherNpc?.Stats.Adaptation.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Регенерация:", $"{npc.Stats.Regeneration.FinalValue}", GetStatColor(npc.Stats.Regeneration.FinalValue), isCompare, otherNpc?.Stats.Regeneration.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Сенсорика:", $"{npc.Stats.Sensorics.FinalValue}", GetStatColor(npc.Stats.Sensorics.FinalValue), isCompare, otherNpc?.Stats.Sensorics.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Долголетие:", $"{npc.Stats.Longevity.FinalValue}", GetStatColor(npc.Stats.Longevity.FinalValue), isCompare, otherNpc?.Stats.Longevity.FinalValue, (a, b) => a == b));
        }

        private void AddMentalStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateInfoRow("  Фокус:", $"{npc.Stats.Focus.FinalValue}", GetStatColor(npc.Stats.Focus.FinalValue), isCompare, otherNpc?.Stats.Focus.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Память:", $"{npc.Stats.Memory.FinalValue}", GetStatColor(npc.Stats.Memory.FinalValue), isCompare, otherNpc?.Stats.Memory.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Логика:", $"{npc.Stats.Logic.FinalValue}", GetStatColor(npc.Stats.Logic.FinalValue), isCompare, otherNpc?.Stats.Logic.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Дедукция:", $"{npc.Stats.Deduction.FinalValue}", GetStatColor(npc.Stats.Deduction.FinalValue), isCompare, otherNpc?.Stats.Deduction.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Интеллект:", $"{npc.Stats.Intelligence.FinalValue}", GetStatColor(npc.Stats.Intelligence.FinalValue), isCompare, otherNpc?.Stats.Intelligence.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Воля:", $"{npc.Stats.Will.FinalValue}", GetStatColor(npc.Stats.Will.FinalValue), isCompare, otherNpc?.Stats.Will.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Обучение:", $"{npc.Stats.Learning.FinalValue}", GetStatColor(npc.Stats.Learning.FinalValue), isCompare, otherNpc?.Stats.Learning.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Гибкость:", $"{npc.Stats.Flexibility.FinalValue}", GetStatColor(npc.Stats.Flexibility.FinalValue), isCompare, otherNpc?.Stats.Flexibility.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Интуиция:", $"{npc.Stats.Intuition.FinalValue}", GetStatColor(npc.Stats.Intuition.FinalValue), isCompare, otherNpc?.Stats.Intuition.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Соц.интеллект:", $"{npc.Stats.SocialIntel.FinalValue}", GetStatColor(npc.Stats.SocialIntel.FinalValue), isCompare, otherNpc?.Stats.SocialIntel.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Творчество:", $"{npc.Stats.Creativity.FinalValue}", GetStatColor(npc.Stats.Creativity.FinalValue), isCompare, otherNpc?.Stats.Creativity.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Математика:", $"{npc.Stats.Mathematics.FinalValue}", GetStatColor(npc.Stats.Mathematics.FinalValue), isCompare, otherNpc?.Stats.Mathematics.FinalValue, (a, b) => a == b));
        }

        private void AddEnergyStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateInfoRow("  Запас энергии:", $"{npc.Stats.EnergyReserve.FinalValue}", GetStatColor(npc.Stats.EnergyReserve.FinalValue), isCompare, otherNpc?.Stats.EnergyReserve.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Восст.энергии:", $"{npc.Stats.EnergyRecovery.FinalValue}", GetStatColor(npc.Stats.EnergyRecovery.FinalValue), isCompare, otherNpc?.Stats.EnergyRecovery.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Контроль:", $"{npc.Stats.Control.FinalValue}", GetStatColor(npc.Stats.Control.FinalValue), isCompare, otherNpc?.Stats.Control.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Концентрация:", $"{npc.Stats.Concentration.FinalValue}", GetStatColor(npc.Stats.Concentration.FinalValue), isCompare, otherNpc?.Stats.Concentration.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Выход:", $"{npc.Stats.Output.FinalValue}", GetStatColor(npc.Stats.Output.FinalValue), isCompare, otherNpc?.Stats.Output.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Тонкость:", $"{npc.Stats.Precision.FinalValue}", GetStatColor(npc.Stats.Precision.FinalValue), isCompare, otherNpc?.Stats.Precision.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Устойчивость:", $"{npc.Stats.EnergyResist.FinalValue}", GetStatColor(npc.Stats.EnergyResist.FinalValue), isCompare, otherNpc?.Stats.EnergyResist.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("  Восприятие:", $"{npc.Stats.EnergySense.FinalValue}", GetStatColor(npc.Stats.EnergySense.FinalValue), isCompare, otherNpc?.Stats.EnergySense.FinalValue, (a, b) => a == b));
        }

        private void AddCombatStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateInfoRow("Сила:", $"{npc.Stats.Strength.FinalValue}", GetStatColor(npc.Stats.Strength.FinalValue), isCompare, otherNpc?.Stats.Strength.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Ловкость:", $"{npc.Stats.Agility.FinalValue}", GetStatColor(npc.Stats.Agility.FinalValue), isCompare, otherNpc?.Stats.Agility.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Выносливость:", $"{npc.Stats.Endurance.FinalValue}", GetStatColor(npc.Stats.Endurance.FinalValue), isCompare, otherNpc?.Stats.Endurance.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Стойкость:", $"{npc.Stats.Toughness.FinalValue}", GetStatColor(npc.Stats.Toughness.FinalValue), isCompare, otherNpc?.Stats.Toughness.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Рефлексы:", $"{npc.Stats.Reflexes.FinalValue}", GetStatColor(npc.Stats.Reflexes.FinalValue), isCompare, otherNpc?.Stats.Reflexes.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Боевая инициатива:", $"{npc.CombatInitiative:F0}", "#c9d1d9", isCompare, otherNpc?.CombatInitiative, (a, b) => a == b));
        }

        private void AddEnergyCombatStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            panel.Children.Add(CreateInfoRow("Запас энергии:", $"{npc.Stats.EnergyReserve.FinalValue}", GetStatColor(npc.Stats.EnergyReserve.FinalValue), isCompare, otherNpc?.Stats.EnergyReserve.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Контроль:", $"{npc.Stats.Control.FinalValue}", GetStatColor(npc.Stats.Control.FinalValue), isCompare, otherNpc?.Stats.Control.FinalValue, (a, b) => a == b));
            panel.Children.Add(CreateInfoRow("Выход:", $"{npc.Stats.Output.FinalValue}", GetStatColor(npc.Stats.Output.FinalValue), isCompare, otherNpc?.Stats.Output.FinalValue, (a, b) => a == b));
        }

        private void AddAllStats(StackPanel panel, Npc npc, bool isCompare, Npc? otherNpc)
        {
            AddPhysicalStats(panel, npc, isCompare, otherNpc);
            AddMentalStats(panel, npc, isCompare, otherNpc);
            AddEnergyStats(panel, npc, isCompare, otherNpc);
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

            var expander = new Expander
            {
                Header = title,
                IsExpanded = false,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#60a5fa")!,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 0)
            };
            return expander;
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

                if (_highlightSame && isEqual)
                    finalColor = "#4ade80";
                else if (_highlightDifferent && !isEqual)
                    finalColor = "#f87171";
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

        private string GetStatColor(int value) => value >= 75 ? "#4ade80" : value >= 50 ? "#c9d1d9" : "#fbbf24";

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}