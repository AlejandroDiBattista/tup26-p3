using System.Text;

namespace EditCsv;

internal static class CsvParser
{
    public static List<List<string>> Parse(string text, char delimiter)
    {
        var rows = new List<List<string>>();

        if (string.IsNullOrEmpty(text))
        {
            return rows;
        }

        var currentRow = new List<string>();
        var currentField = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];

            if (insideQuotes)
            {
                if (current == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        currentField.Append('"');
                        index++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(current);
                }

                continue;
            }

            if (current == '"')
            {
                insideQuotes = true;
                continue;
            }

            if (current == delimiter)
            {
                currentRow.Add(currentField.ToString());
                currentField.Clear();
                continue;
            }

            if (current == '\r' || current == '\n')
            {
                if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                currentRow.Add(currentField.ToString());
                currentField.Clear();
                rows.Add(currentRow);
                currentRow = [];
                continue;
            }

            currentField.Append(current);
        }

        if (currentField.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentField.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }

    public static string Serialize(IEnumerable<IReadOnlyList<string>> rows, char delimiter)
    {
        return string.Join(
            Environment.NewLine,
            rows.Select(row => string.Join(delimiter, row.Select(value => Escape(value, delimiter)))));
    }

    public static char DetectDelimiter(string text)
    {
        var candidates = new[] { ',', ';', '\t', '|' };
        var sample = ReadFirstLogicalLine(text);
        var scores = candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Score = CountDelimiterOutsideQuotes(sample, candidate)
            })
            .OrderByDescending(item => item.Score)
            .ToList();

        return scores[0].Score > 0 ? scores[0].Candidate : ',';
    }

    private static string Escape(string value, char delimiter)
    {
        var needsQuotes =
            value.Contains(delimiter) ||
            value.Contains('"') ||
            value.Contains('\n') ||
            value.Contains('\r');

        if (!needsQuotes)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string ReadFirstLogicalLine(string text)
    {
        var builder = new StringBuilder();
        var insideQuotes = false;

        foreach (var current in text)
        {
            if (current == '"')
            {
                insideQuotes = !insideQuotes;
            }

            if (!insideQuotes && (current == '\n' || current == '\r'))
            {
                break;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static int CountDelimiterOutsideQuotes(string text, char delimiter)
    {
        var count = 0;
        var insideQuotes = false;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];

            if (current == '"')
            {
                if (insideQuotes && index + 1 < text.Length && text[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                insideQuotes = !insideQuotes;
                continue;
            }

            if (!insideQuotes && current == delimiter)
            {
                count++;
            }
        }

        return count;
    }
}
