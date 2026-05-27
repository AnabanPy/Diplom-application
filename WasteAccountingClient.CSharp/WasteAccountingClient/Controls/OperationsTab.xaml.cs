using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class OperationsTab : UserControl, IRefreshableTab
{
    private List<BatchDto> _batches = [];
    private BatchDto? _selectedBatch;

    public OperationsTab()
    {
        InitializeComponent();
        OperationTypeCombo.ItemsSource = new[]
        {
            "Переработка (processing)",
            "Утилизация (disposal)",
            "Вывоз (export)",
            "Передача (transfer)"
        };
        OperationTypeCombo.SelectedIndex = 0;
        SetupColumns();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private void SetupColumns()
    {
        OperationsGrid.Columns.Clear();
        OperationsGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
        OperationsGrid.Columns.Add(new DataGridTextColumn { Header = "Партия", Binding = new System.Windows.Data.Binding("BatchId"), Width = 70 });
        OperationsGrid.Columns.Add(new DataGridTextColumn { Header = "Тип", Binding = new System.Windows.Data.Binding("Type"), Width = 120 });
        OperationsGrid.Columns.Add(new DataGridTextColumn { Header = "Т (тонн)", Binding = new System.Windows.Data.Binding("Quantity"), Width = 90 });
        OperationsGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new System.Windows.Data.Binding("Date"), Width = 130 });
        OperationsGrid.Columns.Add(new DataGridTextColumn { Header = "Пользователь", Binding = new System.Windows.Data.Binding("User"), Width = 120 });
        OperationsGrid.Columns.Add(new DataGridTextColumn { Header = "Примечание", Binding = new System.Windows.Data.Binding("Notes"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            _batches = await AppSession.Client.GetBatchesAsync();
            var items = _batches
                .Where(b => b.Status is not "rejected")
                .Select(b => new BatchComboItem
                {
                    Batch = b,
                    Display = $"{b.Code} — {Truncate(b.Name, 40)} ({b.VolumeTons ?? 0:F2} т, ост.: {b.RemainingTons ?? b.VolumeTons ?? 0:F2} т)"
                })
                .ToList();

            var prevId = _selectedBatch?.Id;
            BatchCombo.ItemsSource = items;
            if (prevId.HasValue)
            {
                var idx = items.FindIndex(i => i.Batch.Id == prevId.Value);
                BatchCombo.SelectedIndex = idx >= 0 ? idx : items.Count > 0 ? 0 : -1;
            }
            else if (items.Count > 0)
                BatchCombo.SelectedIndex = 0;

            if (BatchCombo.SelectedItem is BatchComboItem selected)
                _selectedBatch = selected.Batch;
            else
                _selectedBatch = null;

            await RefreshBalanceAsync();
            await LoadOperationsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadOperationsAsync()
    {
        try
        {
            var ops = _selectedBatch != null
                ? await AppSession.Client.GetOperationsAsync(_selectedBatch.Id)
                : await AppSession.Client.GetOperationsAsync();

            OperationsGrid.ItemsSource = ops.Select(o => new
            {
                o.Id,
                BatchId = o.BatchId?.ToString() ?? "—",
                Type = StatusTranslations.OperationTypeToRu(o.OperationType),
                Quantity = $"{o.QuantityTons:F3}",
                Date = o.OperationAt?.Length >= 16 ? o.OperationAt[..16].Replace('T', ' ') : o.OperationAt ?? "—",
                User = o.UserName ?? "—",
                Notes = o.Notes ?? ""
            }).ToList();
        }
        catch (Exception ex)
        {
            OperationsGrid.ItemsSource = null;
            MessageBox.Show($"Ошибка загрузки операций: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BatchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BatchCombo.SelectedItem is BatchComboItem item)
        {
            _selectedBatch = item.Batch;
            await RefreshBalanceAsync();
            await LoadOperationsAsync();
        }
    }

    private async Task RefreshBalanceAsync()
    {
        if (_selectedBatch == null)
        {
            ClearBalance();
            return;
        }

        try
        {
            var balance = await AppSession.Client.GetBatchBalanceAsync(_selectedBatch.Id);
            BalanceReceived.Text = $"Поступило: {balance.ReceivedTons:F3} т";
            BalanceProcessed.Text = $"Переработано: {balance.ProcessedTons:F3} т";
            BalanceDisposed.Text = $"Вывезено/утилизировано: {balance.DisposedTons:F3} т";
            BalanceRemaining.Text = $"Остаток: {balance.RemainingTons:F3} т";
        }
        catch
        {
            BalanceReceived.Text = $"Поступило: {_selectedBatch.VolumeTons ?? 0:F3} т";
            BalanceProcessed.Text = $"Переработано: {_selectedBatch.ProcessedTons ?? 0:F3} т";
            BalanceDisposed.Text = $"Вывезено: {_selectedBatch.DisposedTons ?? 0:F3} т";
            BalanceRemaining.Text = $"Остаток: {_selectedBatch.RemainingTons ?? 0:F3} т";
        }
    }

    private void ClearBalance()
    {
        BalanceReceived.Text = "Поступило: —";
        BalanceProcessed.Text = "Переработано: —";
        BalanceDisposed.Text = "Вывезено: —";
        BalanceRemaining.Text = "Остаток: —";
    }

    private async void SaveOperation_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBatch == null)
        {
            MessageBox.Show("Выберите партию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(QuantityInput.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var qty) || qty <= 0)
        {
            MessageBox.Show("Введите положительное количество в тоннах", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var opType = ParseOperationType(OperationTypeCombo.SelectedItem?.ToString());
        if (opType == null)
        {
            MessageBox.Show("Выберите тип операции", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await AppSession.Client.CreateOperationAsync(new CreateOperationRequest
            {
                BatchId = _selectedBatch.Id,
                OperationType = opType,
                QuantityTons = Math.Round(qty, 3),
                Notes = NotesInput.Text.Trim()
            });

            MessageBox.Show("Операция записана", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            QuantityInput.Clear();
            NotesInput.Clear();
            await LoadDataAsync();
            AppSession.RefreshAllTabs();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось записать операцию:\n{ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? ParseOperationType(string? label)
    {
        if (string.IsNullOrEmpty(label)) return null;
        var start = label.IndexOf('(');
        var end = label.IndexOf(')');
        if (start >= 0 && end > start)
            return label[(start + 1)..end];
        return null;
    }

    private static string Truncate(string? text, int max) =>
        string.IsNullOrEmpty(text) ? "" : text.Length > max ? text[..max] : text;

    private class BatchComboItem
    {
        public BatchDto Batch { get; set; } = null!;
        public string Display { get; set; } = "";
    }
}
