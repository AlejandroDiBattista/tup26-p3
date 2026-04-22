using System.Globalization;

namespace VisiCalc;

internal readonly record struct CellAddress(int Row, int Column) {
    public static bool TryParse(string text, out CellAddress address) {
        address = default;
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }

        text = text.Trim().ToUpperInvariant();
        int split = 0;

        while (split < text.Length && char.IsLetter(text[split])) {
            split++;
        }

        if (split == 0 || split == text.Length) {
            return false;
        }

        string columnText = text[..split];
        string rowText = text[split..];

        if (!int.TryParse(rowText, NumberStyles.None, CultureInfo.InvariantCulture, out int rowNumber) || rowNumber <= 0) {
            return false;
        }

        int columnNumber = 0;
        foreach (char c in columnText) {
            if (c is < 'A' or > 'Z') {
                return false;
            }

            columnNumber = checked(columnNumber * 26 + (c - 'A' + 1));
        }

        address = new CellAddress(rowNumber - 1, columnNumber - 1);
        return true;
    }

    public static CellAddress Parse(string text) {
        if (!TryParse(text, out CellAddress address)) {
            throw new ArgumentException($"Direccion invalida: '{text}'.", nameof(text));
        }

        return address;
    }

    public static string FormatColumnName(int column) {
        if (column < 0) {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        int value = column + 1;
        Span<char> buffer = stackalloc char[10];
        int index = buffer.Length;

        while (value > 0) {
            value--;
            buffer[--index] = (char)('A' + (value % 26));
            value /= 26;
        }

        return new string(buffer[index..]);
    }

    public override string ToString() => $"{FormatColumnName(Column)}{Row + 1}";
}
