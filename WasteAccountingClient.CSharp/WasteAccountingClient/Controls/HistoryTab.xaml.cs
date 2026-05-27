using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class HistoryTab : UserControl, IRefreshableTab
{
    private readonly List<HistoryRow> _allRows = [];
    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(350) };

    public HistoryTab()
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

    private void SetupColumns()
    {
        BatchesGrid.Columns.Clear();
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new System.Windows.Data.Binding("Code"), Width = 80 });
        BatchesGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Наименование",
            Binding = new System.Windows.Data.Binding("Name"),
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
            MinWidth = 180
        });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Кл.", Binding = new System.Windows.Data.Binding("Hazard"), Width = 50 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Поступило (т)", Binding = new System.Windows.Data.Binding("Volume"), Width = 95 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Перераб. (т)", Binding = new System.Windows.Data.Binding("Processed"), Width = 95 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Вывезено (т)", Binding = new System.Windows.Data.Binding("Disposed"), Width = 95 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Остаток (т)", Binding = new System.Windows.Data.Binding("Remaining"), Width = 90 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new System.Windows.Data.Binding("Date"), Width = 100 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new System.Windows.Data.Binding("Status"), Width = 120 });
        BatchesGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Цех",
            Binding = new System.Windows.Data.Binding("Source"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 100
        });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var batches = await AppSession.Client.GetBatchesAsync();
            _allRows.Clear();
            foreach (var b in batches)
            {
                _allRows.Add(new HistoryRow
                {
                    Id = b.Id,
                    Code = b.Code ?? "",
                    Name = b.Name?.Length > 50 ? b.Name[..50] : b.Name ?? "",
                    Hazard = StatusTranslations.HazardToRoman(b.HazardClass),
                    Volume = $"{b.VolumeTons ?? 0:F2}",
                    Processed = $"{b.ProcessedTons ?? 0:F2}",
                    Disposed = $"{b.DisposedTons ?? 0:F2}",
                    Remaining = $"{b.RemainingTons ?? Math.Max(0, (b.VolumeTons ?? 0) - (b.ProcessedTons ?? 0) - (b.DisposedTons ?? 0)):F2}",
                    Date = b.ReceivedAt?.Length >= 10 ? b.ReceivedAt[..10] : "—",
                    Status = StatusTranslations.ToRu(b.Status),
                    Source = b.SourceDepartment ?? "—"
                });
            }

            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            _allRows.Clear();
            BatchesGrid.ItemsSource = null;
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        if (string.IsNullOrEmpty(q))
        {
            BatchesGrid.ItemsSource = _allRows.ToList();
            return;
        }

        BatchesGrid.ItemsSource = _allRows.Where(r =>
            r.Code.ToLowerInvariant().Contains(q) ||
            r.Name.ToLowerInvariant().Contains(q) ||
            r.Source.ToLowerInvariant().Contains(q) ||
            r.Status.ToLowerInvariant().Contains(q)).ToList();
    }

    private sealed class HistoryRow
    {
        public int Id { get; init; }
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public string Hazard { get; init; } = "";
        public string Volume { get; init; } = "";
        public string Processed { get; init; } = "";
        public string Disposed { get; init; } = "";
        public string Remaining { get; init; } = "";
        public string Date { get; init; } = "";
        public string Status { get; init; } = "";
        public string Source { get; init; } = "";
    }
}
