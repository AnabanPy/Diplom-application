using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Controls;

public partial class RegisterTab : UserControl, IRefreshableTab
{
    private List<WasteTypeDto> _wasteTypes = [];
    private List<WasteTypeDto> _filteredTypes = [];
    private WasteTypeDto? _selectedWaste;
    private List<string> _existingCodes = [];
    private const string SearchPlaceholder = "Поиск по коду или названию...";
    private bool _searchIsPlaceholder = true;

    public RegisterTab()
    {
        InitializeComponent();
        HazardCombo.ItemsSource = new[]
        {
            "1 (чрезвычайно опасный)", "2 (высокоопасный)", "3 (умеренно опасный)",
            "4 (малоопасный)", "5 (практически неопасный)"
        };
        HazardCombo.SelectedIndex = 3;
        UnitCombo.ItemsSource = new[] { "тонн", "м³", "кг", "л" };
        UnitCombo.SelectedIndex = 0;
        SetSearchPlaceholder();
        SearchInput.GotFocus += (_, _) => ClearSearchPlaceholder();
        SearchInput.LostFocus += (_, _) => RestoreSearchPlaceholder();
        Loaded += async (_, _) =>
        {
            await LoadWasteTypesAsync();
            await LoadExistingCodesAsync();
        };
    }

    private async Task LoadWasteTypesAsync()
    {
        try
        {
            _wasteTypes = await AppSession.Client.GetWasteTypesAsync();
            _filteredTypes = _wasteTypes.ToList();
            UpdateWasteList();
        }
        catch
        {
            _wasteTypes = [];
            _filteredTypes = [];
            UpdateWasteList();
        }
    }

    private async Task LoadExistingCodesAsync()
    {
        try
        {
            var batches = await AppSession.Client.GetBatchesAsync();
            _existingCodes = batches.Where(b => !string.IsNullOrEmpty(b.Code)).Select(b => b.Code!).ToList();
            GenerateNextCode();
        }
        catch
        {
            CodeInput.Text = "P1";
        }
    }

    public async Task LoadDataAsync()
    {
        await LoadWasteTypesAsync();
        await LoadExistingCodesAsync();
    }

    private void UpdateWasteList()
    {
        WasteList.Items.Clear();
        if (_filteredTypes.Count == 0)
        {
            WasteList.Items.Add("❌ Нет данных. Проверьте подключение к серверу.");
            return;
        }

        foreach (var wt in _filteredTypes)
        {
            var hazard = StatusTranslations.HazardToRoman(wt.HazardClass ?? 4);
            WasteList.Items.Add($"{wt.Code ?? "???"} — {wt.Name ?? "?"} [ФККО: {wt.FkkoCode ?? "—"}] [Кл.{hazard}]");
        }
    }

    private void SetSearchPlaceholder()
    {
        _searchIsPlaceholder = true;
        SearchInput.Text = SearchPlaceholder;
        SearchInput.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x94, 0xa3, 0xb8));
    }

    private void ClearSearchPlaceholder()
    {
        if (!_searchIsPlaceholder) return;
        _searchIsPlaceholder = false;
        SearchInput.Text = "";
        SearchInput.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x0f, 0x17, 0x2a));
    }

    private void RestoreSearchPlaceholder()
    {
        if (!string.IsNullOrWhiteSpace(SearchInput.Text)) return;
        SetSearchPlaceholder();
    }

    private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_searchIsPlaceholder) return;
        var query = SearchInput.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(query))
            _filteredTypes = _wasteTypes.ToList();
        else
        {
            _filteredTypes = _wasteTypes.Where(wt =>
                (wt.Name ?? "").ToLowerInvariant().Contains(query) ||
                (wt.Code ?? "").ToLowerInvariant().Contains(query) ||
                (wt.FkkoCode ?? "").ToLowerInvariant().Contains(query)).ToList();
        }
        UpdateWasteList();
    }

    private void WasteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var idx = WasteList.SelectedIndex;
        if (idx < 0 || idx >= _filteredTypes.Count) return;
        _selectedWaste = _filteredTypes[idx];
        NameInput.Text = _selectedWaste.Name ?? "";
        FkkoInput.Text = _selectedWaste.FkkoCode ?? "";
        var hazard = _selectedWaste.HazardClass ?? 4;
        HazardCombo.SelectedIndex = Math.Clamp(hazard - 1, 0, 4);
    }

    private void GenerateNextCode()
    {
        if (_existingCodes.Count == 0)
        {
            CodeInput.Text = "P1";
            return;
        }

        var numbers = new List<int>();
        foreach (var code in _existingCodes)
        {
            if (code.StartsWith('P') && int.TryParse(code[1..], out var num))
                numbers.Add(num);
        }

        CodeInput.Text = numbers.Count > 0 ? $"P{numbers.Max() + 1}" : "P1";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWaste == null)
        {
            MessageBox.Show("Выберите вид отхода из списка ФККО", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(VolumeInput.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var volume) || volume <= 0)
        {
            MessageBox.Show("Введите положительный объем", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(DeadlineInput.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var deadline) || deadline <= 0)
            deadline = 48;

        var unit = UnitCombo.SelectedItem?.ToString() ?? "тонн";
        var volumeTons = volume;
        if (unit is "кг" or "л") volumeTons = volume / 1000;
        else if (unit == "м³") volumeTons = volume * 1.5;

        var request = new CreateBatchRequest
        {
            Code = CodeInput.Text.Trim(),
            Name = NameInput.Text.Trim(),
            FkkoCode = FkkoInput.Text.Trim(),
            HazardClass = HazardCombo.SelectedIndex + 1,
            VolumeTons = Math.Round(volumeTons, 3),
            StorageDeadlineHours = deadline
        };

        if (!string.IsNullOrWhiteSpace(SourceInput.Text))
            request.SourceDepartment = SourceInput.Text.Trim();

        try
        {
            var response = await AppSession.Client.CreateBatchAsync(request);
            MessageBox.Show($"Партия зарегистрирована!\nКод: {response.Code}\nОбъем: {volume} {unit}",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            if (!string.IsNullOrEmpty(response.Code))
                _existingCodes.Add(response.Code);
            GenerateNextCode();
            ClearForm();
            AppSession.RefreshAllTabs();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось зарегистрировать партию:\n{ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

    private void ClearForm()
    {
        SetSearchPlaceholder();
        NameInput.Clear();
        FkkoInput.Clear();
        VolumeInput.Clear();
        UnitCombo.SelectedIndex = 0;
        DeadlineInput.Text = "48";
        SourceInput.Clear();
        HazardCombo.SelectedIndex = 3;
        _selectedWaste = null;
        _filteredTypes = _wasteTypes.ToList();
        UpdateWasteList();
    }
}
