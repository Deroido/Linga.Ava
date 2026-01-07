using Avalonia.Controls;
using Avalonia.Interactivity;
using LangTrainer.Desktop.ViewModels;

namespace LangTrainer.Desktop.Views;

public partial class PopupWindow : Window
{
    public PopupWindow()
    {
        InitializeComponent();
    }

    private void Options_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not PopupViewModel vm) return;
        if (sender is not ComboBox cb) return;
        if (cb.SelectedItem is not string s) return;

        // Fill answer textbox when user selects an option.
        vm.UserAnswer = s;
    }

    private void Submit_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PopupViewModel vm) return;

        var correct = vm.Submit();

        // Auto-close quickly if correct; keep visible if incorrect.
        if (correct)
        {
            Hide();
        }
    }

    private void Skip_Click(object? sender, RoutedEventArgs e)
    {
        Hide();
    }
}
