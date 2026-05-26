using System.Windows;
using System.Windows.Controls;
using WasteAccountingClient.Controls;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient;

public partial class MainWindow : Window
{
    private readonly List<IRefreshableTab> _refreshableTabs = [];

    public MainWindow()
    {
        InitializeComponent();
        var user = AppSession.CurrentUser ?? new UserInfo();
        Title = $"Учёт отходов — {user.FullName}";
        UserNameText.Text = user.FullName;
        UserRoleText.Text = StatusTranslations.RoleDisplayName(user.Role);
        UserIcon.Text = user.Role switch
        {
            "operator" => "📦",
            "chief" => "👷",
            "ecologist" => "🌿",
            "admin" => "⚙",
            _ => "👤"
        };

        AddTab(new HistoryTab(), "📋 История поступлений");
        AddTab(new ReportTab(), "📊 Отчеты и аналитика");

        if (user.Role == "operator")
            AddTab(new RegisterTab(), "➕ Регистрация партии");

        if (user.Role is "chief" or "ecologist" or "admin")
        {
            AddTab(new ControlTab(), "✅ Контроль классификации");
            AddTab(new FkkoTab(), "📚 Справочник ФККО");
        }
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
}
