namespace AsistenteIA.UI;

public static class MarkdownFormatter
{
    public static string FormatTurn(string role, string content)
    {
        return $"## {role}{Environment.NewLine}{content.Trim()}{Environment.NewLine}{Environment.NewLine}";
    }
}
