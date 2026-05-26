using System.Windows;
using WasteAccountingClient.Api;
using WasteAccountingClient.Models;

namespace WasteAccountingClient;

public static class AppSession
{
    public static ApiClient Client { get; } = new();
    public static UserInfo? CurrentUser { get; set; }
    public static MainWindow? MainWindow { get; set; }

    public static void RefreshAllTabs()
    {
        MainWindow?.RefreshAllTabs();
    }

    public static void Logout()
    {
        Client.SetToken(null);
        CurrentUser = null;

        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        login.Activate();

        MainWindow?.Close();
        MainWindow = null;
    }
}
