internal static class SystemPromptLoader
{
    public static async Task<string> LoadAsync(string path)
    {
        if (!File.Exists(path))
        {
            return "Sos un asistente de consola util, claro y prudente. Respondé en español.";
        }

        return await File.ReadAllTextAsync(path);
    }
}
