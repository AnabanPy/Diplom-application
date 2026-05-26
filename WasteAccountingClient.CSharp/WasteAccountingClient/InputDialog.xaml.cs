using System.Windows;

namespace WasteAccountingClient;

public partial class InputDialog : Window
{
    public string Result { get; private set; } = "";

    public InputDialog(string prompt, string title)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Result = InputText.Text;
        DialogResult = true;
        Close();
    }

    public static string? Show(string prompt, string title, Window? owner = null)
    {
        var dlg = new InputDialog(prompt, title) { Owner = owner };
        return dlg.ShowDialog() == true ? dlg.Result : null;
    }
}
