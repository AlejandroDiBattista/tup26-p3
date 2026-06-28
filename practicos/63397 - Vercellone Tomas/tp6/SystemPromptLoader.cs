namespace AsistenteIA.Services;

public static class SystemPromptLoader
{
    public static string Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"No se encontro el prompt de sistema: {path}");

        return File.ReadAllText(path);
    }
}
