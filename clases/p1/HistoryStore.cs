using System.Globalization;

internal sealed class HistoryStore
{
    private readonly string _historyPath;

    public HistoryStore(string historyPath)
    {
        _historyPath = historyPath;
    }

    public static string GetDefaultPath()
    {
        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "historial.txt"));
    }

    public void Append(string expression, decimal result)
    {
        var line =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {expression} = {result.ToString(CultureInfo.InvariantCulture)}";

        File.AppendAllLines(_historyPath, new[] { line });
    }

    public IReadOnlyList<string> ReadRecent(int count)
    {
        if (!File.Exists(_historyPath))
        {
            return Array.Empty<string>();
        }

        return File.ReadLines(_historyPath)
            .TakeLast(count)
            .ToArray();
    }
}
