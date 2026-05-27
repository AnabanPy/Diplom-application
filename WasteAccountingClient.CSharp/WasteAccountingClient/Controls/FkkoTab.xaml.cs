using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class FkkoTab : UserControl, IRefreshableTab
{
    private List<FkkoRow> _allRows = [];
    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(300) };

    public FkkoTab()
    {
        InitializeComponent();
        SetupColumns();
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            ApplySearchFilter();
        };
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private static Style CreateCellTextStyle(TextAlignment alignment, Thickness? padding = null)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));
        if (padding.HasValue)
            style.Setters.Add(new Setter(TextBlock.PaddingProperty, padding.Value));
        return style;
    }

    private void SetupColumns()
    {
        FkkoGrid.Columns.Clear();
        FkkoGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "№",
            Binding = new Binding("RowNum"),
            Width = 80,
            MinWidth = 80,
            ElementStyle = CreateCellTextStyle(TextAlignment.Right, new Thickness(0, 0, 10, 0))
        });
        FkkoGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Код",
            Binding = new Binding("Code"),
            Width = 160,
            MinWidth = 140
        });
        FkkoGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Наименование",
            Binding = new Binding("Name"),
            Width = new DataGridLength(3, DataGridLengthUnitType.Star),
            MinWidth = 200
        });
        FkkoGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Код ФККО",
            Binding = new Binding("Fkko"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 130
        });
        FkkoGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Класс",
            Binding = new Binding("Hazard"),
            Width = new DataGridLength(70),
            MinWidth = 60,
            ElementStyle = CreateCellTextStyle(TextAlignment.Center)
        });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var wasteTypes = await AppSession.Client.GetWasteTypesAsync();
            _allRows = wasteTypes
                .OrderBy(wt => wt.FkkoCode ?? wt.Code ?? "", StringComparer.OrdinalIgnoreCase)
                .Select((wt, index) => new FkkoRow
                {
                    RowNum = index + 1,
                    Code = wt.Code ?? "",
                    Name = wt.Name ?? "",
                    Fkko = wt.FkkoCode ?? "",
                    Hazard = StatusTranslations.HazardToRoman(wt.HazardClass)
                })
                .ToList();

            InfoLabel.Text = _allRows.Count > 0
                ? $"Загружено: {_allRows.Count} поз. · порядок по коду ФККО"
                : "Справочник пуст";

            ApplySearchFilter();
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                if (FkkoGrid.Items.Count > 0)
                    FkkoGrid.ScrollIntoView(FkkoGrid.Items[0]!);
            }));
        }
        catch (Exception ex)
        {
            InfoLabel.Text = $"❌ Ошибка загрузки: {ex.Message}";
            _allRows = [];
            FkkoGrid.ItemsSource = null;
        }
    }

    private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private void ResetSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchInput.Text = "";
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        var q = SearchInput.Text.Trim().ToLowerInvariant();
        IEnumerable<FkkoRow> seq = string.IsNullOrEmpty(q)
            ? _allRows
            : _allRows.Where(r =>
                r.Code.ToLowerInvariant().Contains(q) ||
                r.Name.ToLowerInvariant().Contains(q) ||
                r.Fkko.ToLowerInvariant().Contains(q) ||
                r.Hazard.ToLowerInvariant().Contains(q));

        FkkoGrid.ItemsSource = seq.Select((r, i) => new FkkoRow
        {
            RowNum = i + 1,
            Code = r.Code,
            Name = r.Name,
            Fkko = r.Fkko,
            Hazard = r.Hazard
        }).ToList();
    }

    private sealed class FkkoRow
    {
        public int RowNum { get; init; }
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public string Fkko { get; init; } = "";
        public string Hazard { get; init; } = "";
    }
}
