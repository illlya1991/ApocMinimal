using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ApocMinimal.Models.PersonData;
using ApocMinimal.Services;

namespace ApocMinimal.Controls;

public partial class NpcListControl : UserControl
{
    public event Action<Npc>? NpcSelected;

    private readonly GameUIService _uiService;
    private List<Npc> _allFollowers = new();  // Все последователи (уровень 1+)
    private List<Npc> _currentPageNpcs = new();
    private Npc? _currentSelectedNpc;

    private int _currentPage = 1;
    private const int PageSize = 100;
    private int _totalPages = 1;

    public NpcListControl()
    {
        InitializeComponent();
        _uiService = new GameUIService((text, color) => { });
    }

    public void UpdateNpcs(List<Npc> npcs, Npc? currentSelectedNpc = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"  NpcListControl.UpdateNpcs: npcs={npcs?.Count ?? 0}, selected={currentSelectedNpc?.Name}");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            _currentSelectedNpc = currentSelectedNpc;

            // Фильтруем: только живые последователи с уровнем 1+
            _allFollowers = npcs
                .Where(n => n.IsAlive && n.FollowerLevel >= 1)
                .OrderByDescending(n => n.FollowerLevel)
                .ThenBy(n => n.Name)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"    Последователей: {_allFollowers.Count}");

            // Обновляем счётчик
            int totalFollowers = _allFollowers.Count;
            FollowerCountLabel.Text = $"{totalFollowers} посл.";

            // Рассчитываем страницы
            _totalPages = totalFollowers > 0 ? (int)System.Math.Ceiling((double)totalFollowers / PageSize) : 1;
            _currentPage = 1;

            // Отображаем первую страницу
            RefreshCurrentPage();

            // Обновляем состояние кнопок
            UpdatePaginationButtons();

            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"    UpdateNpcs за {sw.ElapsedMilliseconds} мс");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! ОШИБКА В UpdateNpcs: {ex.Message}");
            throw;
        }
    }
    private void RefreshCurrentPage()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"      NpcListControl: отрисовка страницы {_currentPage} START");

        NpcPanel.Children.Clear();

        // Вычисляем индексы для текущей страницы
        int startIndex = (_currentPage - 1) * PageSize;
        int endIndex = System.Math.Min(startIndex + PageSize, _allFollowers.Count);

        if (startIndex >= _allFollowers.Count)
        {
            NpcPanel.Children.Add(new TextBlock
            {
                Text = "Нет последователей",
                Foreground = BrushCache.GetBrush("#8b949e"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
            System.Diagnostics.Debug.WriteLine($"      Нет последователей: {sw.ElapsedMilliseconds} мс");
            return;
        }

        _currentPageNpcs = _allFollowers.Skip(startIndex).Take(PageSize).ToList();

        int cardCount = 0;
        for (int i = 0; i < _currentPageNpcs.Count; i++)
        {
            var npc = _currentPageNpcs[i];
            var card = _uiService.BuildNpcCard(npc);
            var capturedNpc = npc;

            card.MouseLeftButtonUp += (_, e) =>
            {
                NpcSelected?.Invoke(capturedNpc);
            };

            // Подсветка выбранного NPC
            if (_currentSelectedNpc != null && _currentSelectedNpc.Id == npc.Id)
            {
                card.BorderBrush = BrushCache.GetBrush("#60a5fa")!;
                card.BorderThickness = new Thickness(2);
            }

            NpcPanel.Children.Add(card);
            cardCount++;

            // Логируем каждые 20 карточек
            if (cardCount % 20 == 0)
            {
                System.Diagnostics.Debug.WriteLine($"        Создано {cardCount} карточек за {sw.ElapsedMilliseconds} мс");
            }
        }

        // Обновляем информацию о странице
        int totalDisplayed = System.Math.Min(PageSize, _allFollowers.Count - startIndex);
        int from = startIndex + 1;
        int to = startIndex + totalDisplayed;
        PageInfoLabel.Text = $"{from}-{to} / {_allFollowers.Count}";

        System.Diagnostics.Debug.WriteLine($"      NpcListControl: отрисовано {cardCount} карточек за {sw.ElapsedMilliseconds} мс");
    }
    private void UpdatePaginationButtons()
    {
        PrevPageBtn.IsEnabled = _currentPage > 1;
        NextPageBtn.IsEnabled = _currentPage < _totalPages && _allFollowers.Count > PageSize;

        // Стиль для активной страницы
        PageInfoLabel.Foreground = BrushCache.GetBrush(_allFollowers.Count > 0 ? "#60a5fa" : "#8b949e")!;
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            RefreshCurrentPage();
            UpdatePaginationButtons();

            // Прокрутка вверх
            if (NpcPanel.Parent is ScrollViewer sv)
                sv.ScrollToTop();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            RefreshCurrentPage();
            UpdatePaginationButtons();

            // Прокрутка вверх
            if (NpcPanel.Parent is ScrollViewer sv)
                sv.ScrollToTop();
        }
    }

    // Очистка кэша при обновлении
    public void Clear()
    {
        _allFollowers.Clear();
        _currentPageNpcs.Clear();
        NpcPanel.Children.Clear();
        FollowerCountLabel.Text = "0 посл.";
        PageInfoLabel.Text = "0-0 / 0";
        PrevPageBtn.IsEnabled = false;
        NextPageBtn.IsEnabled = false;
    }
}