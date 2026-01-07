using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LangTrainer.Core.Models;

namespace LangTrainer.Core.Services;

public sealed class DeckLoader
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TaskDeck LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Deck file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var deck = JsonSerializer.Deserialize<TaskDeck>(json, _jsonOptions);

        if (deck == null)
        {
            throw new InvalidOperationException($"Failed to parse deck json: {path}");
        }

        return deck;
    }

    public List<TaskDeck> LoadAllFromDirectory(string directoryPath)
    {
        var decks = new List<TaskDeck>();

        if (!Directory.Exists(directoryPath))
        {
            return decks;
        }

        var files = Directory.GetFiles(directoryPath, "tasks.*.json", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            try
            {
                var deck = LoadFromFile(file);
                decks.Add(deck);
            }
            catch
            {
                // Ignore invalid deck files to keep app running.
            }
        }

        return decks;
    }
}
