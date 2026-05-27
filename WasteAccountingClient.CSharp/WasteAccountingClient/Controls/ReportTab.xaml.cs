using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class ReportTab : UserControl, IRefreshableTab
{
    private List<ReportRow> _reportRows = [];
    private bool _filtersReady;
    private readonly DispatcherTimer _dateDebounce = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(300) };

    public ReportTab()
    {
        InitializeComponent();
        DateFrom.SelectedDate = new DateTime(2026, 1, 1);
        DateTo.SelectedDate = DateTime.Today;
        StatusCombo.ItemsSource = new[] { "Все", "Принят", "Классифицирован", "В обработке", "Завершен", "Отклонен" };
        StatusCombo.SelectedIndex = 0;
        SetupColumns();
        _dateDebounce.Tick += async (_, _) =>
        {
            _dateDebounce.Stop();
            await LoadDataAsync();
        };
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            ApplySearchFilter();
        };
        Loaded += async (_, _) =>
        {
            await LoadDepartmentsAsync();
            _filtersReady = true;
            await LoadDataAsync();
        };
    }

    private async Task LoadDepartmentsAsync()
    {
        try
        {
            var departments = await AppSession.Client.GetDepartmentsAsync();
            var items = new List<string> { "Все цеха" };
            items.AddRange(departments
                .Select(d => d.Name ?? d.Code ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            DepartmentCombo.ItemsSource = items;
            DepartmentCombo.SelectedIndex = 0;
        }
        catch
        {
            DepartmentCombo.ItemsSource = new[] { "Все цеха" };
            DepartmentCombo.SelectedIndex = 0;
        }
    }

    private void SetupColumns()
    {
        ReportGrid.Columns.Clear();
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new System.Windows.Data.Binding("Code"), Width = 90 });
        ReportGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Наименование",
            Binding = new System.Windows.Data.Binding("Name"),
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
            MinWidth = 180
        });
        ReportGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Код ФККО",
            Binding = new System.Windows.Data.Binding("FkkoCode"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 100
        });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Кл.", Binding = new System.Windows.Data.Binding("Hazard"), Width = 50 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Поступило", Binding = new System.Windows.Data.Binding("Volume"), Width = 85 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Перераб.", Binding = new System.Windows.Data.Binding("Processed"), Width = 85 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Вывезено", Binding = new System.Windows.Data.Binding("Disposed"), Width = 85 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Остаток", Binding = new System.Windows.Data.Binding("Remaining"), Width = 85 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new System.Windows.Data.Binding("Date"), Width = 100 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new System.Windows.Data.Binding("Status"), Width = 110 });
        ReportGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Цех",
            Binding = new System.Windows.Data.Binding("Source"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 100
        });
    }

    public async Task LoadDataAsync()
    {
        if (!_filtersReady) return;

        try
        {
            var dateFrom = DateFrom.SelectedDate ?? new DateTime(2026, 1, 1);
            var dateTo = DateTo.SelectedDate ?? DateTime.Today;
            if (dateFrom > dateTo)
            {
                InfoLabel.Text = "Дата «от» не может быть позже даты «до»";
                ReportGrid.ItemsSource = null;
                _reportRows = [];
                return;
            }

            var statusFilter = StatusCombo.SelectedItem?.ToString() ?? "Все";
            var statusEn = StatusTranslations.ToEn(statusFilter);
            var dept = DepartmentCombo.SelectedItem?.ToString();
            var deptFilter = dept is null or "Все цеха" ? null : dept;

            var query = new BatchQueryParams
            {
                DateFrom = dateFrom.ToString("yyyy-MM-dd"),
                DateTo = dateTo.ToString("yyyy-MM-dd"),
                Status = statusEn,
                SourceDepartment = deptFilter
            };

            var batches = await AppSession.Client.GetBatchesAsync(query);
            _reportRows = batches.Select(ReportRow.From).ToList();

            try
            {
                var dash = await AppSession.Client.GetDashboardAsync();
                DashboardPanel.Visibility = Visibility.Visible;
                DashboardText.Text =
                    $"📊 Партий: {dash.TotalBatches}  |  Поступило: {dash.TotalVolumeTons:F1} т  |  " +
                    $"Переработано: {dash.TotalProcessedTons:F1} т  |  Вывезено: {dash.TotalDisposedTons:F1} т  |  " +
                    $"Остаток: {dash.TotalRemainingTons:F1} т  |  Операций: {dash.OperationsCount}";
            }
            catch
            {
                DashboardPanel.Visibility = Visibility.Collapsed;
            }

            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            InfoLabel.Text = $"Ошибка загрузки: {ex.Message}";
            ReportGrid.ItemsSource = null;
            _reportRows = [];
        }
    }

    private void DateFilter_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (!_filtersReady) return;
        _dateDebounce.Stop();
        _dateDebounce.Start();
    }

    private async void FilterCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_filtersReady)
            await LoadDataAsync();
    }

    private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private void ApplySearchFilter()
    {
        var q = SearchInput.Text.Trim().ToLowerInvariant();
        List<ReportRow> visible;
        if (string.IsNullOrEmpty(q))
            visible = _reportRows.ToList();
        else
        {
            visible = _reportRows.Where(r =>
                r.Code.ToLowerInvariant().Contains(q) ||
                r.Name.ToLowerInvariant().Contains(q) ||
                (r.FkkoCode ?? "").ToLowerInvariant().Contains(q) ||
                r.Source.ToLowerInvariant().Contains(q) ||
                r.Status.ToLowerInvariant().Contains(q)).ToList();
        }

        ReportGrid.ItemsSource = visible;
        var total = _reportRows.Count;
        InfoLabel.Text = string.IsNullOrEmpty(q)
            ? $"Найдено записей: {total}"
            : $"Найдено записей: {visible.Count} из {total}";
    }

    private async void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        _filtersReady = false;
        DateFrom.SelectedDate = new DateTime(2026, 1, 1);
        DateTo.SelectedDate = DateTime.Today;
        StatusCombo.SelectedIndex = 0;
        DepartmentCombo.SelectedIndex = 0;
        SearchInput.Text = "";
        _filtersReady = true;
        await LoadDataAsync();
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_reportRows.Count == 0)
        {
            MessageBox.Show("Нет данных для экспорта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var rows = GetRowsForExport();
        if (rows.Count == 0)
        {
            MessageBox.Show("Нет строк для экспорта (проверьте поиск)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
            FileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true
        };
        if (dlg.ShowDialog() != true)
            return;

        try
        {
            ReportExportService.ExportExcel(rows.Select(r => r.Batch).ToList(), dlg.FileName);
            MessageBox.Show($"Отчёт сохранён:\n{dlg.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось создать Excel: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportWord_Click(object sender, RoutedEventArgs e)
    {
        if (ReportGrid.SelectedItem is not ReportRow row)
        {
            MessageBox.Show("Выберите партию из таблицы (кликните на строку)", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var safe = ReportExportService.SanitizeFileName(row.Code);
        var dlg = new SaveFileDialog
        {
            Filter = "Word (*.docx)|*.docx|Все файлы (*.*)|*.*",
            FileName = $"act_{safe}_{DateTime.Now:yyyyMMdd_HHmmss}.docx",
            DefaultExt = ".docx",
            AddExtension = true
        };
        if (dlg.ShowDialog() != true)
            return;

        try
        {
            ReportExportService.ExportWordAct(row.Batch, dlg.FileName);
            MessageBox.Show($"Акт сохранён:\n{dlg.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось создать Word: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private List<ReportRow> GetRowsForExport()
    {
        var q = SearchInput.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q))
            return _reportRows.ToList();
        return _reportRows.Where(r =>
            r.Code.ToLowerInvariant().Contains(q) ||
            r.Name.ToLowerInvariant().Contains(q) ||
            (r.FkkoCode ?? "").ToLowerInvariant().Contains(q) ||
            r.Source.ToLowerInvariant().Contains(q) ||
            r.Status.ToLowerInvariant().Contains(q)).ToList();
    }

    private sealed class ReportRow
    {
        public BatchDto Batch { get; private init; } = null!;
        public string Code { get; private init; } = "";
        public string Name { get; private init; } = "";
        public string FkkoCode { get; private init; } = "";
        public string Hazard { get; private init; } = "";
        public string Volume { get; private init; } = "";
        public string Processed { get; private init; } = "";
        public string Disposed { get; private init; } = "";
        public string Remaining { get; private init; } = "";
        public string Date { get; private init; } = "";
        public string Status { get; private init; } = "";
        public string Source { get; private init; } = "";

        public static ReportRow From(BatchDto b) => new()
        {
            Batch = b,
            Code = b.Code ?? "",
            Name = b.Name?.Length > 45 ? b.Name[..45] : b.Name ?? "",
            FkkoCode = b.FkkoCode ?? "",
            Hazard = StatusTranslations.HazardToRoman(b.HazardClass),
            Volume = $"{b.VolumeTons ?? 0:F2}",
            Processed = $"{b.ProcessedTons ?? 0:F2}",
            Disposed = $"{b.DisposedTons ?? 0:F2}",
            Remaining = $"{b.RemainingTons ?? Math.Max(0, (b.VolumeTons ?? 0) - (b.ProcessedTons ?? 0) - (b.DisposedTons ?? 0)):F2}",
            Date = b.ReceivedAt?.Length >= 10 ? b.ReceivedAt[..10] : "—",
            Status = StatusTranslations.ToRu(b.Status),
            Source = b.SourceDepartment ?? "—"
        };
    }
}
