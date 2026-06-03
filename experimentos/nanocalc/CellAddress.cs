namespace NanoCalc;

internal readonly record struct CellAddress(int Row, int Column) {
    public const int MaxRows = 1000;
    public const int MaxColumns = 26;

    public bool IsValid => Row >= 0 && Row < MaxRows && Column >= 0 && Column < MaxColumns;

    public string ToA1() {
        return $"{(char)('A' + Column)}{Row + 1}";
    }

    public CellAddress Offset(int rowOffset, int columnOffset) {
        return new CellAddress(Row + rowOffset, Column + columnOffset);
    }

    public static bool TryParse(string text, out CellAddress address) {
        address = default;
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }

        var value = text.Trim().ToUpperInvariant();
        if (value.Length < 2) {
            return false;
        }

        var columnChar = value[0];
        if (columnChar < 'A' || columnChar > 'Z') {
            return false;
        }

        if (!int.TryParse(value[1..], out var rowNumber)) {
            return false;
        }

        var row = rowNumber - 1;
        var column = columnChar - 'A';
        var parsed = new CellAddress(row, column);
        if (!parsed.IsValid) {
            return false;
        }

        address = parsed;
        return true;
    }
}
