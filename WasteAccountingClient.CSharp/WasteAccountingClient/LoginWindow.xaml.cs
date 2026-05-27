using System.Windows;
using System.Windows.Input;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
        EmailInput.Focus();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (Application.Current.MainWindow == this)
            Application.Current.Shutdown();
    }

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            LoginButton_Click(sender, e);
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailInput.Text.Trim();
        var password = PasswordInput.Password;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Введите email и пароль.");
            return;
        }

        HideError();
        LoginButton.IsEnabled = false;
        LoginButton.Content = "Вход…";

        try
        {
            var result = await AppSession.Client.LoginAsync(email, password);
            if (result != null)
            {
                var profile = await AppSession.Client.GetCurrentUserAsync();
                var role = RolePermissions.NormalizeRole(profile?.Role ?? result.Role);
                if (string.IsNullOrEmpty(role))
                    role = InferRoleFromEmail(email);

                AppSession.CurrentUser = new UserInfo
                {
                    Login = email,
                    FullName = profile?.FullName ?? result.FullName ?? email.Split('@')[0],
                    Role = role
                };

                var main = new MainWindow();
                AppSession.MainWindow = main;
                Application.Current.MainWindow = main;
                main.Show();
                main.Activate();
                Close();
                return;
            }

            ShowError("Неверный email или пароль. Проверьте данные и попробуйте снова.");
            PasswordInput.Clear();
            PasswordInput.Focus();
        }
        catch (Exception ex)
        {
            ShowError($"Не удалось подключиться к серверу:\n{ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Content = "Войти в систему";
        }
    }

    private static string InferRoleFromEmail(string email)
    {
        var local = email.Split('@')[0].ToLowerInvariant();
        return local switch
        {
            "operator" => "operator",
            "chief" => "chief",
            "ecologist" => "ecologist",
            "admin" => "admin",
            _ => "operator"
        };
    }
}
