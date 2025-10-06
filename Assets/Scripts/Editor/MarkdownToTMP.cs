// Assets/Scripts/Editor/MarkdownToTMP.cs
using System.Text.RegularExpressions;
using System;

public static class MarkdownToTMP
{
    public static string Convert(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return markdown;

        string result = markdown;

        // --- Горизонтальная линия
        result = Regex.Replace(result, @"^\s*---\s*$", "<sprite name=\"Divider\">", RegexOptions.Multiline);

        // --- Заголовки
        result = Regex.Replace(result, @"^######\s+(.*)$", "<size=90%><b>$1</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^#####\s+(.*)$", "<size=100%><b>$1</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^####\s+(.*)$", "<size=105%><b>$1</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^###\s+(.*)$", "<size=110%><b>$1</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^##\s+(.*)$", "<size=115%><b>$1</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^#\s+(.*)$", "<size=120%><b>$1</b></size>", RegexOptions.Multiline);

        // --- Списки
        result = Regex.Replace(result, @"^\s*-\s+(.+)$", "• $1\n", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^\s*\d+\.\s+(.+)$", "$0\n", RegexOptions.Multiline);

        // --- Цитаты
        result = Regex.Replace(result, @"^\s*>\s+(.+)$", "<i><color=#888888>«$1»</color></i>", RegexOptions.Multiline);

        // --- Блок кода
        result = Regex.Replace(result, @"```([\s\S]*?)```", match =>
        {
            string code = match.Groups[1].Value;
            return $"<color=#ddd>{EscapeTMP(code)}</color>";
        }, RegexOptions.Multiline);

        // --- Инлайн-код
        result = Regex.Replace(result, @"`([^`]+)`", "<color=#aaa>$1</color>");

        // --- Ссылки
        result = Regex.Replace(result, @"\[([^\]]+)\]\(([^)]+)\)", "<link=\"$2\"><u>$1</u></link>");

        // --- Изображения
        result = Regex.Replace(result, @"!\[([^\]]*)\]\(([^)]+)\)", "<sprite name=\"$1\">");

        // --- Стили с вложенностью (3 прохода)
        for (int i = 0; i < 3; i++)
        {
            result = Regex.Replace(result, @"\*\*([^*]+)\*\*", "<b>$1</b>");
            result = Regex.Replace(result, @"\*([^*]+)\*", "<i>$1</i>");
            result = Regex.Replace(result, @"~~([^~]+)~~", "<s>$1</s>");
            result = Regex.Replace(result, @"__([^_]+)__", "<u>$1</u>");
        }

        return result.TrimEnd('\n');
    }

    private static string EscapeTMP(string text)
    {
        return text?.Replace("<", "<").Replace(">", ">");
    }
}
