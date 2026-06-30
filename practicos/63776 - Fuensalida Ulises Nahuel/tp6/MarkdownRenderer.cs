using System.Text;

internal static class MarkdownRenderer
{
    public static string Render(IEnumerable<ChatMessageViewModel> messages)
    {
        var builder = new StringBuilder();

        foreach (var message in messages)
        {
            builder.Append("## ");
            builder.AppendLine(message.Heading);
            builder.AppendLine();
            builder.AppendLine(string.IsNullOrWhiteSpace(message.Content) ? "_Generando respuesta..._" : message.Content.TrimEnd());
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
