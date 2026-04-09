using ApocMinimal.Database;
using System;
using System.Collections.Generic;
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
        private string _mode;
        private List<OneSave> _saves;
        private DatabaseManager _db;

        public SaveSelectionWindow(List<OneSave> saves, string mode, DatabaseManager db)
        {
            InitializeComponent();
            _mode = mode;
            _saves = saves;

            // Налаштовуємо текст залежно від режиму
            if (mode == "new")
            {
                TitleText.Text = "Нова гра";
                DescriptionText.Text = "Оберіть слот для нової гри:";
            }
            else if (mode == "continue")
            {
                TitleText.Text = "Продовжити гру";
                DescriptionText.Text = "Оберіть збереження для продовження:";
            }

            // Створюємо кнопки для кожного збереження
            CreateSaveButtons(saves);
            _db = db;
        }

        private void CreateSaveButtons(List<OneSave> saves)
        {
            if (saves == null || saves.Count == 0)
            {
                TextBlock noSavesText = new TextBlock
                {
                    Text = "Немає доступних збережень",
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#888"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                SavesPanel.Children.Add(noSavesText);
                return;
            }

            // Створюємо кнопку для кожного збереження
            for (int i = 0; i < saves.Count; i++)
            {
                var save = saves[i];
                int saveIndex = i + 1;

                // Створюємо контейнер для кнопки та додаткових елементів
                StackPanel saveContainer = new StackPanel
                {
                    Margin = new Thickness(0, 0, 0, 8)
                };

                // Основна кнопка вибору збереження
                Button saveButton = new Button
                {
                    Content = $"Збереження {saveIndex}\n{save._connectionString}",
                    Height = 55,
                    FontSize = 13,
                    Background = save._active ? new SolidColorBrush(Color.FromRgb(42, 96, 64)) :
                                              new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Tag = save
                };

                int index = i;
                saveButton.Click += (sender, e) => OnSaveSelected(saves[index]);

                saveContainer.Children.Add(saveButton);

                // Якщо режим "continue" та збереження активне - додаємо кнопку видалення
                if (_mode == "continue" && save._active)
                {
                    Button deleteButton = new Button
                    {
                        Content = "🗑 Видалити збереження",
                        Height = 30,
                        FontSize = 11,
                        Background = new SolidColorBrush(Color.FromRgb(139, 0, 0)), // Темно-червоний
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(0, 2, 0, 0),
                        Tag = save
                    };
                    deleteButton.Click += (sender, e) => OnDeleteSave(saves[index], saveContainer);
                    saveContainer.Children.Add(deleteButton);
                }

                SavesPanel.Children.Add(saveContainer);
            }
        }

        private async void OnSaveSelected(OneSave save)
        {
            if (_mode == "new")
            {
                // Якщо вибрано активне збереження - питаємо про перезапис
                if (save._active)
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"Збереження {save._connectionString} вже існує та є активним.\n\nБажаєте перезаписати його?",
                        "Підтвердження перезапису",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Перезаписуємо збереження
                        SelectedSave = save;
                        IsCanceled = false;
                        DialogResult = true;
                        Close();
                    }
                    // Якщо No - залишаємось у вікні вибору
                }
                else
                {
                    // Якщо збереження неактивне - просто вибираємо
                    SelectedSave = save;
                    IsCanceled = false;
                    DialogResult = true;
                    Close();
                }
            }
            else if (_mode == "continue")
            {
                // Для продовження - просто вибираємо активне збереження
                SelectedSave = save;
                IsCanceled = false;
                DialogResult = true;
                Close();
            }
        }

        private async void OnDeleteSave(OneSave save, StackPanel container)
        {
            MessageBoxResult result = MessageBox.Show(
                $"Ви дійсно хочете видалити збереження {save._connectionString}?\n\nЦю дію не можна буде скасувати!",
                "Видалення збереження",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _db.DeleteSave(save);

                    // Видаляємо контейнер з UI
                    SavesPanel.Children.Remove(container);

                    // Оновлюємо список збережень (видаляємо з переданого списку)
                    _saves.Remove(save);

                    // Оновлюємо інформацію, якщо збережень не залишилось
                    if (_saves.Count == 0)
                    {
                        CreateSaveButtons(_saves);
                    }

                    MessageBox.Show("Збереження успішно видалено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при видаленні: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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