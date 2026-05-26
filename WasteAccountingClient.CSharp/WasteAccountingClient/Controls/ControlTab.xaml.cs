using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class ControlTab : UserControl, IRefreshableTab
{
    private List<int> _batchIds = [];

    public ControlTab()
    {
        InitializeComponent();
        SetupColumns();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private void SetupColumns()
    {
        PendingGrid.Columns.Clear();
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 70 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new System.Windows.Data.Binding("Code"), Width = 90 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new System.Windows.Data.Binding("Name"), Width = 350 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Класс", Binding = new System.Windows.Data.Binding("Hazard"), Width = 80 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new System.Windows.Data.Binding("Status"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var batches = await AppSession.Client.GetBatchesAsync();
            var pending = batches.Where(b => b.Status == "accepted").ToList();
            _batchIds = pending.Select(b => b.Id).ToList();
            PendingGrid.ItemsSource = pending.Select(b => new
            {
                b.Id,
                Code = b.Code ?? "",
                Name = b.Name?.Length > 50 ? b.Name[..50] : b.Name ?? "",
                Hazard = b.HazardClass?.ToString() ?? "",
                Status = StatusTranslations.ToRu(b.Status)
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var idx = PendingGrid.SelectedIndex;
        if (idx < 0)
        {
            MessageBox.Show("Выберите партию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var batchId = _batchIds[idx];
        try
        {
            await AppSession.Client.ClassifyBatchAsync(batchId, new Models.ClassifyBatchRequest
            {
                HazardClass = 3,
                ClassificationNote = "Подтверждено"
            });
            MessageBox.Show($"Партия #{batchId} классифицирована", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadDataAsync();
            AppSession.RefreshAllTabs();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Reject_Click(object sender, RoutedEventArgs e)
    {
        var idx = PendingGrid.SelectedIndex;
        if (idx < 0)
        {
            MessageBox.Show("Выберите партию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var reason = InputDialog.Show("Укажите причину:", "Причина отклонения", Window.GetWindow(this));
        if (!string.IsNullOrWhiteSpace(reason))
        {
            MessageBox.Show($"Партия отклонена.\nПричина: {reason}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadDataAsync();
        }
    }
}
