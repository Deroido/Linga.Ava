using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using LangTrainer.Core.Models;
using LangTrainer.Core.Services;

namespace LangTrainer.Desktop.ViewModels;

public sealed class PopupViewModel : INotifyPropertyChanged
{
    private TrainerTask? _task;
    private string _userAnswer = "";
    private string _statusText = "";
    private bool _wasCorrect;
    private string _correctAnswerText = "";
    private bool _hasResult;
    private string _resultText = "";
    private IBrush _resultBrush = Brushes.Transparent;
    private string _phrasePrefix = "";
    private string _phraseInsert = "";
    private string _phraseSuffix = "";
    private IBrush _insertBrush = Brushes.Transparent;
    private string _promptPrefix = "";
    private string _promptSuffix = "";

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
            }
        }
    }

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

        foreach (var opt in task.Options)
        {
            Options.Add(opt);
        }

        UserAnswer = "";
        StatusText = "";
        CorrectAnswerText = "";
        WasCorrect = false;
        HasResult = false;
        ResultText = "";
        ResultBrush = Brushes.Transparent;
        PhrasePrefix = "";
        PhraseInsert = "";
        PhraseSuffix = "";
        InsertBrush = Brushes.Transparent;
        PromptPrefix = "";
        PromptSuffix = "";

        OnPropertyChanged(nameof(PromptRu));
        OnPropertyChanged(nameof(PromptEsTemplate));

        UpdatePromptParts();
    }

    public bool Submit()
    {
        if (_task == null) return false;

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
            CorrectAnswerText = "Accepted: " + string.Join(", ", _task.AcceptableAnswers);
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
            PromptPrefix = template.Substring(0, index);
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
            PhrasePrefix = template.Substring(0, index);
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

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
