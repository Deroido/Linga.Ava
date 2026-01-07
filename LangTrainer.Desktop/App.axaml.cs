using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using LangTrainer.Core.Models;
using LangTrainer.Core.Services;
using LangTrainer.Desktop.Models;
using LangTrainer.Desktop.Services;
using LangTrainer.Desktop.ViewModels;
using LangTrainer.Desktop.Views;

namespace LangTrainer.Desktop;

public partial class App : Application
{
    private PopupWindow? _popupWindow;
    private PopupViewModel? _popupVm;

    private PopupScheduler? _scheduler;
    private TimeSpan _interval;
    private TrayIcon? _trayIcon;

    private List<TaskDeck> _decks = new();
    private readonly DeckLoader _deckLoader = new();
    private readonly TaskPicker _picker = new();
    private string _lastLoadStatus = "Decks not loaded yet.";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                // Keep app alive even if windows are hidden.
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

                _popupVm = new PopupViewModel();
                _popupWindow = new PopupWindow
                {
                    DataContext = _popupVm
                };

                // Do not show window at start.
                _popupWindow.Hide();

                LoadData();

                var settings = LoadSettings();
                _interval = ResolveInterval(settings);

                _scheduler = new PopupScheduler();
                _scheduler.Start(_interval, ShowNextTaskPopup);

                _popupWindow.PropertyChanged += PopupWindowOnPropertyChanged;

                try
                {
                    InitializeTray();
                }
                catch (Exception ex)
                {
                    LogStartupError("Tray init failed", ex);
                }

                ShowPopupNow();
            }
            catch (Exception ex)
            {
                LogStartupError("Startup failed", ex);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void LoadData()
    {
        var baseDir = AppContext.BaseDirectory;
        var dataDir = Path.Combine(baseDir, "Data");
        var decks = new List<TaskDeck>();
        var taskCount = 0;
        var failedCount = 0;
        var files = Array.Empty<string>();

        if (Directory.Exists(dataDir))
        {
            files = Directory.GetFiles(dataDir, "tasks.*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var deck = _deckLoader.LoadFromFile(file);
                    decks.Add(deck);
                    taskCount += deck.Tasks.Count;
                }
                catch
                {
                    failedCount++;
                }
            }
        }

        _decks = decks;
        _lastLoadStatus = $"Decks loaded: {decks.Count}/{files.Length}. Tasks: {taskCount}. Errors: {failedCount}. Updated: {DateTime.Now:HH:mm:ss}";
    }

    private AppSettings LoadSettings()
    {
        var baseDir = AppContext.BaseDirectory;
        var settingsPath = Path.Combine(baseDir, "Data", "appsettings.json");
        return new SettingsLoader().LoadFromFile(settingsPath);
    }

    private static TimeSpan ResolveInterval(AppSettings settings)
    {
        if (settings.DebugIntervalSeconds > 0)
        {
            return TimeSpan.FromSeconds(settings.DebugIntervalSeconds);
        }

        var minutes = settings.IntervalMinutes <= 0 ? 120 : settings.IntervalMinutes;
        return TimeSpan.FromMinutes(minutes);
    }

    private void ShowNextTaskPopup()
    {
        if (_popupWindow == null || _popupVm == null) return;
        if (_popupWindow.IsVisible)
        {
            _scheduler?.Stop();
            return;
        }
        if (_decks.Count == 0) return;

        _scheduler?.Stop();

        var task = _picker.PickRandom(_decks);
        _popupVm.SetTask(task);

        // Show popup (topmost). Focus textbox is handled by user click for now (we can improve).
        _popupWindow.Show();
        _popupWindow.Activate();

        PositionPopupWindow();
        _popupWindow.LayoutUpdated += PopupWindowOnLayoutUpdated;
    }

    private void PopupWindowOnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_popupWindow == null) return;
        _popupWindow.LayoutUpdated -= PopupWindowOnLayoutUpdated;
        PositionPopupWindow();
    }

    private void PopupWindowOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_popupWindow == null || _scheduler == null) return;
        if (e.Property != Window.IsVisibleProperty) return;

        if (!_popupWindow.IsVisible)
        {
            _scheduler.Start(_interval, ShowNextTaskPopup);
        }
    }

    public void ShowPopupNow()
    {
        ShowNextTaskPopup();
    }

    public void ReloadDecks()
    {
        LoadData();
    }

    public void ReloadDecksAndShow()
    {
        LoadData();
        ShowNextTaskPopup();
    }

    public string GetDiagnosticsText()
    {
        return _lastLoadStatus;
    }

    private void InitializeTray()
    {
        var menu = new NativeMenu();
        var openNowItem = new NativeMenuItem { Header = "Open task now" };
        openNowItem.Click += TrayOpenNow_Click;
        menu.Add(openNowItem);

        var intervalMenu = new NativeMenu();
        intervalMenu.Add(CreateIntervalItem("5 min"));
        intervalMenu.Add(CreateIntervalItem("10 min"));
        intervalMenu.Add(CreateIntervalItem("30 min"));
        intervalMenu.Add(CreateIntervalItem("60 min"));
        intervalMenu.Add(CreateIntervalItem("120 min"));
        menu.Add(new NativeMenuItem { Header = "Interval", Menu = intervalMenu });

        menu.Add(new NativeMenuItemSeparator());
        var exitItem = new NativeMenuItem { Header = "Exit" };
        exitItem.Click += TrayExit_Click;
        menu.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "LangTrainer",
            Menu = menu,
            Icon = LoadTrayIcon()
        };
    }

    private NativeMenuItem CreateIntervalItem(string header)
    {
        var item = new NativeMenuItem { Header = header };
        item.Click += TrayInterval_Click;
        return item;
    }

    private static WindowIcon LoadTrayIcon()
    {
        var uri = new Uri("avares://LangTrainer.Desktop/Assets/tray.png");
        using var stream = AssetLoader.Open(uri);
        return new WindowIcon(new Bitmap(stream));
    }

    private void TrayOpenNow_Click(object? sender, EventArgs e)
    {
        ShowPopupNow();
    }

    private void TrayInterval_Click(object? sender, EventArgs e)
    {
        if (sender is not NativeMenuItem item) return;
        var header = item.Header?.ToString() ?? "";
        var minutesText = header.Replace("min", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!int.TryParse(minutesText, out var minutes)) return;
        if (minutes <= 0) return;

        _interval = TimeSpan.FromMinutes(minutes);

        if (_popupWindow != null && _popupWindow.IsVisible)
        {
            _scheduler?.Stop();
            return;
        }

        _scheduler?.Start(_interval, ShowNextTaskPopup);
    }

    private void TrayExit_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void PositionPopupWindow()
    {
        if (_popupWindow == null) return;

        var screens = _popupWindow.Screens;
        var screen = screens.ScreenFromWindow(_popupWindow) ?? screens.Primary;
        if (screen == null) return;

        var working = screen.WorkingArea;
        var margin = 12;
        var width = (int)Math.Round(_popupWindow.Width);
        var height = (int)Math.Round(_popupWindow.Height);
        if (width <= 0)
        {
            width = (int)Math.Round(_popupWindow.Bounds.Width);
        }
        if (height <= 0)
        {
            height = (int)Math.Round(_popupWindow.Bounds.Height);
        }
        if (width <= 0 || height <= 0) return;

        var x = working.Right - width - margin;
        var y = working.Bottom - height - margin;

        _popupWindow.Position = new PixelPoint(x, y);
    }

    private static void LogStartupError(string message, Exception ex)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "startup.log");
            var text = $"{DateTime.Now:O} {message}: {ex}\n";
            File.AppendAllText(path, text);
        }
        catch
        {
            // Swallow logging errors to avoid recursive failures.
        }
    }
}
