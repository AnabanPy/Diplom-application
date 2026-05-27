using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class HistoryTab : UserControl, IRefreshableTab
{
    public HistoryTab()
    {
        InitializeComponent();
        SetupColumns();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private void SetupColumns()
    {
        BatchesGrid.Columns.Clear();
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new System.Windows.Data.Binding("Code"), Width = 80 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new System.Windows.Data.Binding("Name"), Width = 220 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Кл.", Binding = new System.Windows.Data.Binding("Hazard"), Width = 50 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Поступило (т)", Binding = new System.Windows.Data.Binding("Volume"), Width = 95 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Перераб. (т)", Binding = new System.Windows.Data.Binding("Processed"), Width = 95 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Вывезено (т)", Binding = new System.Windows.Data.Binding("Disposed"), Width = 95 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Остаток (т)", Binding = new System.Windows.Data.Binding("Remaining"), Width = 90 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new System.Windows.Data.Binding("Date"), Width = 100 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new System.Windows.Data.Binding("Status"), Width = 120 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Цех", Binding = new System.Windows.Data.Binding("Source"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var batches = await AppSession.Client.GetBatchesAsync();
            BatchesGrid.ItemsSource = batches.Select(b => new
            {
                b.Id,
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
            }).ToList();
        }
        catch (Exception ex)
        {
            BatchesGrid.ItemsSource = null;
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();
}
