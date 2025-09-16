using System.Text.RegularExpressions;
using System.Web;

namespace MyWebApp.Extensions.Helpers;

public static class StringHelper
{
    public static string ToPlainText(string input, int maxLength = 150)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // Decode HTML entity (&agrave; => à)
        string decoded = HttpUtility.HtmlDecode(input);

        // Loại bỏ các thẻ HTML (<p>, <strong>, <img>...)
        string plainText = Regex.Replace(decoded, "<.*?>", string.Empty);

        // Cắt độ dài preview
        return plainText.Length > maxLength ? plainText.Substring(0, maxLength) + "..." : plainText;
    }
}