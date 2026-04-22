using System.Text;

namespace EditCsv;

internal sealed class CsvEditorApp
{
    private const int RowNumberWidth = 6;
    private const int CellWidth = 18;
    private const int TopLines = 3;
    private const int FooterLines = 2;

    private readonly CsvDocument _document;
    private readonly List<NumericDisplayMode> _numericDisplayModes;
    private int _selectedRow;
    private int _selectedColumn;
    private int _rowOffset;
    private int _columnOffset;
    private string _statusMessage = "Archivo cargado.";

    public CsvEditorApp(CsvDocument document)
    {
        _document = document;
        _numericDisplayModes = Enumerable
            .Repeat(NumericDisplayMode.Integer, _document.Headers.Count)
            .ToList();
    }

    public void Run()
    {
        var previousTreatControlCAsInput = Console.TreatControlCAsInput;

        Console.TreatControlCAsInput = true;
        SetCursorVisibility(false);

        try
        {
            while (true)
            {
                EnsureSelectionIsVisible();
                Render();

                var key = Console.ReadKey(intercept: true);
                if (HandleKey(key))
                {
                    break;
                }
            }
        }
        finally
        {
            Console.ResetColor();
            SetCursorVisibility(true);
            Console.TreatControlCAsInput = previousTreatControlCAsInput;
            Console.Clear();
        }
    }

    private bool HandleKey(ConsoleKeyInfo key)
    {
        if (key.KeyChar == '.')
        {
            ToggleNumericDisplayMode();
            return false;
        }

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                _selectedColumn = Math.Max(0, _selectedColumn - 1);
                return false;

            case ConsoleKey.RightArrow:
                _selectedColumn = Math.Min(_document.Headers.Count - 1, _selectedColumn + 1);
                return false;

            case ConsoleKey.UpArrow:
                if (_document.Rows.Count > 0)
                {
                    _selectedRow = Math.Max(0, _selectedRow - 1);
                }

                return false;

            case ConsoleKey.DownArrow:
                if (_document.Rows.Count > 0)
                {
                    _selectedRow = Math.Min(_document.Rows.Count - 1, _selectedRow + 1);
                }

                return false;

            case ConsoleKey.PageUp:
                if (_document.Rows.Count > 0)
                {
                    _selectedRow = Math.Max(0, _selectedRow - VisibleDataRowCount());
                }

                return false;

            case ConsoleKey.PageDown:
                if (_document.Rows.Count > 0)
                {
                    _selectedRow = Math.Min(_document.Rows.Count - 1, _selectedRow + VisibleDataRowCount());
                }

                return false;

            case ConsoleKey.Home:
                _selectedColumn = 0;
                if (_document.Rows.Count > 0)
                {
                    _selectedRow = 0;
                }

                return false;

            case ConsoleKey.End:
                _selectedColumn = _document.Headers.Count - 1;
                if (_document.Rows.Count > 0)
                {
                    _selectedRow = _document.Rows.Count - 1;
                }

                return false;

            case ConsoleKey.Tab:
                if ((key.Modifiers & ConsoleModifiers.Shift) != 0)
                {
                    _selectedColumn = Math.Max(0, _selectedColumn - 1);
                }
                else
                {
                    _selectedColumn = Math.Min(_document.Headers.Count - 1, _selectedColumn + 1);
                }

                return false;

            case ConsoleKey.Enter:
            case ConsoleKey.E:
                EditCurrentCell();
                return false;

            case ConsoleKey.A:
                AddRow();
                return false;

            case ConsoleKey.D:
                DeleteCurrentRow();
                return false;

            case ConsoleKey.O:
                SortByCurrentColumn();
                return false;

            case ConsoleKey.R:
                RenameCurrentColumn();
                return false;

            case ConsoleKey.L:
                ChangeDelimiter();
                return false;

            case ConsoleKey.H:
                AddColumn();
                return false;

            case ConsoleKey.X:
                DeleteCurrentColumn();
                return false;

            case ConsoleKey.S:
                SaveDocument();
                return false;

            case ConsoleKey.Q:
            case ConsoleKey.Escape:
                return ConfirmExit();

            default:
                return false;
        }
    }

    private void EditCurrentCell()
    {
        EnsureAtLeastOneRow();
        var currentValue = _document.GetCell(_selectedRow, _selectedColumn);
        var header = _document.Headers[_selectedColumn];
        var updatedValue = PromptInput($"Editar [{header}] fila {_selectedRow + 1}: ", currentValue);

        if (updatedValue is null)
        {
            SetStatus("Edicion cancelada.");
            return;
        }

        _document.SetCell(_selectedRow, _selectedColumn, updatedValue);
        SetStatus("Celda actualizada.");
    }

    private void AddRow()
    {
        _document.InsertRowAfter(_document.Rows.Count == 0 ? -1 : _selectedRow);
        _selectedRow = Math.Min(_selectedRow + 1, _document.Rows.Count - 1);
        SetStatus("Fila agregada.");
    }

    private void DeleteCurrentRow()
    {
        if (_document.Rows.Count == 0)
        {
            SetStatus("No hay filas para borrar.");
            return;
        }

        if (!PromptConfirmation($"Borrar fila {_selectedRow + 1}?"))
        {
            SetStatus("Borrado cancelado.");
            return;
        }

        _document.DeleteRow(_selectedRow);

        if (_document.Rows.Count == 0)
        {
            _selectedRow = 0;
        }
        else
        {
            _selectedRow = Math.Min(_selectedRow, _document.Rows.Count - 1);
        }

        SetStatus("Fila borrada.");
    }

    private void SortByCurrentColumn()
    {
        var header = _document.Headers[_selectedColumn];
        var result = _document.SortByColumn(_selectedColumn);

        switch (result)
        {
            case CsvDocument.SortResult.Ascending:
                _selectedRow = 0;
                _rowOffset = 0;
                SetStatus($"Columna '{header}' ordenada ascendente.");
                break;

            case CsvDocument.SortResult.Descending:
                _selectedRow = 0;
                _rowOffset = 0;
                SetStatus($"Columna '{header}' ordenada descendente.");
                break;

            default:
                SetStatus("No hay suficientes filas para ordenar.");
                break;
        }
    }

    private void AddColumn()
    {
        var columnName = PromptInput("Nombre de la nueva columna: ", $"Columna {_selectedColumn + 2}");

        if (columnName is null)
        {
            SetStatus("Alta de columna cancelada.");
            return;
        }

        _document.InsertColumnAfter(_selectedColumn, columnName);
        _numericDisplayModes.Insert(Math.Clamp(_selectedColumn + 1, 0, _numericDisplayModes.Count), NumericDisplayMode.Integer);
        _selectedColumn = Math.Min(_selectedColumn + 1, _document.Headers.Count - 1);
        SetStatus("Columna agregada.");
    }

    private void RenameCurrentColumn()
    {
        if (!_document.HasHeader)
        {
            SetStatus("Renombrar columnas requiere abrir el CSV con encabezados.");
            return;
        }

        var currentHeader = _document.Headers[_selectedColumn];
        var columnName = PromptInput("Nuevo nombre de la columna: ", currentHeader);

        if (columnName is null)
        {
            SetStatus("Renombrado cancelado.");
            return;
        }

        _document.RenameColumn(_selectedColumn, columnName);
        SetStatus("Columna renombrada.");
    }

    private void ChangeDelimiter()
    {
        var input = PromptInput("Nuevo delimitador (, ; | \\t): ", FormatDelimiter(_document.Delimiter));

        if (input is null)
        {
            SetStatus("Cambio de delimitador cancelado.");
            return;
        }

        if (!CommandLineOptions.TryParseDelimiter(input, out var delimiter))
        {
            SetStatus("Delimitador invalido. Use un caracter o \\t.");
            return;
        }

        _document.SetDelimiter(delimiter);
        SetStatus($"Delimitador cambiado a {FormatDelimiter(delimiter)}.");
    }

    private void DeleteCurrentColumn()
    {
        if (_document.Headers.Count <= 1)
        {
            SetStatus("Debe quedar al menos una columna.");
            return;
        }

        var header = _document.Headers[_selectedColumn];
        if (!PromptConfirmation($"Borrar columna '{header}'?"))
        {
            SetStatus("Borrado de columna cancelado.");
            return;
        }

        if (_selectedColumn >= 0 && _selectedColumn < _numericDisplayModes.Count)
        {
            _numericDisplayModes.RemoveAt(_selectedColumn);
        }

        _document.DeleteColumn(_selectedColumn);
        _selectedColumn = Math.Min(_selectedColumn, _document.Headers.Count - 1);
        SetStatus("Columna borrada.");
    }

    private void SaveDocument()
    {
        _document.Save();
        SetStatus($"Archivo guardado en {_document.FilePath}.");
    }

    private void ToggleNumericDisplayMode()
    {
        if (!_document.IsNumericColumn(_selectedColumn))
        {
            SetStatus("La columna actual no es numerica.");
            return;
        }

        _numericDisplayModes[_selectedColumn] = _numericDisplayModes[_selectedColumn] switch
        {
            NumericDisplayMode.Integer => NumericDisplayMode.TwoDecimals,
            _ => NumericDisplayMode.Integer
        };

        SetStatus(
            _numericDisplayModes[_selectedColumn] == NumericDisplayMode.Integer
                ? $"Columna '{_document.Headers[_selectedColumn]}' en modo enteros."
                : $"Columna '{_document.Headers[_selectedColumn]}' en modo 2 decimales.");
    }

    private bool ConfirmExit()
    {
        if (!_document.IsDirty)
        {
            return true;
        }

        if (PromptConfirmation("Hay cambios sin guardar. Salir igual?"))
        {
            return true;
        }

        SetStatus("Salida cancelada.");
        return false;
    }

    private void Render()
    {
        Console.Clear();

        WriteLine(0, $"editcsv | {_document.FilePath}");
        WriteLine(1, "Flechas | Enter/E | A fila | D fila | O ordenar | R col | H+ | X- | L delim | . num | S | Q");
        WriteLine(2, $"Columnas: {_document.Headers.Count} | Filas: {_document.Rows.Count} | Delimitador: {FormatDelimiter(_document.Delimiter)} | Modo col actual: {FormatNumericMode(_selectedColumn)} | Cambios: {(_document.IsDirty ? "si" : "no")}");

        RenderGrid();
        RenderFooter();
    }

    private void RenderGrid()
    {
        var visibleColumns = VisibleColumnCount();
        var visibleRows = VisibleDataRowCount();
        var top = TopLines;

        WriteGridCell(0, top, "Fila", RowNumberWidth, false, true);

        var x = RowNumberWidth;
        for (var columnIndex = _columnOffset; columnIndex < _document.Headers.Count && columnIndex < _columnOffset + visibleColumns; columnIndex++)
        {
            WriteSeparator(x, top);
            x++;
            WriteGridCell(x, top, _document.Headers[columnIndex], CellWidth, false, true);
            x += CellWidth;
        }

        for (var rowOffset = 0; rowOffset < visibleRows; rowOffset++)
        {
            var rowIndex = _rowOffset + rowOffset;
            var y = top + 1 + rowOffset;

            if (rowIndex >= _document.Rows.Count)
            {
                ClearLine(y);
                continue;
            }

            WriteGridCell(0, y, (rowIndex + 1).ToString(), RowNumberWidth, false, true, true);
            x = RowNumberWidth;

            for (var columnIndex = _columnOffset; columnIndex < _document.Headers.Count && columnIndex < _columnOffset + visibleColumns; columnIndex++)
            {
                WriteSeparator(x, y);
                x++;

                var isSelected = rowIndex == _selectedRow && columnIndex == _selectedColumn;
                var isNumericColumn = _document.IsNumericColumn(columnIndex);
                var value = GetDisplayValue(rowIndex, columnIndex, isNumericColumn);
                WriteGridCell(x, y, value, CellWidth, isSelected, false, isNumericColumn);
                x += CellWidth;
            }
        }

        if (_document.Rows.Count == 0)
        {
            WriteLine(top + 1, "[sin filas] Presione A para agregar una fila nueva.");
        }
    }

    private void RenderFooter()
    {
        var selectedHeader = _document.Headers[_selectedColumn];
        var selectedValue = _document.Rows.Count == 0 ? string.Empty : _document.GetCell(_selectedRow, _selectedColumn);
        var renderedValue = _document.Rows.Count == 0
            ? string.Empty
            : GetDisplayValue(_selectedRow, _selectedColumn, _document.IsNumericColumn(_selectedColumn));
        var infoLine = Console.WindowHeight - 2;
        var statusLine = Console.WindowHeight - 1;

        WriteLine(
            infoLine,
            $"Celda: fila {_selectedRow + 1}, columna {_selectedColumn + 1} ({selectedHeader}) | Valor: {selectedValue} | Vista: {renderedValue}");
        WriteLine(statusLine, _statusMessage);
    }

    private void EnsureSelectionIsVisible()
    {
        _selectedColumn = Math.Clamp(_selectedColumn, 0, _document.Headers.Count - 1);

        if (_document.Rows.Count == 0)
        {
            _selectedRow = 0;
            _rowOffset = 0;
            return;
        }

        _selectedRow = Math.Clamp(_selectedRow, 0, _document.Rows.Count - 1);

        var visibleRows = VisibleDataRowCount();
        if (_selectedRow < _rowOffset)
        {
            _rowOffset = _selectedRow;
        }
        else if (_selectedRow >= _rowOffset + visibleRows)
        {
            _rowOffset = _selectedRow - visibleRows + 1;
        }

        var visibleColumns = VisibleColumnCount();
        if (_selectedColumn < _columnOffset)
        {
            _columnOffset = _selectedColumn;
        }
        else if (_selectedColumn >= _columnOffset + visibleColumns)
        {
            _columnOffset = _selectedColumn - visibleColumns + 1;
        }
    }

    private void EnsureAtLeastOneRow()
    {
        if (_document.Rows.Count > 0)
        {
            return;
        }

        _document.InsertRowAfter(-1);
        _selectedRow = 0;
    }

    private int VisibleColumnCount()
    {
        var availableWidth = Math.Max(20, Console.WindowWidth - RowNumberWidth);
        return Math.Max(1, availableWidth / (CellWidth + 1));
    }

    private int VisibleDataRowCount()
    {
        var availableHeight = Math.Max(4, Console.WindowHeight - TopLines - FooterLines - 1);
        return Math.Max(1, availableHeight);
    }

    private string? PromptInput(string label, string initialValue)
    {
        var buffer = new StringBuilder(initialValue);
        var cursorIndex = buffer.Length;
        var line = Console.WindowHeight - 1;

        SetCursorVisibility(true);

        try
        {
            while (true)
            {
                DrawPrompt(label, buffer.ToString(), cursorIndex, line);
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        return buffer.ToString();

                    case ConsoleKey.Escape:
                        return null;

                    case ConsoleKey.LeftArrow:
                        cursorIndex = Math.Max(0, cursorIndex - 1);
                        break;

                    case ConsoleKey.RightArrow:
                        cursorIndex = Math.Min(buffer.Length, cursorIndex + 1);
                        break;

                    case ConsoleKey.Home:
                        cursorIndex = 0;
                        break;

                    case ConsoleKey.End:
                        cursorIndex = buffer.Length;
                        break;

                    case ConsoleKey.Backspace:
                        if (cursorIndex > 0)
                        {
                            buffer.Remove(cursorIndex - 1, 1);
                            cursorIndex--;
                        }

                        break;

                    case ConsoleKey.Delete:
                        if (cursorIndex < buffer.Length)
                        {
                            buffer.Remove(cursorIndex, 1);
                        }

                        break;

                    default:
                        if (!char.IsControl(key.KeyChar))
                        {
                            buffer.Insert(cursorIndex, key.KeyChar);
                            cursorIndex++;
                        }

                        break;
                }
            }
        }
        finally
        {
            SetCursorVisibility(false);
        }
    }

    private bool PromptConfirmation(string message)
    {
        var line = Console.WindowHeight - 1;
        WriteLine(line, $"{message} [s/N]");

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.S || key.Key == ConsoleKey.Y)
            {
                return true;
            }

            if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Escape)
            {
                return false;
            }
        }
    }

    private void DrawPrompt(string label, string value, int cursorIndex, int line)
    {
        var available = Math.Max(10, Console.WindowWidth - label.Length - 1);
        var start = 0;

        if (cursorIndex >= available)
        {
            start = cursorIndex - available + 1;
        }

        var visibleValue = value.Length <= available
            ? value
            : value.Substring(start, Math.Min(available, value.Length - start));

        WriteLine(line, label + visibleValue);

        var cursorColumn = Math.Min(label.Length + cursorIndex - start, Console.WindowWidth - 1);
        Console.SetCursorPosition(cursorColumn, line);
    }

    private void WriteLine(int y, string content)
    {
        if (y < 0 || y >= Console.WindowHeight)
        {
            return;
        }

        Console.SetCursorPosition(0, y);
        var truncated = Fit(content, Console.WindowWidth);
        Console.Write(truncated.PadRight(Console.WindowWidth));
    }

    private void ClearLine(int y)
    {
        WriteLine(y, string.Empty);
    }

    private void WriteSeparator(int x, int y)
    {
        if (x < 0 || x >= Console.WindowWidth || y < 0 || y >= Console.WindowHeight)
        {
            return;
        }

        Console.SetCursorPosition(x, y);
        Console.ResetColor();
        Console.Write("|");
    }

    private void WriteGridCell(int x, int y, string text, int width, bool selected, bool header, bool rightAlign = false)
    {
        if (x >= Console.WindowWidth || y >= Console.WindowHeight)
        {
            return;
        }

        Console.SetCursorPosition(x, y);

        if (selected)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
        }
        else if (header)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }
        else
        {
            Console.ResetColor();
        }

        var fitted = Fit(text, width);
        Console.Write(rightAlign ? fitted.PadLeft(width) : fitted.PadRight(width));
        Console.ResetColor();
    }

    private void SetStatus(string message)
    {
        _statusMessage = message;
    }

    private static string Fit(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var sanitized = text
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ');

        if (sanitized.Length <= width)
        {
            return sanitized;
        }

        if (width <= 3)
        {
            return sanitized[..width];
        }

        return sanitized[..(width - 3)] + "...";
    }

    private static string FormatDelimiter(char delimiter)
    {
        return delimiter switch
        {
            '\t' => "\\t",
            _ => delimiter.ToString()
        };
    }

    private string GetDisplayValue(int rowIndex, int columnIndex, bool isNumericColumn)
    {
        if (!isNumericColumn)
        {
            return _document.GetCell(rowIndex, columnIndex);
        }

        if (!_document.TryGetNumericCell(rowIndex, columnIndex, out var number))
        {
            return _document.GetCell(rowIndex, columnIndex);
        }

        return _numericDisplayModes[columnIndex] switch
        {
            NumericDisplayMode.Integer => number.ToString("0"),
            NumericDisplayMode.TwoDecimals => number.ToString("0.00"),
            _ => _document.GetCell(rowIndex, columnIndex)
        };
    }

    private string FormatNumericMode(int columnIndex)
    {
        if (!_document.IsNumericColumn(columnIndex))
        {
            return "n/a";
        }

        return _numericDisplayModes[columnIndex] switch
        {
            NumericDisplayMode.Integer => "enteros",
            NumericDisplayMode.TwoDecimals => "2 decimales",
            _ => "original"
        };
    }

    private static void SetCursorVisibility(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
        }
        catch (PlatformNotSupportedException)
        {
        }
    }

    private enum NumericDisplayMode
    {
        Integer,
        TwoDecimals
    }
}
