using System;
using System.IO;
using System.Text.Json;
using LangTrainer.Desktop.Models;

namespace LangTrainer.Desktop.Services;

public sealed class SettingsLoader
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AppSettings LoadFromFile(string path)
    {
        // If settings file does not exist, return default settings.
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(path);

            // Empty or whitespace-only file -> defaults.
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            // If deserialization failed, fall back to defaults.
            return settings ?? new AppSettings();
        }
        catch
        {
            // Any IO or JSON error should never crash the app.
            // We intentionally swallow the exception and return defaults.
            return new AppSettings();
        }
    }
}
