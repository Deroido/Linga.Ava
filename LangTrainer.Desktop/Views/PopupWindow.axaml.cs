using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LangTrainer.Desktop.ViewModels;

namespace LangTrainer.Desktop.Views;

public partial class PopupWindow : Window
{
    private DispatcherTimer? _autoCloseTimer;

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

    private void ReloadDecks_Click(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ReloadDecks();
        }
    }

    private void Submit_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PopupViewModel vm) return;

        var correct = vm.Submit();

        // Keep visible to show feedback; auto-close after a short delay if correct.
        if (correct)
        {
            StartAutoCloseTimer();
        }
    }

    private void Skip_Click(object? sender, RoutedEventArgs e)
    {
        StopAutoCloseTimer();
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        StopAutoCloseTimer();
        base.OnClosed(e);
    }

    private void StartAutoCloseTimer()
    {
        StopAutoCloseTimer();

        _autoCloseTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Normal, (_, _) =>
        {
            StopAutoCloseTimer();
            Hide();
        });
        _autoCloseTimer.Start();
    }

    private void StopAutoCloseTimer()
    {
        if (_autoCloseTimer == null) return;
        _autoCloseTimer.Stop();
        _autoCloseTimer = null;
    }
}
