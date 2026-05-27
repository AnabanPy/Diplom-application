using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

    private static Style CreateCellTextStyle(TextAlignment alignment, Thickness? padding = null)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));
        if (padding.HasValue)
            style.Setters.Add(new Setter(TextBlock.PaddingProperty, padding.Value));
        return style;
    }

    private void SetupColumns()
    {
        FkkoGrid.Columns.Clear();
        FkkoGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "№",
            Binding = new Binding("RowNum"),
            Width = 80,
            MinWidth = 80,
            ElementStyle = CreateCellTextStyle(TextAlignment.Right, new Thickness(0, 0, 10, 0))
        });
        FkkoGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Код",
            Binding = new Binding("Code"),
            Width = 160,
            MinWidth = 140
        });
        FkkoGrid.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new Binding("Name"), Width = 300 });
        FkkoGrid.Columns.Add(new DataGridTextColumn { Header = "Код ФККО", Binding = new Binding("Fkko"), Width = 160, MinWidth = 150 });
        FkkoGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Класс",
            Binding = new Binding("Hazard"),
            Width = new DataGridLength(70),
            MinWidth = 60,
            ElementStyle = CreateCellTextStyle(TextAlignment.Center)
        });
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var wasteTypes = await AppSession.Client.GetWasteTypesAsync();
            FkkoGrid.ItemsSource = wasteTypes.Select((wt, index) => new
            {
                RowNum = index + 1,
                Code = wt.Code ?? "",
                Name = wt.Name ?? "",
                Fkko = wt.FkkoCode ?? "",
                Hazard = StatusTranslations.HazardToRoman(wt.HazardClass)
            }).ToList();
            InfoLabel.Text = wasteTypes.Count > 0
                ? $"Загружено: {wasteTypes.Count} поз. · порядок по коду ФККО · первая: {wasteTypes[0].FkkoCode}"
                : "Справочник пуст";
            Dispatcher.BeginInvoke(() =>
            {
                if (FkkoGrid.Items.Count > 0)
                    FkkoGrid.ScrollIntoView(FkkoGrid.Items[0]!);
            });
        }
        catch (Exception ex)
        {
            InfoLabel.Text = $"❌ Ошибка загрузки: {ex.Message}";
            FkkoGrid.ItemsSource = null;
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();
}
