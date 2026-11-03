using System.Text.RegularExpressions;

namespace Thread.API.Service;

public static partial class ChatBotMentionParser
{
    [GeneratedRegex(@"(?<![\p{L}\p{N}_])@chatbot(?![\p{L}\p{N}_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MentionRegex();

    public static bool TryExtractPrompt(string content, out string prompt)
    {
        if (!MentionRegex().IsMatch(content))
        {
            prompt = string.Empty;
            return false;
        }

        prompt = MentionRegex().Replace(content, string.Empty).Trim();
        return prompt.Length > 0;
    }
}
