using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class ReportTab : UserControl, IRefreshableTab
{
    private List<BatchDto> _batchesData = [];

    public ReportTab()
    {
        InitializeComponent();
        DateFrom.SelectedDate = new DateTime(2026, 1, 1);
        DateTo.SelectedDate = DateTime.Today;
        StatusCombo.ItemsSource = new[] { "Все", "Принят", "Классифицирован", "Завершен" };
        StatusCombo.SelectedIndex = 0;
        SetupColumns();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private void SetupColumns()
    {
        ReportGrid.Columns.Clear();
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new System.Windows.Data.Binding("Code"), Width = 100 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new System.Windows.Data.Binding("Name"), Width = 300 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Класс", Binding = new System.Windows.Data.Binding("Hazard"), Width = 70 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Объем (т)", Binding = new System.Windows.Data.Binding("Volume"), Width = 90 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "ФККО", Binding = new System.Windows.Data.Binding("Fkko"), Width = 160 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new System.Windows.Data.Binding("Date"), Width = 110 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new System.Windows.Data.Binding("Status"), Width = 120 });
        ReportGrid.Columns.Add(new DataGridTextColumn { Header = "Цех", Binding = new System.Windows.Data.Binding("Source"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var batches = await AppSession.Client.GetBatchesAsync();
            var dateFrom = DateFrom.SelectedDate?.ToString("yyyy-MM-dd") ?? "2026-01-01";
            var dateTo = DateTo.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            var statusFilter = StatusCombo.SelectedItem?.ToString() ?? "Все";
            var statusEn = StatusTranslations.ToEn(statusFilter);

            var filtered = batches.Where(batch =>
            {
                var received = batch.ReceivedAt?.Length >= 10 ? batch.ReceivedAt[..10] : "";
                if (string.Compare(received, dateFrom, StringComparison.Ordinal) < 0) return false;
                if (string.Compare(received, dateTo, StringComparison.Ordinal) > 0) return false;
                if (statusEn != null && batch.Status != statusEn) return false;
                return true;
            }).ToList();

            _batchesData = filtered;
            ReportGrid.ItemsSource = filtered.Select(b => new
            {
                Code = b.Code ?? "",
                Name = b.Name?.Length > 50 ? b.Name[..50] : b.Name ?? "",
                Hazard = StatusTranslations.HazardToRoman(b.HazardClass),
                Volume = $"{b.VolumeTons ?? 0:F2}",
                Fkko = (b.FkkoCode?.Length > 25 ? b.FkkoCode[..25] : b.FkkoCode) ?? "—",
                Date = b.ReceivedAt?.Length >= 10 ? b.ReceivedAt[..10] : "—",
                Status = StatusTranslations.ToRu(b.Status),
                Source = b.SourceDepartment ?? "—"
            }).ToList();

            InfoLabel.Text = $"📊 Найдено записей: {filtered.Count}";
        }
        catch (Exception ex)
        {
            InfoLabel.Text = $"❌ Ошибка загрузки: {ex.Message}";
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();
    private async void Filter_Changed(object sender, SelectionChangedEventArgs e) => await LoadDataAsync();

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_batchesData.Count == 0)
        {
            MessageBox.Show("Нет данных для экспорта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var path = ReportExportService.ExportExcel(_batchesData);
            MessageBox.Show($"Отчет сохранен:\n{path}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось создать Excel: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportWord_Click(object sender, RoutedEventArgs e)
    {
        var idx = ReportGrid.SelectedIndex;
        if (idx < 0 || idx >= _batchesData.Count)
        {
            MessageBox.Show("Выберите партию из таблицы (кликните на строку)", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var path = ReportExportService.ExportWordAct(_batchesData[idx]);
            MessageBox.Show($"Акт сохранен:\n{path}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось создать Word: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
