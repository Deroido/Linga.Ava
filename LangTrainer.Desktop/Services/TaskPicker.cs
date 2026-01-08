using System;
using System.Collections.Generic;
using LangTrainer.Core.Models;

namespace LangTrainer.Core.Services;

public sealed class TaskPicker
{
    private readonly Random _rng = new();
    private readonly List<TrainerTask> _globalPool = new();
    private readonly List<string> _deckIds = new();
    private readonly List<int> _taskCounts = new();

    public TrainerTask PickRandom(TaskDeck deck)
    {
        return PickRandom(new[] { deck });
    }

    public TrainerTask PickRandom(IReadOnlyList<TaskDeck> decks)
    {
        EnsureState(decks);

        if (_globalPool.Count == 0)
        {
            throw new InvalidOperationException("Decks have no tasks.");
        }

        var index = _rng.Next(0, _globalPool.Count);
        return _globalPool[index];
    }

    private void EnsureState(IReadOnlyList<TaskDeck> decks)
    {
        if (decks == null)
        {
            RebuildPool(Array.Empty<TaskDeck>());
            return;
        }

        var needsRebuild = decks.Count != _deckIds.Count;

        if (!needsRebuild)
        {
            for (var i = 0; i < decks.Count; i++)
            {
                var id = decks[i].DeckId ?? "";
                var count = decks[i].Tasks.Count;

                if (_deckIds[i] != id || _taskCounts[i] != count)
                {
                    needsRebuild = true;
                    break;
                }
            }
        }

        if (needsRebuild)
        {
            RebuildPool(decks);
        }
    }

    private void RebuildPool(IReadOnlyList<TaskDeck> decks)
    {
        _deckIds.Clear();
        _taskCounts.Clear();
        _globalPool.Clear();

        for (var i = 0; i < decks.Count; i++)
        {
            _deckIds.Add(decks[i].DeckId ?? "");
            _taskCounts.Add(decks[i].Tasks.Count);

            foreach (var task in decks[i].Tasks)
            {
                _globalPool.Add(task);
            }
        }
    }
}
