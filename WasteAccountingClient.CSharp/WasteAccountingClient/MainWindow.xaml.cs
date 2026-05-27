using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WasteAccountingClient.Controls;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient;

public partial class MainWindow : Window
{
    private readonly List<IRefreshableTab> _refreshableTabs = [];
    private readonly DispatcherTimer _autoRefreshTimer;

    public MainWindow()
    {
        InitializeComponent();
        var user = AppSession.CurrentUser ?? new UserInfo();
        var role = RolePermissions.NormalizeRole(user.Role);

        Title = $"Учёт отходов — {user.FullName}";
        UserNameText.Text = user.FullName;
        UserRoleText.Text = StatusTranslations.RoleDisplayName(role);
        UserIcon.Text = role switch
        {
            "operator" => "📦",
            "chief" => "👷",
            "ecologist" => "🌿",
            "admin" => "⚙",
            _ => "👤"
        };

        if (RolePermissions.CanViewHistory(role))
            AddTab(new HistoryTab(), "📋 История поступлений");

        if (RolePermissions.CanRegisterBatch(role))
            AddTab(new RegisterTab(), "➕ Регистрация партии");

        if (RolePermissions.CanViewOperationsTab(role))
            AddTab(new OperationsTab(), "♻ Операции (переработка/вывоз)");

        if (RolePermissions.CanViewReporting(role))
            AddTab(new ReportTab(), "📊 Отчёты и аналитика");

        if (RolePermissions.CanViewControlQueue(role))
            AddTab(new ControlTab(), "✅ Контроль классификации");

        if (RolePermissions.CanViewFkko(role))
            AddTab(new FkkoTab(), "📚 Справочник ФККО");

        if (MainTabs.Items.Count == 0)
            AddTab(new HistoryTab(), "📋 История поступлений");

        _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
        _autoRefreshTimer.Tick += async (_, _) => await RefreshSelectedTabAsync();
        Loaded += OnMainLoaded;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible && IsLoaded)
                _autoRefreshTimer.Start();
            else
                _autoRefreshTimer.Stop();
        };
        Closed += (_, _) => _autoRefreshTimer.Stop();
    }

    private void OnMainLoaded(object sender, RoutedEventArgs e)
    {
        DesktopShortcutService.TryCreateDesktopShortcutIfMissing();
        _autoRefreshTimer.Start();
        _ = RefreshSelectedTabAsync();
    }

    private void AddTab(UserControl control, string header)
    {
        if (control is IRefreshableTab refreshable)
            _refreshableTabs.Add(refreshable);
        MainTabs.Items.Add(new TabItem { Header = header, Content = control });
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Выйти из учётной записи и вернуться к окну авторизации?",
            "Выход",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
            AppSession.Logout();
    }

    public async void RefreshAllTabs()
    {
        foreach (var tab in _refreshableTabs)
        {
            try
            {
                await tab.LoadDataAsync();
            }
            catch
            {
                // ignore per-tab refresh errors
            }
        }
    }

    private async void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Только смена вкладки: SelectionChanged всплывает и от ComboBox/ListBox внутри вкладок.
        if (!ReferenceEquals(e.Source, MainTabs)) return;
        await RefreshSelectedTabAsync();
    }

    /// <summary>Периодическое обновление активной вкладки (без регистрации партии — чтобы не сбрасывать форму).</summary>
    private async Task RefreshSelectedTabAsync()
    {
        if (!IsLoaded || MainTabs.SelectedItem is not TabItem { Content: IRefreshableTab tab })
            return;
        if (tab is RegisterTab)
            return;
        try
        {
            await tab.LoadDataAsync();
        }
        catch
        {
            // игнорируем ошибки фонового обновления
        }
    }
}
