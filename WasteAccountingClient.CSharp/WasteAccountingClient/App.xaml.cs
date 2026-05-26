using System.Windows;

namespace WasteAccountingClient;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Ошибка приложения:\n{args.Exception.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        _ = RunStartupAsync();
    }

    private async Task RunStartupAsync()
    {
        SplashWindow? splash = null;
        try
        {
            splash = new SplashWindow();
            MainWindow = splash;
            splash.Show();

            var health = await AppSession.Client.GetHealthAsync();
            Console.WriteLine($"Сервер: {string.Join(", ", health.Select(kv => $"{kv.Key}={kv.Value}"))}");

            await Task.Delay(1500);

            var login = new LoginWindow();
            MainWindow = login;
            login.Show();
            login.Activate();
            login.Focus();

            splash.Close();
            splash = null;
        }
        catch (Exception ex)
        {
            splash?.Close();
            MessageBox.Show($"Не удалось запустить приложение:\n{ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
