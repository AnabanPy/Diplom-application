using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class ReportTab : UserControl, IRefreshableTab
{
    private List<ReportRow> _reportRows = [];
    private bool _filtersReady;

    public ReportTab()
    {
        InitializeComponent();
        DateFrom.SelectedDate = new DateTime(2026, 1, 1);
        DateTo.SelectedDate = DateTime.Today;
        StatusCombo.ItemsSource = new[] { "Все", "Принят", "Классифицирован", "В обработке", "Завершен", "Отклонен" };
        StatusCombo.SelectedIndex = 0;
        SetupColumns();
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
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new System.Windows.Data.Binding("Name"), Width = 240 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Кл.", Binding = new System.Windows.Data.Binding("Hazard"), Width = 50 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Поступило", Binding = new System.Windows.Data.Binding("Volume"), Width = 85 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Перераб.", Binding = new System.Windows.Data.Binding("Processed"), Width = 85 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Вывезено", Binding = new System.Windows.Data.Binding("Disposed"), Width = 85 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Остаток", Binding = new System.Windows.Data.Binding("Remaining"), Width = 85 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new System.Windows.Data.Binding("Date"), Width = 100 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new System.Windows.Data.Binding("Status"), Width = 110 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Цех", Binding = new System.Windows.Data.Binding("Source"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
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
            ReportGrid.ItemsSource = _reportRows;

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

            InfoLabel.Text = $"Найдено записей: {_reportRows.Count}";
        }
        catch (Exception ex)
        {
            InfoLabel.Text = $"Ошибка загрузки: {ex.Message}";
            ReportGrid.ItemsSource = null;
            _reportRows = [];
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

    private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_filtersReady)
            await LoadDataAsync();
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_reportRows.Count == 0)
        {
            MessageBox.Show("Нет данных для экспорта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var path = ReportExportService.ExportExcel(_reportRows.Select(r => r.Batch).ToList());
            MessageBox.Show($"Отчет сохранен:\n{path}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

        try
        {
            var path = ReportExportService.ExportWordAct(row.Batch);
            MessageBox.Show($"Акт сохранен:\n{path}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось создать Word: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private sealed class ReportRow
    {
        public BatchDto Batch { get; private init; } = null!;
        public string Code { get; private init; } = "";
        public string Name { get; private init; } = "";
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
