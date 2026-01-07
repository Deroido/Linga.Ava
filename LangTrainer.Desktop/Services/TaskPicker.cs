using System;
using System.Collections.Generic;
using LangTrainer.Core.Models;

namespace LangTrainer.Core.Services;

public sealed class TaskPicker
{
    private readonly Random _rng = new();

    public TrainerTask PickRandom(TaskDeck deck)
    {
        return PickRandom(new[] { deck });
    }

    public TrainerTask PickRandom(IReadOnlyList<TaskDeck> decks)
    {
        if (decks == null || decks.Count == 0)
        {
            throw new InvalidOperationException("No decks available.");
        }

        var totalTasks = 0;
        foreach (var deck in decks)
        {
            totalTasks += deck.Tasks.Count;
        }

        if (totalTasks == 0)
        {
            throw new InvalidOperationException("Decks have no tasks.");
        }

        var index = _rng.Next(0, totalTasks);
        foreach (var deck in decks)
        {
            if (index < deck.Tasks.Count)
            {
                return deck.Tasks[index];
            }

            index -= deck.Tasks.Count;
        }

        throw new InvalidOperationException("Failed to select a task.");
    }
}
