namespace VisiCalc;

internal enum CellValueKind {
    Empty,
    Number,
    Text,
    Error
}

internal readonly record struct CellValue(CellValueKind Kind, double Number, string Text) {
    public static CellValue Empty => new(CellValueKind.Empty, 0d, string.Empty);

    public static CellValue FromNumber(double number) => new(CellValueKind.Number, number, string.Empty);

    public static CellValue FromText(string text) => new(CellValueKind.Text, 0d, text);

    public static CellValue FromError(string message) => new(CellValueKind.Error, double.NaN, message);

    public bool TryGetNumber(out double number) {
        if (Kind == CellValueKind.Number) {
            number = Number;
            return true;
        }

        if (Kind == CellValueKind.Empty) {
            number = 0d;
            return true;
        }

        number = 0d;
        return false;
    }
}

internal readonly record struct CellView(string DisplayText, bool AlignRight, bool IsError);
