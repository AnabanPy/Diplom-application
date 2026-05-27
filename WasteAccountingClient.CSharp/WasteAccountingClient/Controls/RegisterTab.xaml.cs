using System.Globalization;
using System.Net;
using System.Net.Http;
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
    private bool _catalogAvailable;
    private bool _localCatalog;
    private const int LargeCatalogThreshold = 100;
    private const int SearchMinLength = 2;
    private const int MaxListResults = 200;
    private const string SearchPlaceholder = "Поиск по коду или названию…";
    private bool _searchIsPlaceholder = true;
    private readonly List<WasteTypeDto> _displayTypes = [];

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
        _localCatalog = false;
        try
        {
            _wasteTypes = (await AppSession.Client.GetWasteTypesAsync()).PrepareForRegistration();
            _catalogAvailable = _wasteTypes.Count > 0;
            _filteredTypes = [];
            ApplyCatalogMode();
            UpdateWasteList();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden
                                              || (ex.Message?.Contains("403") ?? false)
                                              || (ex.Message?.Contains("Недостаточно прав", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            if (TryLoadLocalCatalog())
                return;
            SetManualEntryMode();
        }
        catch
        {
            if (TryLoadLocalCatalog())
                return;
            _wasteTypes = [];
            _filteredTypes = [];
            _catalogAvailable = false;
            ApplyCatalogMode();
            UpdateWasteList();
        }
    }

    private bool TryLoadLocalCatalog()
    {
        var local = FkkoLocalCatalog.TryLoad();
        if (local is not { Count: > 0 })
            return false;

        _localCatalog = true;
        _wasteTypes = local.PrepareForRegistration();
        _filteredTypes = [];
        _catalogAvailable = true;
        ApplyCatalogMode();
        UpdateWasteList();
        return true;
    }

    private void SetManualEntryMode()
    {
        _catalogAvailable = false;
        _wasteTypes = [];
        _filteredTypes = [];
        ApplyCatalogMode();
    }

    private void ApplyCatalogMode()
    {
        if (_catalogAvailable)
        {
            FkkoCatalogPanel.Visibility = Visibility.Visible;
            RegisterSubtitle.Text = _localCatalog
                ? $"Справочник: {_wasteTypes.Count} видов отходов (без групп каталога). Используйте поле поиска."
                : $"Справочник: {_wasteTypes.Count} видов отходов. Используйте поле поиска.";
            NameInput.IsReadOnly = true;
            FkkoInput.IsReadOnly = true;
            return;
        }

        FkkoCatalogPanel.Visibility = Visibility.Collapsed;
        RegisterSubtitle.Text = "Заполните наименование, код ФККО и параметры партии вручную";
        NameInput.IsReadOnly = false;
        FkkoInput.IsReadOnly = false;
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
        _displayTypes.Clear();
        if (!_catalogAvailable) return;

        if (_wasteTypes.Count > LargeCatalogThreshold)
        {
            var query = _searchIsPlaceholder ? "" : SearchInput.Text.Trim();
            if (query.Length < SearchMinLength)
            {
                WasteList.Items.Add($"Справочник: {_wasteTypes.Count} поз. Введите в поле поиска часть кода или названия.");
                return;
            }

            _filteredTypes = FilterWasteTypes(query).Take(MaxListResults).ToList();
        }
        else if (_filteredTypes.Count == 0)
        {
            _filteredTypes = _wasteTypes.ToList();
        }

        if (_filteredTypes.Count == 0)
        {
            WasteList.Items.Add("Ничего не найдено");
            return;
        }

        foreach (var wt in _filteredTypes)
        {
            _displayTypes.Add(wt);
            var hazard = StatusTranslations.HazardToRoman(wt.HazardClass ?? 4);
            WasteList.Items.Add($"{wt.Code ?? "???"} — {wt.Name ?? "?"} [ФККО: {wt.FkkoCode ?? "—"}] [Кл.{hazard}]");
        }

        if (_wasteTypes.Count > LargeCatalogThreshold && _filteredTypes.Count >= MaxListResults)
            WasteList.Items.Add($"... показаны первые {MaxListResults} результатов, уточните запрос");
    }

    private List<WasteTypeDto> FilterWasteTypes(string query)
    {
        var normalized = query.ToLowerInvariant();
        return _wasteTypes.Where(wt =>
            (wt.Name ?? "").ToLowerInvariant().Contains(normalized) ||
            (wt.Code ?? "").ToLowerInvariant().Contains(normalized) ||
            (wt.FkkoCode ?? "").ToLowerInvariant().Contains(normalized)).ToList();
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
        if (_searchIsPlaceholder || !_catalogAvailable) return;
        _filteredTypes = [];
        UpdateWasteList();
    }

    private void WasteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_catalogAvailable) return;
        var idx = WasteList.SelectedIndex;
        if (idx < 0 || idx >= _displayTypes.Count) return;
        _selectedWaste = _displayTypes[idx];
        NameInput.Text = _selectedWaste.Name ?? "";
        FkkoInput.Text = _selectedWaste.FkkoCode ?? "";
        var hazard = _selectedWaste.HazardClass is > 0 and <= 5 ? _selectedWaste.HazardClass.Value : 4;
        HazardCombo.SelectedIndex = hazard - 1;
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
        if (_catalogAvailable && _selectedWaste == null)
        {
            MessageBox.Show("Выберите вид отхода из списка ФККО", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_catalogAvailable && _selectedWaste?.HazardClass == 0)
        {
            MessageBox.Show("Выберите конкретный вид отхода (не группу каталога)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(NameInput.Text) || string.IsNullOrWhiteSpace(FkkoInput.Text))
        {
            MessageBox.Show("Укажите наименование и код ФККО", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        var apiUnit = StatusTranslations.UiUnitToApi(unit);
        var volumeTons = volume;
        if (unit is "кг" or "л") volumeTons = volume / 1000;
        else if (unit == "м³") volumeTons = volume * 1.5;

        var request = new CreateBatchRequest
        {
            Code = CodeInput.Text.Trim(),
            Name = NameInput.Text.Trim(),
            FkkoCode = FkkoInput.Text.Trim(),
            HazardClass = HazardCombo.SelectedIndex + 1,
            Volume = Math.Round(volume, 3),
            VolumeUnit = apiUnit,
            VolumeTons = Math.Round(volumeTons, 3),
            StorageDeadlineHours = deadline
        };

        if (!string.IsNullOrWhiteSpace(SourceInput.Text))
            request.SourceDepartment = SourceInput.Text.Trim();

        try
        {
            SaveButton.IsEnabled = false;
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
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

    private void ClearForm()
    {
        if (_catalogAvailable)
            SetSearchPlaceholder();
        NameInput.Clear();
        FkkoInput.Clear();
        VolumeInput.Clear();
        UnitCombo.SelectedIndex = 0;
        DeadlineInput.Text = "48";
        SourceInput.Clear();
        HazardCombo.SelectedIndex = 3;
        _selectedWaste = null;
        if (_catalogAvailable)
        {
            _filteredTypes = [];
            UpdateWasteList();
        }
    }
}
