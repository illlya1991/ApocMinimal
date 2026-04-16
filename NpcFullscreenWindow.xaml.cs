using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;

namespace ApocMinimal
{
    public partial class NpcFullscreenWindow : Window
    {
        private readonly List<Npc> _npcs;
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

            // Подписываемся на события
            TabView.Checked += (s, e) => SwitchMode("view");
            TabCompare.Checked += (s, e) => SwitchMode("compare");
            NpcSelector.SelectionChanged += (s, e) => UpdateSingleView();
            NpcSelectorLeft.SelectionChanged += (s, e) => UpdateCompareView();
            NpcSelectorRight.SelectionChanged += (s, e) => UpdateCompareView();
            HighlightSame.Checked += (s, e) => { _highlightSame = true; UpdateCompareView(); };
            HighlightSame.Unchecked += (s, e) => { _highlightSame = false; UpdateCompareView(); };
            HighlightDifferent.Checked += (s, e) => { _highlightDifferent = true; UpdateCompareView(); };
            HighlightDifferent.Unchecked += (s, e) => { _highlightDifferent = false; UpdateCompareView(); };

            // Отключаем заголовки у внутренних контролов (они не нужны в полноэкранном режиме)
            SingleNpcInfo.SetHeaderVisible(false);
            LeftNpcInfo.SetHeaderVisible(false);
            RightNpcInfo.SetHeaderVisible(false);

            // Синхронизация режимов при сравнении
            bool _syncing = false;
            LeftNpcInfo.ModeChanged += mode => { if (!_syncing) { _syncing = true; RightNpcInfo.SetMode(mode); _syncing = false; } };
            RightNpcInfo.ModeChanged += mode => { if (!_syncing) { _syncing = true; LeftNpcInfo.SetMode(mode); _syncing = false; } };

            // Обновляем отображение
            UpdateSingleView();
        }

        private void SwitchMode(string mode)
        {
            if (mode == "view")
            {
                ViewSelectorPanel.Visibility = Visibility.Visible;
                CompareSelectorPanel.Visibility = Visibility.Collapsed;
                CompareOptions.Visibility = Visibility.Collapsed;
                SingleNpcInfo.Visibility = Visibility.Visible;
                CompareContent.Visibility = Visibility.Collapsed;
                UpdateSingleView();
            }
            else
            {
                ViewSelectorPanel.Visibility = Visibility.Collapsed;
                CompareSelectorPanel.Visibility = Visibility.Visible;
                CompareOptions.Visibility = Visibility.Visible;
                SingleNpcInfo.Visibility = Visibility.Collapsed;
                CompareContent.Visibility = Visibility.Visible;
                UpdateCompareView();
            }
        }

        private void UpdateSingleView()
        {
            if (NpcSelector.SelectedItem is Npc npc)
            {
                SingleNpcInfo.ShowNpc(npc);
            }
        }

        private void UpdateCompareView()
        {
            if (NpcSelectorLeft.SelectedItem is Npc leftNpc && NpcSelectorRight.SelectedItem is Npc rightNpc)
            {
                LeftNpcInfo.SetCompareContext(rightNpc, _highlightSame, _highlightDifferent);
                RightNpcInfo.SetCompareContext(leftNpc, _highlightSame, _highlightDifferent);
                LeftNpcInfo.ShowNpc(leftNpc);
                RightNpcInfo.ShowNpc(rightNpc);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}