// Assets/Scripts/Editor/TMPToMarkdown.cs
using System.Text.RegularExpressions;

public static class TMPToMarkdown
{
    public static string Convert(string tmp)
    {
        if (string.IsNullOrEmpty(tmp))
            return tmp;

        string result = tmp;

        // ќбратные преобразовани€ (только базовые стили)
        result = Regex.Replace(result, @"<b>([^<]+)</b>", "**$1**", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<i>([^<]+)</i>", "*$1*", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<s>([^<]+)</s>", "~~$1~~", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<u>([^<]+)</u>", "__$1__", RegexOptions.IgnoreCase);

        return result;
    }
}