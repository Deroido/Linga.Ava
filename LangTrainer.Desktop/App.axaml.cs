using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
    private readonly Random _rng = new();
    private string _lastLoadStatus = "Decks not loaded yet.";
    private readonly List<(NativeMenuItem Item, string Label)> _intervalItems = new();
    private PixelPoint? _savedPopupPosition;
    private bool _handlingPositionChange;

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
            _popupWindow.PropertyChanged += PopupWindowOnPropertyChanged;
            _popupWindow.PositionChanged += PopupWindowOnPositionChanged;

            LoadData();
            LoadWindowPosition();
            LoadRecentHistory();

                var settings = LoadSettings();
                _interval = ResolveInterval(settings);

            _scheduler = new PopupScheduler();
            _scheduler.Start(_interval, ShowNextTaskPopup);

            try
            {
                InitializeTray();
            }
            catch (Exception ex)
            {
                LogStartupError("Tray init failed", ex);
            }

            UpdateIntervalChecks();
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
        SaveRecentHistory();
        task.Options = BuildOptions(task, 6);
        _popupVm.SetTask(task);

        // Show popup (topmost). Focus textbox is handled by user click for now (we can improve).
        _popupWindow.Show();

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

    private void PopupWindowOnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (_popupWindow == null) return;
        if (_handlingPositionChange) return;

        _savedPopupPosition = e.Point;
        SaveWindowPosition(e.Point);
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
        intervalMenu.Add(CreateIntervalItem("1 sec"));
        intervalMenu.Add(CreateIntervalItem("3 sec"));
        intervalMenu.Add(CreateIntervalItem("5 sec"));
        intervalMenu.Add(CreateIntervalItem("10 sec"));
        intervalMenu.Add(CreateIntervalItem("30 sec"));
        intervalMenu.Add(CreateIntervalItem("1 min"));
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
        _intervalItems.Add((item, header));
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
        var interval = ParseIntervalHeader(header);
        if (interval <= TimeSpan.Zero) return;

        _interval = interval;
        SaveIntervalToSettings(interval);
        UpdateIntervalChecks();

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

        if (_savedPopupPosition.HasValue)
        {
            _handlingPositionChange = true;
            _popupWindow.Position = _savedPopupPosition.Value;
            _handlingPositionChange = false;
            return;
        }

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

        var target = _savedPopupPosition ?? new PixelPoint(x, y);

        _handlingPositionChange = true;
        _popupWindow.Position = target;
        _handlingPositionChange = false;
    }

    private List<string> BuildOptions(TrainerTask task, int targetCount)
    {
        if (task.Options.Count >= targetCount)
        {
            return Dedup(task.Options);
        }

        var key = string.IsNullOrWhiteSpace(task.Group) ? task.Type : task.Group;
        var pool = new List<string>();

        foreach (var deck in _decks)
        {
            foreach (var t in deck.Tasks)
            {
                if (!IsSameTheme(t, key)) continue;
                if (t.Options.Count == 0) continue;
                pool.AddRange(t.Options);
            }
        }

        var result = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var opt in task.Options)
        {
            if (used.Add(opt))
            {
                result.Add(opt);
            }
        }

        Shuffle(pool);
        foreach (var opt in pool)
        {
            if (result.Count >= targetCount) break;
            if (used.Add(opt))
            {
                result.Add(opt);
            }
        }

        Shuffle(result);
        return result;
    }

    private static bool IsSameTheme(TrainerTask task, string key)
    {
        if (!string.IsNullOrWhiteSpace(task.Group))
        {
            return string.Equals(task.Group, key, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(task.Type, key, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> Dedup(List<string> options)
    {
        var result = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var opt in options)
        {
            if (used.Add(opt))
            {
                result.Add(opt);
            }
        }

        return result;
    }

    private void Shuffle(List<string> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private static TimeSpan ParseIntervalHeader(string header)
    {
        var text = header.Replace("✓", "", StringComparison.Ordinal).Trim();
        if (text.Length == 0) return TimeSpan.Zero;

        if (text.Contains("sec", StringComparison.OrdinalIgnoreCase))
        {
            var valueText = text.Replace("sec", "", StringComparison.OrdinalIgnoreCase).Trim();
            return int.TryParse(valueText, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : TimeSpan.Zero;
        }

        if (text.Contains("min", StringComparison.OrdinalIgnoreCase))
        {
            var valueText = text.Replace("min", "", StringComparison.OrdinalIgnoreCase).Trim();
            return int.TryParse(valueText, out var minutes) && minutes > 0
                ? TimeSpan.FromMinutes(minutes)
                : TimeSpan.Zero;
        }

        return TimeSpan.Zero;
    }

    private void UpdateIntervalChecks()
    {
        foreach (var (item, label) in _intervalItems)
        {
            var interval = ParseIntervalHeader(label);
            item.Header = interval == _interval ? $"✓ {label}" : label;
        }
    }

    private void SaveIntervalToSettings(TimeSpan interval)
    {
        try
        {
            var settings = new AppSettings();
            if (interval.TotalSeconds < 60)
            {
                settings.DebugIntervalSeconds = Math.Max(1, (int)interval.TotalSeconds);
                settings.IntervalMinutes = 0;
            }
            else
            {
                settings.IntervalMinutes = Math.Max(1, (int)interval.TotalMinutes);
                settings.DebugIntervalSeconds = 0;
            }

            var baseDir = AppContext.BaseDirectory;
            var dataDir = Path.Combine(baseDir, "Data");
            Directory.CreateDirectory(dataDir);

            var path = Path.Combine(dataDir, "appsettings.json");
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore persistence errors.
        }
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

    private void LoadWindowPosition()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "Data", "windowstate.json");
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<WindowState>(json);
            if (state == null) return;

            _savedPopupPosition = new PixelPoint(state.X, state.Y);
        }
        catch
        {
            // Ignore failures.
        }
    }

    private void SaveWindowPosition(PixelPoint position)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var dataDir = Path.Combine(baseDir, "Data");
            Directory.CreateDirectory(dataDir);
            var path = Path.Combine(dataDir, "windowstate.json");

            var state = new WindowState { X = position.X, Y = position.Y };
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore failures.
        }
    }

    private sealed class WindowState
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private void LoadRecentHistory()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "Data", "recent.json");
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var ids = JsonSerializer.Deserialize<List<string>>(json);
            if (ids == null) return;

            _picker.RestoreRecentIds(ids);
        }
        catch
        {
            // Ignore failures.
        }
    }

    private void SaveRecentHistory()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var dataDir = Path.Combine(baseDir, "Data");
            Directory.CreateDirectory(dataDir);
            var path = Path.Combine(dataDir, "recent.json");

            var ids = _picker.GetRecentIds();
            var json = JsonSerializer.Serialize(ids, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore failures.
        }
    }
}
