public interface IChatTextSanitizer
{
    string Sanitize(string text);
}

public class ChatTextSanitizer : IChatTextSanitizer
{
    readonly int maxLength;

    public ChatTextSanitizer(int maxLength)
    {
        this.maxLength = maxLength < 1 ? 1 : maxLength;
    }

    public string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string trimmed = text.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed.Substring(0, maxLength);
    }
}
