using System.Collections.Generic;

namespace LangTrainer.Core.Models;

public sealed class TrainerTask
{
    public string Id { get; set; } = "";
    public string Group { get; set; } = "";
    public string Type { get; set; } = "";
    public string PromptRu { get; set; } = "";
    public string PromptEsTemplate { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public List<string> AcceptableAnswers { get; set; } = new();
    public string? Note { get; set; }
}
