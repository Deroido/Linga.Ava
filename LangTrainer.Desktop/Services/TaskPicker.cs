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
    private readonly Queue<string> _recentIds = new();
    private readonly HashSet<string> _recentSet = new(StringComparer.OrdinalIgnoreCase);
    private const int RecentLimit = 30;

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

        for (var i = 0; i < _globalPool.Count * 2; i++)
        {
            var index = _rng.Next(0, _globalPool.Count);
            var task = _globalPool[index];
            if (IsRecent(task.Id)) continue;

            Remember(task.Id);
            return task;
        }

        var fallback = _globalPool[_rng.Next(0, _globalPool.Count)];
        Remember(fallback.Id);
        return fallback;
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

    private bool IsRecent(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _recentSet.Contains(id);
    }

    private void Remember(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        if (_recentSet.Contains(id)) return;
        _recentSet.Add(id);
        _recentIds.Enqueue(id);

        while (_recentIds.Count > RecentLimit)
        {
            var old = _recentIds.Dequeue();
            _recentSet.Remove(old);
        }
    }

    public List<string> GetRecentIds()
    {
        return new List<string>(_recentIds);
    }

    public void RestoreRecentIds(IEnumerable<string> ids)
    {
        _recentIds.Clear();
        _recentSet.Clear();

        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (_recentSet.Contains(id)) continue;
            _recentSet.Add(id);
            _recentIds.Enqueue(id);
        }

        while (_recentIds.Count > RecentLimit)
        {
            var old = _recentIds.Dequeue();
            _recentSet.Remove(old);
        }
    }
}
