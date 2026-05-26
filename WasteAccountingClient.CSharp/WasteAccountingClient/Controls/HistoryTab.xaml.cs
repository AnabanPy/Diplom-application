using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Models;
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
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 70 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new System.Windows.Data.Binding("Code"), Width = 90 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new System.Windows.Data.Binding("Name"), Width = 280 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Класс", Binding = new System.Windows.Data.Binding("Hazard"), Width = 70 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Объем (т)", Binding = new System.Windows.Data.Binding("Volume"), Width = 90 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "ФККО код", Binding = new System.Windows.Data.Binding("Fkko"), Width = 160 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new System.Windows.Data.Binding("Date"), Width = 110 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new System.Windows.Data.Binding("Status"), Width = 130 });
        BatchesGrid.Columns.Add(new DataGridTextColumn { Header = "Цех", Binding = new System.Windows.Data.Binding("Source"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var batches = await AppSession.Client.GetBatchesAsync();
            BatchesGrid.ItemsSource = batches.Select(b => new BatchRowView
            {
                Id = b.Id,
                Code = b.Code ?? "",
                Name = b.Name?.Length > 60 ? b.Name[..60] : b.Name ?? "",
                Hazard = StatusTranslations.HazardToRoman(b.HazardClass),
                Volume = $"{b.VolumeTons ?? 0:F2}",
                Fkko = (b.FkkoCode?.Length > 25 ? b.FkkoCode[..25] : b.FkkoCode) ?? "—",
                Date = b.ReceivedAt?.Length >= 10 ? b.ReceivedAt[..10] : "—",
                Status = StatusTranslations.ToRu(b.Status),
                Source = b.SourceDepartment ?? "—"
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

    private class BatchRowView
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Hazard { get; set; } = "";
        public string Volume { get; set; } = "";
        public string Fkko { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
        public string Source { get; set; } = "";
    }
}
