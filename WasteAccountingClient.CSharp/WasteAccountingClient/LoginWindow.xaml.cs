using System.Windows;
using System.Windows.Input;
using WasteAccountingClient.Models;

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

    private void QuickLoginOperator_Click(object sender, RoutedEventArgs e)
    {
        FillCredentials("operator@example.com", "123546");
        LoginButton_Click(sender, e);
    }

    private void QuickLoginChief_Click(object sender, RoutedEventArgs e)
    {
        FillCredentials("chief@example.com", "123546");
        LoginButton_Click(sender, e);
    }

    private void FillCredentials(string email, string password)
    {
        EmailInput.Text = email;
        PasswordInput.Password = password;
        HideError();
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
                AppSession.CurrentUser = new UserInfo
                {
                    Login = email,
                    FullName = result.FullName ?? email.Split('@')[0],
                    Role = result.Role ?? "operator"
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
            LoginButton.Content = "Войти";
        }
    }
}
