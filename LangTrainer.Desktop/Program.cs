using System;
using System.IO;
using Avalonia;

namespace LangTrainer.Desktop;

internal static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var path = Path.Combine(baseDir, "bootstrap.log");
                var text = $"{DateTime.Now:O} Startup failed: {ex}\n";
                File.AppendAllText(path, text);
            }
            catch
            {
                // Swallow logging errors to avoid recursive failures.
            }
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
