using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LangTrainer.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateDiagnostics();
    }

    private void OpenPopup_Click(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ShowPopupNow();
        }
    }

    private void ReloadDecks_Click(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ReloadDecks();
        }

        UpdateDiagnostics();
    }

    private void UpdateDiagnostics()
    {
        if (Application.Current is App app)
        {
            DiagnosticsText.Text = app.GetDiagnosticsText();
        }
        else
        {
            DiagnosticsText.Text = "Diagnostics unavailable.";
        }
    }
}
