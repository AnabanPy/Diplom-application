using System.Text;
using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class ControlTab : UserControl, IRefreshableTab
{
    private readonly bool _canModify;

    public ControlTab()
    {
        InitializeComponent();
        var role = RolePermissions.NormalizeRole(AppSession.CurrentUser?.Role);
        _canModify = RolePermissions.CanClassifyOrReject(role);

        if (!_canModify)
        {
            ConfirmButton.Visibility = Visibility.Collapsed;
            RejectButton.Visibility = Visibility.Collapsed;
            ReadOnlyHint.Visibility = Visibility.Visible;
        }

        SetupColumns();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private void SetupColumns()
    {
        PendingGrid.Columns.Clear();
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new System.Windows.Data.Binding("Code"), Width = 80 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new System.Windows.Data.Binding("Name"), Width = 280 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Кл.", Binding = new System.Windows.Data.Binding("Hazard"), Width = 50 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Объем (т)", Binding = new System.Windows.Data.Binding("Volume"), Width = 85 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Цех", Binding = new System.Windows.Data.Binding("Source"), Width = 120 });
        PendingGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new System.Windows.Data.Binding("Status"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var batches = await AppSession.Client.GetBatchesAsync(new BatchQueryParams { Status = "accepted" });
            PendingGrid.ItemsSource = batches.Select(b => new PendingRow(b)).ToList();
        }
        catch (Exception ex)
        {
            PendingGrid.ItemsSource = null;
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private BatchDto? GetSelectedBatch() =>
        PendingGrid.SelectedItem is PendingRow row ? row.Batch : null;

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var batch = GetSelectedBatch();
        if (batch == null)
        {
            MessageBox.Show("Выберите партию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await AppSession.Client.ClassifyBatchAsync(batch.Id, new ClassifyBatchRequest
            {
                HazardClass = batch.HazardClass ?? 3,
                ClassificationNote = "Подтверждено"
            });
            MessageBox.Show($"Партия #{batch.Id} классифицирована", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
        var batch = GetSelectedBatch();
        if (batch == null)
        {
            MessageBox.Show("Выберите партию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var reason = InputDialog.Show("Укажите причину (мин. 3 символа):", "Причина отклонения", Window.GetWindow(this));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3) return;

        try
        {
            await AppSession.Client.RejectBatchAsync(batch.Id, new RejectBatchRequest { Reason = reason.Trim() });
            MessageBox.Show($"Партия #{batch.Id} отклонена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadDataAsync();
            AppSession.RefreshAllTabs();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void History_Click(object sender, RoutedEventArgs e)
    {
        var batch = GetSelectedBatch();
        if (batch == null)
        {
            MessageBox.Show("Выберите партию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var history = await AppSession.Client.GetClassificationHistoryAsync(batch.Id);
            if (history.Count == 0)
            {
                MessageBox.Show("История классификации пуста", "История", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Партия {batch.Code}:");
            foreach (var item in history)
            {
                var date = item.OperationAt?.Length >= 16 ? item.OperationAt[..16].Replace('T', ' ') : item.OperationAt ?? "—";
                var hazard = item.OldHazardClass.HasValue || item.NewHazardClass.HasValue
                    ? $" (кл. {item.OldHazardClass} → {item.NewHazardClass})"
                    : "";
                sb.AppendLine($"• {date} — {StatusTranslations.OperationTypeToRu(item.OperationType)}{hazard}");
                if (!string.IsNullOrWhiteSpace(item.Notes))
                    sb.AppendLine($"  {item.Notes}");
                if (!string.IsNullOrWhiteSpace(item.UserName))
                    sb.AppendLine($"  {item.UserName}");
            }

            MessageBox.Show(sb.ToString(), "История классификации", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private sealed class PendingRow
    {
        public BatchDto Batch { get; }

        public PendingRow(BatchDto batch) => Batch = batch;

        public int Id => Batch.Id;
        public string Code => Batch.Code ?? "";
        public string Name => Batch.Name?.Length > 45 ? Batch.Name[..45] : Batch.Name ?? "";
        public string Hazard => StatusTranslations.HazardToRoman(Batch.HazardClass);
        public string Volume => $"{Batch.VolumeTons ?? 0:F2}";
        public string Source => Batch.SourceDepartment ?? "—";
        public string Status => StatusTranslations.ToRu(Batch.Status);
    }
}
