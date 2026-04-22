namespace VisiCalc;

internal static class SelfTest {
    public static int Run() {
        Spreadsheet sheet = new(6, 6);

        sheet.SetRaw(CellAddress.Parse("A1"), "10");
        sheet.SetRaw(CellAddress.Parse("A2"), "15");
        sheet.SetRaw(CellAddress.Parse("B1"), "=A1+A2*2");
        sheet.SetRaw(CellAddress.Parse("B2"), "=SUM(A1:A2)");
        sheet.SetRaw(CellAddress.Parse("C1"), "=AVG(A1:B2)");
        sheet.SetRaw(CellAddress.Parse("D1"), "=D2");
        sheet.SetRaw(CellAddress.Parse("D2"), "=D1");
        sheet.SetRaw(CellAddress.Parse("E1"), "hola");
        sheet.SetRaw(CellAddress.Parse("F1"), "=E1+1");

        ExpectNumber(sheet.Evaluate(CellAddress.Parse("B1")), 40d, "B1");
        ExpectNumber(sheet.Evaluate(CellAddress.Parse("B2")), 25d, "B2");
        ExpectNumber(sheet.Evaluate(CellAddress.Parse("C1")), 22.5d, "C1");
        ExpectError(sheet.Evaluate(CellAddress.Parse("D1")), "D1");
        ExpectError(sheet.Evaluate(CellAddress.Parse("F1")), "F1");

        string text = sheet.ToText();
        Spreadsheet reloaded = new();
        reloaded.LoadText(text);

        ExpectRaw(reloaded, "B2", "=SUM(A1:A2)");
        ExpectNumber(reloaded.Evaluate(CellAddress.Parse("B2")), 25d, "TXT B2");

        Console.WriteLine("Self-test OK");
        return 0;
    }

    private static void ExpectRaw(Spreadsheet sheet, string addressText, string expected) {
        string actual = sheet.GetRaw(CellAddress.Parse(addressText));
        if (!string.Equals(actual, expected, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"Se esperaba '{expected}' en {addressText}, pero fue '{actual}'.");
        }
    }

    private static void ExpectNumber(CellValue value, double expected, string label) {
        if (value.Kind != CellValueKind.Number || Math.Abs(value.Number - expected) > 0.0001d) {
            throw new InvalidOperationException($"Fallo en {label}: esperado {expected}, recibido {value.Kind} {value.Number}.");
        }
    }

    private static void ExpectError(CellValue value, string label) {
        if (value.Kind != CellValueKind.Error) {
            throw new InvalidOperationException($"Fallo en {label}: se esperaba error y llego {value.Kind}.");
        }
    }
}
