using System.Globalization;

namespace NanoCalc;

internal readonly record struct CalcValue(CalcValueKind Kind, decimal Number, string Text) {
    public static readonly CalcValue Empty = new(CalcValueKind.Empty, 0m, string.Empty);

    public static CalcValue FromNumber(decimal number) => new(CalcValueKind.Number, number, string.Empty);

    public static CalcValue FromText(string text) => new(CalcValueKind.Text, 0m, text);

    public static CalcValue Error(string message) => new(CalcValueKind.Error, 0m, message);

    public bool IsError => Kind == CalcValueKind.Error;

    public bool IsTruthy() {
        return ToNumber() != 0m;
    }

    public decimal ToNumber() {
        if (Kind == CalcValueKind.Number) {
            return Number;
        }

        if (Kind == CalcValueKind.Text) {
            if (decimal.TryParse(Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)) {
                return invariant;
            }

            if (decimal.TryParse(Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var current)) {
                return current;
            }
        }

        return 0m;
    }

    public string ToText() {
        return Kind switch {
            CalcValueKind.Number => Number.ToString(CultureInfo.InvariantCulture),
            CalcValueKind.Text => Text,
            CalcValueKind.Error => Text,
            _ => string.Empty
        };
    }
}

internal enum CalcValueKind {
    Empty,
    Number,
    Text,
    Error
}
