using ApocMinimal.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ApocMinimal
{
    public partial class SaveSelectionWindow : Window
    {
        public OneSave SelectedSave { get; private set; }
        public bool IsCanceled { get; private set; } = true;

        private readonly string _mode;
        private readonly List<OneSave> _saves;
        private readonly DatabaseManager _db;

        private OneSave _pendingOverwrite;
        private OneSave _pendingDelete;
        private StackPanel _pendingDeleteContainer;

        public SaveSelectionWindow(List<OneSave> saves, string mode, DatabaseManager db)
        {
            InitializeComponent();
            _mode = mode;
            _saves = saves;
            _db = db;

            if (mode == "new")
            {
                TitleText.Text = "Новая игра";
                DescriptionText.Text = "Выберите слот для новой игры:";
            }
            else
            {
                TitleText.Text = "Загрузить игру";
                DescriptionText.Text = "Выберите сохранение для продолжения:";
            }

            CreateSaveButtons(saves);
        }

        private void CreateSaveButtons(List<OneSave> saves)
        {
            SavesPanel.Children.Clear();

            if (saves == null || saves.Count == 0)
            {
                SavesPanel.Children.Add(new TextBlock
                {
                    Text = "Нет доступных сохранений",
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#8b949e"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 16, 0, 0)
                });
                return;
            }

            for (int i = 0; i < saves.Count; i++)
            {
                var save = saves[i];
                int idx = i;
                string slotName = $"Слот {i + 1}";
                string saveName = Path.GetFileNameWithoutExtension(save._fileName);

                var container = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };

                // Основная кнопка
                var saveBtn = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString(save._active ? "#0f2a1a" : "#111827"),
                    BorderBrush = (Brush)new BrushConverter().ConvertFromString(save._active ? "#2a6040" : "#1e2a3a"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand,
                    Padding = new Thickness(14, 10, 14, 10)
                };

                var saveContent = new Grid();
                saveContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                saveContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameBlock = new TextBlock
                {
                    Text = slotName,
                    Foreground = (Brush)new BrushConverter().ConvertFromString(save._active ? "#4ade80" : "#8b949e"),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold
                };

                var statusBlock = new TextBlock
                {
                    Text = save._active ? "активно" : "пусто",
                    Foreground = (Brush)new BrushConverter().ConvertFromString(save._active ? "#4ade80" : "#555"),
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(statusBlock, 1);

                saveContent.Children.Add(nameBlock);
                saveContent.Children.Add(statusBlock);
                saveBtn.Child = saveContent;

                saveBtn.MouseLeftButtonUp += (s, e) => OnSaveSelected(saves[idx]);
                container.Children.Add(saveBtn);

                // Кнопка удаления (только в режиме continue для активных слотов)
                if (_mode == "continue" && save._active)
                {
                    var delBtn = new Button
                    {
                        Content = "Удалить сохранение",
                        Height = 26, FontSize = 11,
                        Background = (Brush)new BrushConverter().ConvertFromString("#1e1010"),
                        Foreground = (Brush)new BrushConverter().ConvertFromString("#f87171"),
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(0, 2, 0, 0),
                        Tag = (save, container)
                    };
                    delBtn.Click += (s, e) =>
                    {
                        _pendingDelete = saves[idx];
                        _pendingDeleteContainer = container;
                        DeleteConfirmText.Text = $"Удалить «{slotName}»? Это действие нельзя отменить.";
                        DeleteConfirmPanel.Visibility = Visibility.Visible;
                        ConfirmPanel.Visibility = Visibility.Collapsed;
                    };
                    container.Children.Add(delBtn);
                }

                SavesPanel.Children.Add(container);
            }
        }

        private void OnSaveSelected(OneSave save)
        {
            if (_mode == "new" && save._active)
            {
                _pendingOverwrite = save;
                int idx = _saves.IndexOf(save) + 1;
                ConfirmText.Text = $"Слот {idx} уже содержит сохранение. Перезаписать?";
                ConfirmPanel.Visibility = Visibility.Visible;
                DeleteConfirmPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                Commit(save);
            }
        }

        private void Commit(OneSave save)
        {
            SelectedSave = save;
            IsCanceled = false;
            DialogResult = true;
            Close();
        }

        private void ConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            ConfirmPanel.Visibility = Visibility.Collapsed;
            if (_pendingOverwrite != null) Commit(_pendingOverwrite);
        }

        private void ConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            ConfirmPanel.Visibility = Visibility.Collapsed;
            _pendingOverwrite = null;
        }

        private void DeleteYes_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;
            if (_pendingDelete == null) return;
            try
            {
                _db.DeleteSave(_pendingDelete);
                _saves.Remove(_pendingDelete);
                if (_pendingDeleteContainer != null)
                    SavesPanel.Children.Remove(_pendingDeleteContainer);
                if (_saves.Count == 0)
                    CreateSaveButtons(_saves);
            }
            catch (Exception ex)
            {
                DeleteConfirmText.Text = $"Ошибка: {ex.Message}";
                DeleteConfirmPanel.Visibility = Visibility.Visible;
            }
            _pendingDelete = null;
            _pendingDeleteContainer = null;
        }

        private void DeleteNo_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;
            _pendingDelete = null;
            _pendingDeleteContainer = null;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsCanceled = true;
            SelectedSave = null;
            DialogResult = false;
            Close();
        }
    }
}
