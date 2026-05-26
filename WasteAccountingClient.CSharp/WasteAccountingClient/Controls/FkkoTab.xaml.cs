using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class FkkoTab : UserControl, IRefreshableTab
{
    public FkkoTab()
    {
        InitializeComponent();
        SetupColumns();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private void SetupColumns()
    {
        FkkoGrid.Columns.Clear();
        FkkoGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
        FkkoGrid.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new System.Windows.Data.Binding("Code"), Width = 100 });
        FkkoGrid.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new System.Windows.Data.Binding("Name"), Width = 280 });
        FkkoGrid.Columns.Add(new DataGridTextColumn { Header = "Код ФККО", Binding = new System.Windows.Data.Binding("Fkko"), Width = 150 });
        FkkoGrid.Columns.Add(new DataGridTextColumn { Header = "Класс", Binding = new System.Windows.Data.Binding("Hazard"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var wasteTypes = await AppSession.Client.GetWasteTypesAsync();
            FkkoGrid.ItemsSource = wasteTypes.Select(wt => new
            {
                wt.Id,
                Code = wt.Code ?? "",
                Name = wt.Name ?? "",
                Fkko = wt.FkkoCode ?? "",
                Hazard = StatusTranslations.HazardToRoman(wt.HazardClass)
            }).ToList();
            InfoLabel.Text = $"📋 Загружено видов отходов: {wasteTypes.Count}";
        }
        catch (Exception ex)
        {
            InfoLabel.Text = $"❌ Ошибка загрузки: {ex.Message}";
            MessageBox.Show($"Не удалось загрузить справочник ФККО:\n{ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();
}
