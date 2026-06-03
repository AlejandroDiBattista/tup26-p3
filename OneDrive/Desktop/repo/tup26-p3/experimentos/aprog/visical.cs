using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class VisiCalcError : Exception {
    public VisiCalcError(string message) : base(message) { }
}

public class Spreadsheet {
    private readonly int _rows;
    private readonly int _cols;
    private readonly string?[,] _cells;

    public int Rows => _rows;
    public int Cols => _cols;

    public Spreadsheet(int rows = 12, int cols = 8) {
        _rows = rows;
        _cols = cols;
        _cells = new string?[rows, cols];
    }

    public void SetCell(string cellName, string content) {
        (int row, int col) = ParseCellName(cellName);
        _cells[row, col] = content.Trim();
    }

    public string? GetRawCell(string cellName) {
        (int row, int col) = ParseCellName(cellName);
        return _cells[row, col];
    }

    public string? GetRawCell(int row, int col) {
        ValidatePosition(row, col);
        return _cells[row, col];
    }

    public string GetCellName(int row, int col) {
        ValidatePosition(row, col);
        return $"{ColumnName(col)}{row + 1}";
    }

    public double EvaluateCell(string cellName) {
        return EvaluateCell(cellName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    internal double EvaluateCell(string cellName, HashSet<string> visiting) {
        string normalized = cellName.ToUpperInvariant();
        if (visiting.Contains(normalized)) {
            throw new VisiCalcError($"Referencia circular en {normalized}");
        }

        visiting.Add(normalized);
        string? raw = GetRawCell(normalized);
        double result = EvaluateContent(raw, visiting);
        visiting.Remove(normalized);
        return result;
    }

    public string GetDisplayValue(int row, int col) {
        string cellName = GetCellName(row, col);
        try {
            double value = EvaluateCell(cellName);
            return value % 1 == 0
                ? ((int)value).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
        catch {
            return "#ERR";
        }
    }

    private double EvaluateContent(string? raw, HashSet<string> visiting) {
        if (string.IsNullOrWhiteSpace(raw)) {
            return 0;
        }

        string text = raw.Trim();

        if (text.StartsWith("=")) {
            var parser = new FormulaParser(text[1..], this, visiting);
            return parser.Parse();
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            return number;

        throw new VisiCalcError($"Valor inválido: {text}");
    }

    private (int row, int col) ParseCellName(string cellName) {
        if (string.IsNullOrWhiteSpace(cellName)) {
            throw new VisiCalcError("Celda inválida");
        }

        string text = cellName.Trim().ToUpperInvariant();
        int i = 0;
        while (i < text.Length && char.IsLetter(text[i])) {
            i++;
        }

        if (i == 0 || i == text.Length) {
            throw new VisiCalcError($"Celda inválida: {cellName}");
        }

        string colPart = text[..i];
        string rowPart = text[i..];

        if (!int.TryParse(rowPart, out int rowNumber)) {
            throw new VisiCalcError($"Celda inválida: {cellName}");
        }

        int col = ColumnIndex(colPart);
        int row = rowNumber - 1;
        ValidatePosition(row, col);
        return (row, col);
    }

    private void ValidatePosition(int row, int col) {
        if (row < 0 || row >= _rows || col < 0 || col >= _cols) {
            throw new VisiCalcError("Celda fuera de rango");
        }
    }

    private static int ColumnIndex(string name) {
        int value = 0;
        foreach (char ch in name) {
            if (!char.IsLetter(ch)) {
                throw new VisiCalcError($"Columna inválida: {name}");
            }
            value = value * 26 + (ch - 'A' + 1);
        }
        return value - 1;
    }

    public static string ColumnName(int index) {
        index++;
        var sb = new StringBuilder();
        while (index > 0) {
            int rem = (index - 1) % 26;
            sb.Insert(0, (char)('A' + rem));
            index = (index - 1) / 26;
        }
        return sb.ToString();
    }
}

public class FormulaParser {
    private readonly string _text;
    private readonly Spreadsheet _sheet;
    private readonly HashSet<string> _visiting;
    private int _pos;

    public FormulaParser(string text, Spreadsheet sheet, HashSet<string> visiting) {
        _text = text;
        _sheet = sheet;
        _visiting = visiting;
        _pos = 0;
    }

    public double Parse() {
        double value = Expr();
        SkipWhitespace();
        if (_pos < _text.Length) {
            throw new VisiCalcError($"Token inesperado: {_text[_pos]}");
        }
        return value;
    }

    private double Expr() {
        double value = Term();
        while (true) {
            SkipWhitespace();
            if (Match('+')) {
                value += Term();
            }
            else if (Match('-')) {
                value -= Term();
            }
            else {
                break;
            }
        }
        return value;
    }

    private double Term() {
        double value = Factor();
        while (true) {
            SkipWhitespace();
            if (Match('*')) {
                value *= Factor();
            }
            else if (Match('/')) {
                double divisor = Factor();
                if (divisor == 0) {
                    throw new VisiCalcError("División por cero");
                }
                value /= divisor;
            }
            else {
                break;
            }
        }
        return value;
    }

    private double Factor() {
        SkipWhitespace();

        if (Match('+')) {
            return Factor();
        }
        if (Match('-')) {
            return -Factor();
        }
        if (Match('(')) {
            double value = Expr();
            SkipWhitespace();
            if (!Match(')')) {
                throw new VisiCalcError("Falta cerrar paréntesis");
            }
            return value;
        }

        if (char.IsLetter(Current())) {
            string cell = ParseCellReference();
            return _sheet.EvaluateCell(cell, _visiting);
        }

        return ParseNumber();
    }

    private string ParseCellReference() {
        int start = _pos;
        while (_pos < _text.Length && char.IsLetter(_text[_pos])) {
            _pos++;
        }
        while (_pos < _text.Length && char.IsDigit(_text[_pos])) {
            _pos++;
        }

        string cell = _text[start.._pos].ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(cell)) {
            throw new VisiCalcError("Referencia inválida");
        }
        return cell;
    }

    private double ParseNumber() {
        SkipWhitespace();
        int start = _pos;
        bool dot = false;

        while (_pos < _text.Length) {
            char ch = _text[_pos];
            if (char.IsDigit(ch)) {
                _pos++;
            }
            else if (ch == '.' && !dot) {
                dot = true;
                _pos++;
            }
            else {
                break;
            }
        }

        if (start == _pos) {
            throw new VisiCalcError("Se esperaba número o celda");
        }

        string text = _text[start.._pos];
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) {
            throw new VisiCalcError($"Número inválido: {text}");
        }

        return value;
    }

    private void SkipWhitespace() {
        while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) {
            _pos++;
        }
    }

    private bool Match(char expected) {
        SkipWhitespace();
        if (_pos < _text.Length && _text[_pos] == expected) {
            _pos++;
            return true;
        }
        return false;
    }

    private char Current() {
        return _pos < _text.Length ? _text[_pos] : '\0';
    }
}

public class Program {
    private static void Draw(Spreadsheet sheet, int currentRow, int currentCol, string message) {
        Console.Clear();
        Console.WriteLine("VisiCalc TUI  | Flechas: mover | Enter/E: editar | Q/Esc: salir");
        Console.WriteLine();

        const int width = 10;
        Console.Write("".PadRight(6));
        for (int col = 0; col < sheet.Cols; col++) {
            string header = Spreadsheet.ColumnName(col);
            Console.Write(header.PadRight(width));
        }
        Console.WriteLine();

        for (int row = 0; row < sheet.Rows; row++) {
            Console.Write((row + 1).ToString().PadRight(6));
            for (int col = 0; col < sheet.Cols; col++) {
                bool selected = row == currentRow && col == currentCol;
                string text = sheet.GetDisplayValue(row, col);
                if (text.Length > width - 2)
                    text = text[..(width - 2)];
                text = text.PadRight(width - 1);

                var oldBg = Console.BackgroundColor;
                var oldFg = Console.ForegroundColor;

                if (selected) {
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                Console.Write(" " + text);
                Console.BackgroundColor = oldBg;
                Console.ForegroundColor = oldFg;
            }
            Console.WriteLine();
        }

        string cellName = sheet.GetCellName(currentRow, currentCol);
        string raw = sheet.GetRawCell(currentRow, currentCol) ?? "";
        string evaluated = sheet.GetDisplayValue(currentRow, currentCol);

        Console.WriteLine();
        Console.WriteLine($"Celda: {cellName}");
        Console.WriteLine($"Contenido: {raw}");
        Console.WriteLine($"Valor: {evaluated}");
        Console.WriteLine(message);
    }

    private static string Prompt(string label, string initial) {
        Console.Write(label + initial);
        var buffer = new StringBuilder(initial);

        while (true) {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) {
                Console.WriteLine();
                return buffer.ToString();
            }

            if (key.Key == ConsoleKey.Escape) {
                Console.WriteLine();
                return initial;
            }

            if (key.Key == ConsoleKey.Backspace) {
                if (buffer.Length > 0) {
                    buffer.Length--;
                    Console.Write("\b \b");
                }
                continue;
            }

            if (!char.IsControl(key.KeyChar)) {
                buffer.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }
        }
    }

    public static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        var sheet = new Spreadsheet();
        int row = 0;
        int col = 0;
        string message = "";

        try {
            while (true) {
                Draw(sheet, row, col, message);
                message = "";
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.LeftArrow && col > 0) {
                    col--;
                }
                else if (key.Key == ConsoleKey.RightArrow && col < sheet.Cols - 1) {
                    col++;
                }
                else if (key.Key == ConsoleKey.UpArrow && row > 0) {
                    row--;
                }
                else if (key.Key == ConsoleKey.DownArrow && row < sheet.Rows - 1) {
                    row++;
                }
                else if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape) {
                    break;
                }
                else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.E) {
                    Console.CursorVisible = true;
                    string cellName = sheet.GetCellName(row, col);
                    string current = sheet.GetRawCell(row, col) ?? "";
                    Console.SetCursorPosition(0, sheet.Rows + 8);
                    string value = Prompt($"Editar {cellName}: ", current);
                    try {
                        sheet.SetCell(cellName, value);
                        message = "OK";
                    }
                    catch (VisiCalcError ex) {
                        message = $"Error: {ex.Message}";
                    }
                    finally {
                        Console.CursorVisible = false;
                    }
                }
            }
        }
        finally {
            Console.CursorVisible = true;
            Console.Clear();
        }
    }
}
