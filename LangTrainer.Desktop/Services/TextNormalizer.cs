using System.Globalization;
using System.Text;

namespace LangTrainer.Core.Services;

public static class TextNormalizer
{
    public static string NormalizeForCompare(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // 1) Trim and lower
        var s = input.Trim().ToLowerInvariant();

        // 2) Remove diacritics (áéíóúüñ -> aeiouun for comparison)
        s = RemoveDiacritics(s);

        // 3) Collapse spaces
        s = CollapseWhitespace(s);

        return s;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CollapseWhitespace(string text)
    {
        var sb = new StringBuilder(text.Length);
        var prevIsSpace = false;

        foreach (var ch in text)
        {
            var isSpace = char.IsWhiteSpace(ch);
            if (isSpace)
            {
                if (!prevIsSpace) sb.Append(' ');
                prevIsSpace = true;
            }
            else
            {
                sb.Append(ch);
                prevIsSpace = false;
            }
        }

        return sb.ToString().Trim();
    }
}
