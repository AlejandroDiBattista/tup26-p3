using System.Globalization;
using System.Text;

namespace VisiCalc;

internal sealed class Spreadsheet {
    private readonly Dictionary<CellAddress, string> cells = [];

    public Spreadsheet(int rowCount = 20, int columnCount = 8) {
        RowCount = Math.Max(1, rowCount);
        ColumnCount = Math.Max(1, columnCount);
    }

    public int RowCount { get; private set; }

    public int ColumnCount { get; private set; }

    public string GetRaw(CellAddress address) => cells.TryGetValue(address, out string? raw) ? raw : string.Empty;

    public void SetRaw(CellAddress address, string? raw) {
        EnsureContains(address);

        raw = raw?.Trim() ?? string.Empty;
        if (raw.Length == 0) {
            cells.Remove(address);
            return;
        }

        cells[address] = raw;
    }

    public void Clear(CellAddress address) => cells.Remove(address);

    public void ClearAll() => cells.Clear();

    public void Resize(int rows, int columns) {
        RowCount = Math.Max(1, rows);
        ColumnCount = Math.Max(1, columns);

        List<CellAddress> outsideBounds = cells.Keys
            .Where(address => address.Row >= RowCount || address.Column >= ColumnCount)
            .ToList();

        foreach (CellAddress address in outsideBounds) {
            cells.Remove(address);
        }
    }

    public CellValue Evaluate(CellAddress address) {
        Dictionary<CellAddress, CellValue> cache = [];
        HashSet<CellAddress> stack = [];
        return EvaluateInternal(address, cache, stack);
    }

    public CellView GetView(CellAddress address, bool showRawValues) {
        if (showRawValues) {
            return new CellView(GetRaw(address), AlignRight: false, IsError: false);
        }

        CellValue value = Evaluate(address);
        return value.Kind switch {
            CellValueKind.Empty  => new CellView(string.Empty, AlignRight: false, IsError: false),
            CellValueKind.Number => new CellView(FormatNumber(value.Number), AlignRight: true, IsError: false),
            CellValueKind.Text   => new CellView(value.Text, AlignRight: false, IsError: false),
            CellValueKind.Error  => new CellView("#ERR", AlignRight: false, IsError: true),
            _ => new CellView("?", AlignRight: false, IsError: true)
        };
    }

    public string Describe(CellAddress address) {
        string raw = GetRaw(address);
        CellValue value = Evaluate(address);
        string rendered = value.Kind switch {
            CellValueKind.Empty  => "(vacia)",
            CellValueKind.Number => FormatNumber(value.Number),
            CellValueKind.Text   => value.Text,
            CellValueKind.Error  => $"ERROR: {value.Text}",
            _ => "?"
        };

        return $"crudo='{raw}', valor={rendered}";
    }

    public (int Rows, int Columns) GetUsedSize() {
        if (cells.Count == 0) {
            return (1, 1);
        }

        int maxRow    = cells.Keys.Max(address => address.Row) + 1;
        int maxColumn = cells.Keys.Max(address => address.Column) + 1;
        return (Math.Max(1, maxRow), Math.Max(1, maxColumn));
    }

    public string ToText() {
        IEnumerable<string> lines = cells
            .OrderBy(entry => entry.Key.Row)
            .ThenBy(entry => entry.Key.Column)
            .Select(entry => $"{entry.Key}: {entry.Value}");

        return string.Join(Environment.NewLine, lines);
    }

    public void LoadText(string text) {
        ClearAll();
        Resize(1, 1);

        if (string.IsNullOrWhiteSpace(text)) {
            return;
        }

        string[] separators = ["\r\n", "\n", "\r"];
        string[] lines = text.Split(separators, StringSplitOptions.None);

        foreach (string line in lines) {
            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }

            int separatorIndex = line.IndexOf(':');
            if (separatorIndex < 0) {
                throw new FormatException($"Linea invalida: '{line}'.");
            }

            string addressText = line[..separatorIndex].Trim();
            string rawText = line[(separatorIndex + 1)..].TrimStart();
            SetRaw(CellAddress.Parse(addressText), rawText);
        }
    }

    public string RenderSnapshot(int maxRows = 10, int maxColumns = 8) {
        int rows    = Math.Min(RowCount, maxRows);
        int columns = Math.Min(ColumnCount, maxColumns);
        const int rowHeaderWidth = 5;
        const int cellWidth = 12;
        StringBuilder builder = new();

        builder.Append(' ', rowHeaderWidth);
        for (int column = 0; column < columns; column++) {
            builder.Append(Pad(CellAddress.FormatColumnName(column), cellWidth, alignRight: false));
            builder.Append(' ');
        }
        builder.AppendLine();

        for (int row = 0; row < rows; row++) {
            builder.Append((row + 1).ToString(CultureInfo.InvariantCulture).PadLeft(rowHeaderWidth - 1));
            builder.Append(' ');
            for (int column = 0; column < columns; column++) {
                CellView view = GetView(new CellAddress(row, column), showRawValues: false);
                builder.Append(Pad(view.DisplayText, cellWidth, view.AlignRight));
                builder.Append(' ');
            }
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private CellValue EvaluateInternal( CellAddress address, Dictionary<CellAddress, CellValue> cache, HashSet<CellAddress> stack) {
        if (cache.TryGetValue(address, out CellValue cached)) {
            return cached;
        }

        if (!stack.Add(address)) {
            return CellValue.FromError($"Referencia circular en {address}.");
        }

        try {
            string raw = GetRaw(address);
            CellValue value;

            if (string.IsNullOrWhiteSpace(raw)) {
                value = CellValue.Empty;
            } else if (raw.StartsWith('=')) {
                value = EvaluateFormula(raw[1..], cache, stack);
            } else if (TryParseNumber(raw, out double number)) {
                value = CellValue.FromNumber(number);
            } else {
                value = CellValue.FromText(raw);
            }

            cache[address] = value;
            return value;
        } finally {
            stack.Remove(address);
        }
    }

    private CellValue EvaluateFormula( string formula, Dictionary<CellAddress, CellValue> cache, HashSet<CellAddress> stack) {
        try {
            FormulaParser parser = new(
                formula,
                resolveCell:  address      => ResolveNumericCell(address, cache, stack),
                resolveRange: (start, end) => ResolveRange(start, end, cache, stack));

            double result = parser.Parse();
            if (double.IsNaN(result) || double.IsInfinity(result)) {
                return CellValue.FromError("El resultado numerico no es valido.");
            }

            return CellValue.FromNumber(result);
        } catch (FormulaException ex) {
            return CellValue.FromError(ex.Message);
        }
    }

    private double ResolveNumericCell( CellAddress address, Dictionary<CellAddress, CellValue> cache, HashSet<CellAddress> stack) {
        CellValue value = EvaluateInternal(address, cache, stack);
        if (value.TryGetNumber(out double number)) {
            return number;
        }

        throw new FormulaException(
            value.Kind == CellValueKind.Error
                ? value.Text
                : $"La celda {address} no contiene un valor numerico.");
    }

    private IEnumerable<double> ResolveRange( CellAddress start, CellAddress end, Dictionary<CellAddress, CellValue> cache, HashSet<CellAddress> stack) {
        int minRow = Math.Min(start.Row, end.Row);
        int maxRow = Math.Max(start.Row, end.Row);
        int minColumn = Math.Min(start.Column, end.Column);
        int maxColumn = Math.Max(start.Column, end.Column);

        for (int row = minRow; row <= maxRow; row++) {
            for (int column = minColumn; column <= maxColumn; column++) {
                yield return ResolveNumericCell(new CellAddress(row, column), cache, stack);
            }
        }
    }

    private void EnsureContains(CellAddress address) {
        if (address.Row >= RowCount || address.Column >= ColumnCount) {
            Resize(Math.Max(RowCount, address.Row + 1), Math.Max(ColumnCount, address.Column + 1));
        }
    }

    private static bool TryParseNumber(string text, out double number) {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) {
            return true;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number);
    }

    private static string FormatNumber(double number) {
        if (Math.Abs(number % 1d) < 0.0000000001d) {
            return number.ToString("0", CultureInfo.InvariantCulture);
        }

        return number.ToString("0.########", CultureInfo.InvariantCulture);
    }

    private static string Pad(string text, int width, bool alignRight) {
        text ??= string.Empty;
        if (text.Length > width) {
            text = text[..width];
        }

        return alignRight ? text.PadLeft(width) : text.PadRight(width);
    }
}
