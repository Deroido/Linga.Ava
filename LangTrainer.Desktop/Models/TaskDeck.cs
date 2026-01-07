using System.Collections.Generic;

namespace LangTrainer.Core.Models;

public sealed class TaskDeck
{
    public string DeckId { get; set; } = "";
    public string Title { get; set; } = "";
    public List<TrainerTask> Tasks { get; set; } = new();
}
