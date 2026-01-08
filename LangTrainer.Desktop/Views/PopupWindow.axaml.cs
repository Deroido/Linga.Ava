using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LangTrainer.Desktop.ViewModels;

namespace LangTrainer.Desktop.Views;

public partial class PopupWindow : Window
{
    private DispatcherTimer? _autoCloseTimer;
    private DispatcherTimer? _autoSubmitTimer;
    private IBrush? _gradientBackground;
    private IBrush? _solidBackground;

    public PopupWindow()
    {
        InitializeComponent();
        _gradientBackground = BackgroundBorder.Background;
        _solidBackground = new SolidColorBrush(Color.Parse("#22262B"));
        SetFadeActive(true);
    }

    private void Options_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not PopupViewModel vm) return;
        if (sender is not ComboBox cb) return;
        if (cb.SelectedItem is not string s) return;

        // Fill answer textbox when user selects an option.
        vm.UserAnswer = s;

        StartAutoSubmitTimer();
    }

    private void ReloadDecks_Click(object? sender, RoutedEventArgs e)
    {
        StopAutoSubmitTimer();
        StopAutoCloseTimer();
        SetFadeActive(true);

        if (Application.Current is App app)
        {
            app.ReloadDecksAndShow();
            Hide();
            app.ShowPopupNow();
        }
    }

    private void Submit_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PopupViewModel vm) return;

        var correct = vm.Submit();

        SetFadeActive(false);

        // Keep visible to show feedback; auto-close after a short delay if correct.
        if (correct)
        {
            StartAutoCloseTimer();
        }
    }

    private void Skip_Click(object? sender, RoutedEventArgs e)
    {
        StopAutoSubmitTimer();
        StopAutoCloseTimer();
        SetFadeActive(true);
        Hide();
    }

    private void AnswerBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Submit_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void SubmitButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Submit_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void WindowDrag_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, sender)) return;
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void SetFadeActive(bool active)
    {
        BackgroundBorder.Background = active ? _gradientBackground : _solidBackground;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopAutoSubmitTimer();
        StopAutoCloseTimer();
        base.OnClosed(e);
    }

    private void StartAutoCloseTimer()
    {
        StopAutoCloseTimer();

        _autoCloseTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Normal, (_, _) =>
        {
            StopAutoCloseTimer();
            SetFadeActive(true);
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

    private void StartAutoSubmitTimer()
    {
        StopAutoSubmitTimer();

        _autoSubmitTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Normal, (_, _) =>
        {
            StopAutoSubmitTimer();
            Submit_Click(this, new RoutedEventArgs());
        });
        _autoSubmitTimer.Start();
    }

    private void StopAutoSubmitTimer()
    {
        if (_autoSubmitTimer == null) return;
        _autoSubmitTimer.Stop();
        _autoSubmitTimer = null;
    }
}
