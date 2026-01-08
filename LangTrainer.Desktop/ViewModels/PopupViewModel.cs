using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using LangTrainer.Core.Models;
using LangTrainer.Core.Services;

namespace LangTrainer.Desktop.ViewModels;

public sealed class PopupViewModel : INotifyPropertyChanged
{
    private static readonly HashSet<string> CliticTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "me", "te", "se", "nos", "os", "lo", "la", "los", "las", "le", "les"
    };

    private TrainerTask? _task;
    private string _userAnswer = "";
    private string _statusText = "";
    private bool _wasCorrect;
    private string _correctAnswerText = "";
    private bool _hasResult;
    private bool _hasUserAnswer;
    private string _resultText = "";
    private IBrush _resultBrush = Brushes.Transparent;
    private string _phrasePrefix = "";
    private string _phraseInsert = "";
    private string _phraseSuffix = "";
    private IBrush _insertBrush = Brushes.Transparent;
    private string _promptPrefix = "";
    private string _promptSuffix = "";
    private HashSet<string> _blockedAnswers = new(StringComparer.OrdinalIgnoreCase);
    private bool _alignOptionsLeft;
    private bool _alignOptionsCenter = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string PromptRu => _task?.PromptRu ?? "";
    public string PromptEsTemplate => _task?.PromptEsTemplate ?? "";

    public ObservableCollection<string> Options { get; } = new();

    public string UserAnswer
    {
        get => _userAnswer;
        set
        {
            if (_userAnswer != value)
            {
                _userAnswer = value;
                HasUserAnswer = !string.IsNullOrWhiteSpace(_userAnswer);
                OnPropertyChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    public string CorrectAnswerText
    {
        get => _correctAnswerText;
        private set
        {
            if (_correctAnswerText != value)
            {
                _correctAnswerText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasResult
    {
        get => _hasResult;
        private set
        {
            if (_hasResult != value)
            {
                _hasResult = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSubmitButton));
            }
        }
    }

    public bool HasUserAnswer
    {
        get => _hasUserAnswer;
        private set
        {
            if (_hasUserAnswer != value)
            {
                _hasUserAnswer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSubmitButton));
            }
        }
    }

    public bool ShowSubmitButton => HasUserAnswer && !HasResult;

    public string ResultText
    {
        get => _resultText;
        private set
        {
            if (_resultText != value)
            {
                _resultText = value;
                OnPropertyChanged();
            }
        }
    }

    public IBrush ResultBrush
    {
        get => _resultBrush;
        private set
        {
            if (_resultBrush != value)
            {
                _resultBrush = value;
                OnPropertyChanged();
            }
        }
    }

    public string PhrasePrefix
    {
        get => _phrasePrefix;
        private set
        {
            if (_phrasePrefix != value)
            {
                _phrasePrefix = value;
                OnPropertyChanged();
            }
        }
    }

    public string PhraseInsert
    {
        get => _phraseInsert;
        private set
        {
            if (_phraseInsert != value)
            {
                _phraseInsert = value;
                OnPropertyChanged();
            }
        }
    }

    public string PhraseSuffix
    {
        get => _phraseSuffix;
        private set
        {
            if (_phraseSuffix != value)
            {
                _phraseSuffix = value;
                OnPropertyChanged();
            }
        }
    }

    public IBrush InsertBrush
    {
        get => _insertBrush;
        private set
        {
            if (_insertBrush != value)
            {
                _insertBrush = value;
                OnPropertyChanged();
            }
        }
    }

    public string PromptPrefix
    {
        get => _promptPrefix;
        private set
        {
            if (_promptPrefix != value)
            {
                _promptPrefix = value;
                OnPropertyChanged();
            }
        }
    }

    public string PromptSuffix
    {
        get => _promptSuffix;
        private set
        {
            if (_promptSuffix != value)
            {
                _promptSuffix = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AlignOptionsLeft
    {
        get => _alignOptionsLeft;
        private set
        {
            if (_alignOptionsLeft != value)
            {
                _alignOptionsLeft = value;
                AlignOptionsCenter = !value;
                OnPropertyChanged();
            }
        }
    }

    public bool AlignOptionsCenter
    {
        get => _alignOptionsCenter;
        private set
        {
            if (_alignOptionsCenter != value)
            {
                _alignOptionsCenter = value;
                OnPropertyChanged();
            }
        }
    }

    public bool WasCorrect
    {
        get => _wasCorrect;
        private set
        {
            if (_wasCorrect != value)
            {
                _wasCorrect = value;
                OnPropertyChanged();
            }
        }
    }

    public void SetTask(TrainerTask task)
    {
        _task = task;
        Options.Clear();

        _blockedAnswers = GetBlockedAnswers(task);

        foreach (var opt in task.Options)
        {
            if (_blockedAnswers.Contains(opt)) continue;
            Options.Add(opt);
        }

        UserAnswer = "";
        StatusText = "";
        CorrectAnswerText = "";
        WasCorrect = false;
        HasResult = false;
        HasUserAnswer = false;
        ResultText = "";
        ResultBrush = Brushes.Transparent;
        PhrasePrefix = "";
        PhraseInsert = "";
        PhraseSuffix = "";
        InsertBrush = Brushes.Transparent;
        PromptPrefix = "";
        PromptSuffix = "";
        AlignOptionsLeft = false;
        AlignOptionsCenter = true;

        OnPropertyChanged(nameof(PromptRu));
        OnPropertyChanged(nameof(PromptEsTemplate));

        UpdatePromptParts();
        AlignOptionsLeft = ShouldJoinWithoutSpace(task);
    }

    public bool Submit()
    {
        if (_task == null) return false;

        if (_blockedAnswers.Contains(UserAnswer))
        {
            WasCorrect = false;
            StatusText = "Incorrect";
            ResultText = "Incorrect";
            ResultBrush = Brushes.IndianRed;
            CorrectAnswerText = "Accepted: " + string.Join(", ", GetAllowedAnswers());
            UpdatePhrase(false);
            HasResult = true;
            return false;
        }

        var correct = AnswerValidator.IsCorrect(_task, UserAnswer);
        WasCorrect = correct;

        if (correct)
        {
            StatusText = "Correct";
            CorrectAnswerText = "";
            ResultText = "Correct";
            ResultBrush = Brushes.LimeGreen;
        }
        else
        {
            StatusText = "Incorrect";
            CorrectAnswerText = "Accepted: " + string.Join(", ", GetAllowedAnswers());
            ResultText = "Incorrect";
            ResultBrush = Brushes.IndianRed;
        }

        UpdatePhrase(correct);
        HasResult = true;

        return correct;
    }

    public void UpdatePromptParts()
    {
        if (_task == null)
        {
            PromptPrefix = "";
            PromptSuffix = "";
            return;
        }

        var template = _task.PromptEsTemplate ?? "";
        var placeholder = "___";
        var index = template.IndexOf(placeholder, StringComparison.Ordinal);

        if (index >= 0)
        {
            var prefix = TrimDuplicatePrefixToken(template.Substring(0, index), _task.Options);
            if (ShouldJoinWithoutSpace(_task)) prefix = TrimEndWhitespace(prefix);
            PromptPrefix = prefix;
            PromptSuffix = template.Substring(index + placeholder.Length);
        }
        else
        {
            PromptPrefix = template;
            PromptSuffix = "";
        }
    }

    private void UpdatePhrase(bool correct)
    {
        if (_task == null)
        {
            PhrasePrefix = "";
            PhraseInsert = "";
            PhraseSuffix = "";
            InsertBrush = Brushes.Transparent;
            return;
        }

        var answer = correct
            ? UserAnswer
            : (_task.AcceptableAnswers.Count > 0 ? _task.AcceptableAnswers[0] : UserAnswer);

        var template = _task.PromptEsTemplate ?? "";
        var placeholder = "___";
        var index = template.IndexOf(placeholder, StringComparison.Ordinal);

        if (index >= 0)
        {
            var prefix = TrimDuplicatePrefixToken(template.Substring(0, index), new List<string> { answer });
            if (ShouldJoinWithoutSpace(_task)) prefix = TrimEndWhitespace(prefix);
            PhrasePrefix = prefix;
            PhraseInsert = answer;
            PhraseSuffix = template.Substring(index + placeholder.Length);
        }
        else
        {
            PhrasePrefix = template;
            PhraseInsert = answer;
            PhraseSuffix = "";
        }

        InsertBrush = correct ? Brushes.LimeGreen : Brushes.IndianRed;
    }

    private List<string> GetAllowedAnswers()
    {
        if (_task == null) return new List<string>();

        var list = new List<string>();
        foreach (var ans in _task.AcceptableAnswers)
        {
            if (_blockedAnswers.Contains(ans)) continue;
            list.Add(ans);
        }

        return list.Count > 0 ? list : new List<string>(_task.AcceptableAnswers);
    }

    private static HashSet<string> GetBlockedAnswers(TrainerTask task)
    {
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefix = task.PromptEsTemplate ?? "";
        var placeholder = "___";
        var index = prefix.IndexOf(placeholder, StringComparison.Ordinal);
        if (index < 0) return blocked;

        var before = prefix.Substring(0, index).TrimEnd();
        var token = GetLastToken(before);
        if (token.Length == 0) return blocked;

        foreach (var opt in task.Options)
        {
            var candidate = CleanToken(opt);
            if (candidate.Length == 0) continue;
            if (!CliticTokens.Contains(candidate)) continue;

            if (token.Length > candidate.Length &&
                token.EndsWith(candidate, StringComparison.OrdinalIgnoreCase))
            {
                blocked.Add(opt);
                blocked.Add(candidate);
            }
        }

        return blocked;
    }

    private static string GetLastToken(string value)
    {
        var trimmed = value.TrimEnd();
        if (trimmed.Length == 0) return "";

        var end = trimmed.Length - 1;
        var start = end;
        while (start >= 0 && !char.IsWhiteSpace(trimmed[start]))
        {
            start--;
        }

        return trimmed.Substring(start + 1);
    }

    private static string TrimDuplicatePrefixToken(string prefix, List<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return prefix;

        var trimmed = prefix.TrimEnd();
        var lastSpace = LastWhitespaceIndex(trimmed);
        if (lastSpace < 0)
        {
            var onlyToken = CleanToken(trimmed);
            if (onlyToken.Length == 0) return prefix;
            foreach (var c in candidates)
            {
                var candidate = CleanToken(c);
                if (candidate.Length == 0) continue;
                if (string.Equals(onlyToken, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return "";
                }
            }
            return prefix;
        }

        var token = trimmed[(lastSpace + 1)..];
        var cleaned = CleanToken(token);
        if (string.IsNullOrEmpty(cleaned)) return prefix;

        foreach (var c in candidates)
        {
            var candidate = CleanToken(c);
            if (candidate.Length == 0) continue;

            if (string.Equals(cleaned, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return prefix.Substring(0, lastSpace + 1);
            }
        }

        return prefix;
    }

    private static int LastWhitespaceIndex(string value)
    {
        for (var i = value.Length - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static string CleanToken(string value)
    {
        return value.Trim().Trim(',', '.', ';', ':', '!', '?', '¿', '¡', '"', '\'');
    }

    private static string TrimEndWhitespace(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var i = value.Length - 1;
        while (i >= 0 && char.IsWhiteSpace(value[i]))
        {
            i--;
        }
        return i == value.Length - 1 ? value : value.Substring(0, i + 1);
    }

    private static bool ShouldJoinWithoutSpace(TrainerTask task)
    {
        if (task == null) return false;
        if (!string.IsNullOrWhiteSpace(task.Type) &&
            task.Type.Contains("verbs.endings", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Heuristic: endings are short and alphabetic; join them to the stem.
        var shortCount = 0;
        foreach (var opt in task.Options)
        {
            var t = CleanToken(opt);
            if (t.Length >= 1 && t.Length <= 4 && IsAlpha(t))
            {
                shortCount++;
            }
        }

        return shortCount >= 2;
    }

    private static bool IsAlpha(string value)
    {
        foreach (var ch in value)
        {
            if (!char.IsLetter(ch)) return false;
        }

        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
