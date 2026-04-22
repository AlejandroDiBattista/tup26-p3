using System.Globalization;

namespace EditCsv;

internal sealed class CsvDocument
{
    public string FilePath { get; }
    public bool HasHeader { get; }
    public char Delimiter { get; private set; }
    public List<string> Headers { get; }
    public List<List<string>> Rows { get; }
    public bool IsDirty { get; private set; }

    private CsvDocument(string filePath, bool hasHeader, char delimiter, List<string> headers, List<List<string>> rows)
    {
        FilePath = filePath;
        HasHeader = hasHeader;
        Delimiter = delimiter;
        Headers = headers;
        Rows = rows;
    }

    public static CsvDocument Load(string filePath, bool hasHeader, char? forcedDelimiter)
    {
        if (!File.Exists(filePath))
        {
            return new CsvDocument(
                filePath,
                hasHeader,
                forcedDelimiter ?? ',',
                ["Columna 1", "Columna 2", "Columna 3"],
                []);
        }

        var text = File.ReadAllText(filePath);
        var delimiter = forcedDelimiter ?? CsvParser.DetectDelimiter(text);
        var parsedRows = CsvParser.Parse(text, delimiter);

        if (parsedRows.Count == 0)
        {
            return new CsvDocument(
                filePath,
                hasHeader,
                delimiter,
                ["Columna 1", "Columna 2", "Columna 3"],
                []);
        }

        List<string> headers;
        List<List<string>> dataRows;

        if (hasHeader)
        {
            headers = parsedRows[0]
                .Select((value, index) => NormalizeHeader(value, index))
                .ToList();
            dataRows = parsedRows.Skip(1).Select(row => new List<string>(row)).ToList();
        }
        else
        {
            var maxColumns = parsedRows.Max(row => row.Count);
            headers = Enumerable.Range(1, Math.Max(1, maxColumns))
                .Select(index => $"Columna {index}")
                .ToList();
            dataRows = parsedRows.Select(row => new List<string>(row)).ToList();
        }

        var widestRow = Math.Max(headers.Count, dataRows.DefaultIfEmpty([]).Max(row => row.Count));
        while (headers.Count < widestRow)
        {
            headers.Add($"Columna {headers.Count + 1}");
        }

        foreach (var row in dataRows)
        {
            NormalizeRow(row, headers.Count);
        }

        return new CsvDocument(filePath, hasHeader, delimiter, headers, dataRows);
    }

    public string GetCell(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            return string.Empty;
        }

        return Rows[rowIndex][columnIndex];
    }

    public void SetCell(int rowIndex, int columnIndex, string value)
    {
        EnsureRowExists(rowIndex);
        Rows[rowIndex][columnIndex] = value;
        IsDirty = true;
    }

    public void InsertRowAfter(int rowIndex)
    {
        var insertIndex = Math.Clamp(rowIndex + 1, 0, Rows.Count);
        Rows.Insert(insertIndex, CreateEmptyRow());
        IsDirty = true;
    }

    public void DeleteRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            return;
        }

        Rows.RemoveAt(rowIndex);
        IsDirty = true;
    }

    public void InsertColumnAfter(int columnIndex, string headerName)
    {
        var insertIndex = Math.Clamp(columnIndex + 1, 0, Headers.Count);
        Headers.Insert(insertIndex, string.IsNullOrWhiteSpace(headerName) ? $"Columna {insertIndex + 1}" : headerName.Trim());

        foreach (var row in Rows)
        {
            row.Insert(insertIndex, string.Empty);
        }

        RenormalizeGeneratedHeaders();
        IsDirty = true;
    }

    public void RenameColumn(int columnIndex, string headerName)
    {
        if (columnIndex < 0 || columnIndex >= Headers.Count)
        {
            return;
        }

        Headers[columnIndex] = string.IsNullOrWhiteSpace(headerName)
            ? $"Columna {columnIndex + 1}"
            : headerName.Trim();
        IsDirty = true;
    }

    public void SetDelimiter(char delimiter)
    {
        if (Delimiter == delimiter)
        {
            return;
        }

        Delimiter = delimiter;
        IsDirty = true;
    }

    public SortResult SortByColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= Headers.Count || Rows.Count <= 1)
        {
            return SortResult.NotChanged;
        }

        var isNumeric = IsNumericColumn(columnIndex);
        var ascending = IsSorted(columnIndex, isNumeric, ascending: true);
        var descending = !ascending && IsSorted(columnIndex, isNumeric, ascending: false);

        if (ascending)
        {
            Rows.Reverse();
            IsDirty = true;
            return SortResult.Descending;
        }

        if (descending)
        {
            Rows.Sort((left, right) => CompareRows(left, right, columnIndex, isNumeric, ascending: true));
            IsDirty = true;
            return SortResult.Ascending;
        }

        Rows.Sort((left, right) => CompareRows(left, right, columnIndex, isNumeric, ascending: true));
        IsDirty = true;
        return SortResult.Ascending;
    }

    public void DeleteColumn(int columnIndex)
    {
        if (Headers.Count <= 1 || columnIndex < 0 || columnIndex >= Headers.Count)
        {
            return;
        }

        Headers.RemoveAt(columnIndex);

        foreach (var row in Rows)
        {
            if (columnIndex < row.Count)
            {
                row.RemoveAt(columnIndex);
            }

            NormalizeRow(row, Headers.Count);
        }

        RenormalizeGeneratedHeaders();
        IsDirty = true;
    }

    public void Save()
    {
        var rowsToWrite = new List<IReadOnlyList<string>>();

        if (HasHeader)
        {
            rowsToWrite.Add(Headers);
        }

        rowsToWrite.AddRange(Rows);
        var text = CsvParser.Serialize(rowsToWrite, Delimiter);
        File.WriteAllText(FilePath, text);
        IsDirty = false;
    }

    public bool IsNumericColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= Headers.Count)
        {
            return false;
        }

        var hasNumericValue = false;

        foreach (var row in Rows)
        {
            var value = row[columnIndex].Trim();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (!TryParseNumber(value, out _))
            {
                return false;
            }

            hasNumericValue = true;
        }

        return hasNumericValue;
    }

    public bool TryGetNumericCell(int rowIndex, int columnIndex, out decimal number)
    {
        number = default;

        if (rowIndex < 0 || rowIndex >= Rows.Count || columnIndex < 0 || columnIndex >= Headers.Count)
        {
            return false;
        }

        return TryParseNumber(Rows[rowIndex][columnIndex], out number);
    }

    public enum SortResult
    {
        NotChanged,
        Ascending,
        Descending
    }

    private List<string> CreateEmptyRow()
    {
        return Enumerable.Repeat(string.Empty, Headers.Count).ToList();
    }

    private void EnsureRowExists(int rowIndex)
    {
        while (Rows.Count <= rowIndex)
        {
            Rows.Add(CreateEmptyRow());
        }
    }

    private static string NormalizeHeader(string value, int index)
    {
        return string.IsNullOrWhiteSpace(value) ? $"Columna {index + 1}" : value.Trim();
    }

    private static void NormalizeRow(List<string> row, int width)
    {
        while (row.Count < width)
        {
            row.Add(string.Empty);
        }

        while (row.Count > width)
        {
            row.RemoveAt(row.Count - 1);
        }
    }

    private void RenormalizeGeneratedHeaders()
    {
        for (var index = 0; index < Headers.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(Headers[index]))
            {
                Headers[index] = $"Columna {index + 1}";
            }
        }
    }

    private bool IsSorted(int columnIndex, bool isNumeric, bool ascending)
    {
        for (var index = 1; index < Rows.Count; index++)
        {
            if (CompareRows(Rows[index - 1], Rows[index], columnIndex, isNumeric, ascending) > 0)
            {
                return false;
            }
        }

        return true;
    }

    private int CompareRows(List<string> left, List<string> right, int columnIndex, bool isNumeric, bool ascending)
    {
        var result = CompareValues(left[columnIndex], right[columnIndex], isNumeric);
        return ascending ? result : -result;
    }

    private static int CompareValues(string left, string right, bool isNumeric)
    {
        var leftTrimmed = left.Trim();
        var rightTrimmed = right.Trim();

        var leftEmpty = string.IsNullOrEmpty(leftTrimmed);
        var rightEmpty = string.IsNullOrEmpty(rightTrimmed);

        if (leftEmpty && rightEmpty)
        {
            return 0;
        }

        if (leftEmpty)
        {
            return 1;
        }

        if (rightEmpty)
        {
            return -1;
        }

        if (isNumeric && TryParseNumber(leftTrimmed, out var leftNumber) && TryParseNumber(rightTrimmed, out var rightNumber))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        return string.Compare(leftTrimmed, rightTrimmed, StringComparison.CurrentCultureIgnoreCase);
    }

    private static bool TryParseNumber(string value, out decimal number)
    {
        var normalized = value.Trim();
        var commaDecimal = new NumberFormatInfo
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "."
        };

        var dotDecimal = new NumberFormatInfo
        {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = ","
        };

        if (normalized.Contains('.') && !normalized.Contains(','))
        {
            return
                decimal.TryParse(normalized, NumberStyles.Number, dotDecimal, out number) ||
                decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out number) ||
                decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out number);
        }

        if (normalized.Contains(',') && !normalized.Contains('.'))
        {
            return
                decimal.TryParse(normalized, NumberStyles.Number, commaDecimal, out number) ||
                decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out number) ||
                decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
        }

        return
            decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out number) ||
            decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out number) ||
            decimal.TryParse(normalized, NumberStyles.Number, commaDecimal, out number) ||
            decimal.TryParse(normalized, NumberStyles.Number, dotDecimal, out number);
    }
}
