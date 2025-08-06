namespace MyWebApp.Extensions.Validation;

public static class CommentFilter
{
    public static readonly List<string> BannedWords = ["địt", "chó", "cặc", "lồn", "fuck", "shit", "dm", "đmm"];

    public static bool ContainsBannedWord(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var lowered = content.ToLowerInvariant();

        foreach (var word in BannedWords)
        {
            if (lowered.Contains(word))
                return true;
        }

        return false;
    }

    public static bool ContainsLink(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var regex = new System.Text.RegularExpressions.Regex(@"(http|https)://[^\s]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return regex.IsMatch(content);
    }
}
