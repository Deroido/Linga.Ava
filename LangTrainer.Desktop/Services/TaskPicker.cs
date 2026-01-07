using System;
using System.Collections.Generic;
using LangTrainer.Core.Models;

namespace LangTrainer.Core.Services;

public sealed class TaskPicker
{
    private readonly Random _rng = new();
    private readonly List<string> _deckIds = new();
    private readonly List<int> _taskCounts = new();
    private Queue<int> _deckQueue = new();
    private List<Queue<int>> _taskQueues = new();

    public TrainerTask PickRandom(TaskDeck deck)
    {
        return PickRandom(new[] { deck });
    }

    public TrainerTask PickRandom(IReadOnlyList<TaskDeck> decks)
    {
        EnsureState(decks);

        if (decks == null || decks.Count == 0)
        {
            throw new InvalidOperationException("No decks available.");
        }

        for (var attempt = 0; attempt < decks.Count; attempt++)
        {
            if (_deckQueue.Count == 0)
            {
                RebuildQueues(decks);
            }

            var deckIndex = _deckQueue.Dequeue();
            var deck = decks[deckIndex];

            if (deck.Tasks.Count == 0)
            {
                _deckQueue.Enqueue(deckIndex);
                continue;
            }

            var queue = _taskQueues[deckIndex];
            if (queue.Count == 0)
            {
                _taskQueues[deckIndex] = BuildTaskQueue(deck.Tasks.Count);
                queue = _taskQueues[deckIndex];
            }

            var taskIndex = queue.Dequeue();
            _deckQueue.Enqueue(deckIndex);
            return deck.Tasks[taskIndex];
        }

        throw new InvalidOperationException("Decks have no tasks.");
    }

    private void EnsureState(IReadOnlyList<TaskDeck> decks)
    {
        if (decks == null)
        {
            RebuildQueues(Array.Empty<TaskDeck>());
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
            RebuildQueues(decks);
        }
    }

    private void RebuildQueues(IReadOnlyList<TaskDeck> decks)
    {
        _deckIds.Clear();
        _taskCounts.Clear();
        _taskQueues = new List<Queue<int>>(decks.Count);

        for (var i = 0; i < decks.Count; i++)
        {
            _deckIds.Add(decks[i].DeckId ?? "");
            _taskCounts.Add(decks[i].Tasks.Count);
            _taskQueues.Add(BuildTaskQueue(decks[i].Tasks.Count));
        }

        _deckQueue = new Queue<int>(ShuffleIndices(decks.Count));
    }

    private Queue<int> BuildTaskQueue(int count)
    {
        return new Queue<int>(ShuffleIndices(count));
    }

    private IEnumerable<int> ShuffleIndices(int count)
    {
        var list = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(i);
        }

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
