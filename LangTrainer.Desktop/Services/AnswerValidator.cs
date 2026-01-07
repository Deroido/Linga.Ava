using LangTrainer.Core.Models;

namespace LangTrainer.Core.Services;

public static class AnswerValidator
{
    public static bool IsCorrect(TrainerTask task, string userAnswer)
    {
        if (task.AcceptableAnswers.Count == 0) return false;

        var userNorm = TextNormalizer.NormalizeForCompare(userAnswer);

        foreach (var a in task.AcceptableAnswers)
        {
            var aNorm = TextNormalizer.NormalizeForCompare(a);
            if (userNorm == aNorm)
            {
                return true;
            }
        }

        return false;
    }
}
