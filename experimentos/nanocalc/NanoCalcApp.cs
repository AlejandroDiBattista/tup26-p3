using System.Globalization;
using System.Text;

namespace NanoCalc;

internal sealed class NanoCalcApp {
    private const int RowHeaderWidth = 5;
    private const int CellWidth = 11;
    private const int HeaderLines = 4;
    private const int FooterLines = 3;

    private readonly SpreadsheetDocument _document = new();
    private readonly EvaluationEngine _engine;
    private readonly Queue<ConsoleKeyInfo> _pendingKeys = new();
    private CellAddress _current = new(0, 0);
    private CellAddress? _referencePreview;
    private int _rowOffset;
    private int _columnOffset;
    private string _status = "Nanocalc listo. Use / para ver comandos.";

    public NanoCalcApp() {
        _engine = new EvaluationEngine(_document);
    }

    public void Run() {
        var previousTreatControl = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;
        SetCursorVisibility(false);

        try {
            while (true) {
                EnsureVisible();
                Render();
                var key = ReadNormalizedKey();
                if (HandleKey(key)) {
                    return;
                }
            }
        }
        finally {
            Console.ResetColor();
            SetCursorVisibility(true);
            Console.TreatControlCAsInput = previousTreatControl;
            Console.Clear();
        }
    }

    private bool HandleKey(ConsoleKeyInfo key) {
        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key is ConsoleKey.Enter or ConsoleKey.J) {
            EnterFormulaLine();
            return false;
        }

        if (CommandParser.TryGetShortcut(key, out var shortcut)) {
            return RunKeyboardShortcut(shortcut);
        }

        if (key.KeyChar == '.') {
            ToggleCurrentDisplayMode();
            return false;
        }

        if (key.KeyChar == '/') {
            return RunSlashCommand();
        }

        if (key.KeyChar == '=') {
            EnterFormulaLine("=");
            return false;
        }

        switch (key.Key) {
            case ConsoleKey.LeftArrow:
                _current = new CellAddress(_current.Row, Math.Max(0, _current.Column - 1));
                return false;

            case ConsoleKey.RightArrow:
                _current = new CellAddress(_current.Row, Math.Min(CellAddress.MaxColumns - 1, _current.Column + 1));
                return false;

            case ConsoleKey.UpArrow:
                _current = new CellAddress(Math.Max(0, _current.Row - 1), _current.Column);
                return false;

            case ConsoleKey.DownArrow:
                _current = new CellAddress(Math.Min(CellAddress.MaxRows - 1, _current.Row + 1), _current.Column);
                return false;

            case ConsoleKey.PageUp:
                _current = new CellAddress(Math.Max(0, _current.Row - VisibleRowCount()), _current.Column);
                return false;

            case ConsoleKey.PageDown:
                _current = new CellAddress(Math.Min(CellAddress.MaxRows - 1, _current.Row + VisibleRowCount()), _current.Column);
                return false;

            case ConsoleKey.Home:
                _current = new CellAddress(_current.Row, 0);
                return false;

            case ConsoleKey.End:
                _current = new CellAddress(_current.Row, CellAddress.MaxColumns - 1);
                return false;

            case ConsoleKey.Enter:
                if (_document.GetRaw(_current).StartsWith('=')) {
                    EnterFormulaLine();
                }
                else {
                    EditCurrentCell();
                }
                return false;

            case ConsoleKey.Delete:
                ClearCurrentCell();
                return false;

            default:
                if (!char.IsControl(key.KeyChar)) {
                    EditCurrentCell(key.KeyChar.ToString());
                    return false;
                }

                return false;
        }
    }

    private void EditCurrentCell(string? replaceWith = null) {
        var raw = _document.GetRaw(_current);
        var editor = new StringBuilder(replaceWith ?? raw);
        var cursor = editor.Length;
        var referenceAddress = _current;
        var referenceStart = -1;
        var referenceLength = 0;
        var pointMode = false;
        SetCursorVisibility(true);

        try {
            while (true) {
                EnsureVisible();
                Render();
                DrawCellEditor(editor.ToString(), cursor);

                var key = ReadNormalizedKey();
                if (pointMode &&
                    TryHandlePointSelection(key, ref referenceAddress, editor, ref cursor, ref referenceStart, ref referenceLength)) {
                    continue;
                }

                if (pointMode && ShouldLeavePointModeForEditing(key)) {
                    ExitPointMode(ref pointMode);
                }

                switch (key.Key) {
                    case ConsoleKey.Escape:
                        ExitPointMode(ref pointMode);
                        SetStatus("Edicion cancelada.");
                        return;

                    case ConsoleKey.Enter:
                        ExitPointMode(ref pointMode);
                        if (TryCommitCellEdit(editor.ToString())) {
                            MoveCurrent(1, 0);
                            return;
                        }
                        break;

                    case ConsoleKey.Tab:
                        if (SupportsPointMode(editor)) {
                            SetPointMode(ref pointMode, !pointMode, referenceAddress);
                            continue;
                        }

                        ExitPointMode(ref pointMode);
                        if (TryCommitCellEdit(editor.ToString())) {
                            MoveCurrent(0, 1);
                            return;
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        ExitPointMode(ref pointMode);
                        if (TryCommitCellEdit(editor.ToString())) {
                            MoveCurrent(0, -1);
                            return;
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        ExitPointMode(ref pointMode);
                        if (TryCommitCellEdit(editor.ToString())) {
                            MoveCurrent(0, 1);
                            return;
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        ExitPointMode(ref pointMode);
                        if (TryCommitCellEdit(editor.ToString())) {
                            MoveCurrent(-1, 0);
                            return;
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        ExitPointMode(ref pointMode);
                        if (TryCommitCellEdit(editor.ToString())) {
                            MoveCurrent(1, 0);
                            return;
                        }
                        break;

                    case ConsoleKey.Home:
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        cursor = 0;
                        break;

                    case ConsoleKey.End:
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        cursor = editor.Length;
                        break;

                    case ConsoleKey.Backspace:
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        _referencePreview = null;
                        if (cursor > 0) {
                            editor.Remove(cursor - 1, 1);
                            cursor--;
                        }
                        break;

                    case ConsoleKey.Delete:
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        _referencePreview = null;
                        if (cursor < editor.Length) {
                            editor.Remove(cursor, 1);
                        }
                        break;

                    default:
                        if (!char.IsControl(key.KeyChar)) {
                            ResetReferenceTracking(ref referenceStart, ref referenceLength);
                            _referencePreview = null;
                            editor.Insert(cursor, key.KeyChar);
                            cursor++;
                        }
                        break;
                }
            }
        }
        finally {
            _referencePreview = null;
            SetCursorVisibility(false);
        }
    }

    private void EnterFormulaLine(string? initialOverride = null) {
        var targetAddress = _current;
        var initial = initialOverride ?? _document.GetRaw(targetAddress);
        if (!initial.StartsWith('=')) {
            initial = "=";
        }

        while (true) {
            var formula = PromptFormulaInput($"Formula {targetAddress.ToA1()}", initial);
            if (formula is null) {
                SetStatus("Ingreso de formula cancelado.");
                return;
            }

            if (!formula.StartsWith('=')) {
                formula = "=" + formula;
            }

            if (TryCommitCellEdit(targetAddress, formula)) {
                SetStatus($"Formula cargada en {targetAddress.ToA1()}.");
                return;
            }

            initial = formula;
        }
    }

    private bool RunSlashCommand(string initialCommand = "/") {
        SetStatus(CommandParser.HelpSummary());
        var commandText = PromptInput("Comando: ", initialCommand, CommandParser.TryComplete);
        if (commandText is null) {
            SetStatus("Comando cancelado.");
            return false;
        }

        try {
            var command = CommandParser.Parse(commandText, _current, _document, _engine);
            return ExecuteCommand(command);
        }
        catch (Exception ex) {
            SetStatus(ex.Message);
            return false;
        }
    }

    private bool RunKeyboardShortcut(KeyboardShortcut shortcut) {
        if (!shortcut.ExecuteDirectly) {
            return RunSlashCommand(shortcut.CommandLine);
        }

        try {
            var command = CommandParser.Parse(shortcut.CommandLine, _current, _document, _engine);
            return ExecuteCommand(command);
        }
        catch (Exception ex) {
            SetStatus(ex.Message);
            return false;
        }
    }

    private bool ExecuteCommand(SlashCommand command) {
        switch (command) {
            case HelpCommand:
                SetStatus(CommandParser.HelpSummary());
                return false;

            case ReadCommand read:
                _document.Load(read.FileName);
                SetStatus($"Archivo '{read.FileName}' cargado.");
                return false;

            case SaveCommand save:
                _document.Save(save.FileName);
                SetStatus($"Archivo '{save.FileName}' guardado.");
                return false;

            case InsertRowCommand insertRow:
                _document.InsertRow(insertRow.RowIndex);
                _current = new CellAddress(insertRow.RowIndex, _current.Column);
                SetStatus($"Fila {insertRow.RowIndex + 1} insertada.");
                return false;

            case DeleteRowCommand deleteRow:
                _document.DeleteRow(deleteRow.RowIndex);
                _current = new CellAddress(Math.Min(deleteRow.RowIndex, CellAddress.MaxRows - 1), _current.Column);
                SetStatus($"Fila {deleteRow.RowIndex + 1} eliminada.");
                return false;

            case InsertColumnCommand insertColumn:
                _document.InsertColumn(insertColumn.ColumnIndex);
                _current = new CellAddress(_current.Row, insertColumn.ColumnIndex);
                SetStatus($"Columna {(char)('A' + insertColumn.ColumnIndex)} insertada.");
                return false;

            case DeleteColumnCommand deleteColumn:
                _document.DeleteColumn(deleteColumn.ColumnIndex);
                _current = new CellAddress(_current.Row, Math.Min(deleteColumn.ColumnIndex, CellAddress.MaxColumns - 1));
                SetStatus($"Columna {(char)('A' + deleteColumn.ColumnIndex)} eliminada.");
                return false;

            case SortCommand sort:
                var outcome = _document.SortByColumns(sort.Columns, _engine);
                SetStatus(outcome switch {
                    SortOutcome.Ascending => "Tabla ordenada ascendente.",
                    SortOutcome.Descending => "Tabla ordenada descendente.",
                    _ => "No hay suficientes filas para ordenar."
                });
                return false;

            case ExitCommand:
                SetStatus("Saliendo.");
                return true;

            default:
                return false;
        }
    }

    private void ToggleCurrentDisplayMode() {
        var cell = _document.GetCell(_current);
        var value = _document.GetDisplayValue(_current, _engine);

        if (value.IsError || value.Kind == CalcValueKind.Empty) {
            SetStatus("La celda actual no tiene un valor formateable.");
            return;
        }

        var nextFormat = value.Kind switch {
            CalcValueKind.Number => NextNumericFormat(cell.DisplayFormat),
            _ => NextTextFormat(cell.DisplayFormat)
        };

        cell.SetDisplayFormat(nextFormat);
        SetStatus($"Celda {_current.ToA1()} en modo {DescribeFormat(nextFormat)}.");
    }

    private void Render() {
        Console.Clear();

        WriteLine(0, $"nanocalc | archivo: {_document.CurrentFileName ?? "(sin guardar)"}");
        WriteLine(1, "Flechas mover | Enter editar | = formula | / comandos | . formato");
        WriteLine(2, $"Celda actual: {_current.ToA1()} | Raw: {_document.GetRaw(_current)}");
        WriteLine(3, $"Valor: {FormatDisplayValue(_current)} | Modo celda: {FormatMode(_current)}");

        RenderGrid();
        WriteLine(Console.WindowHeight - 2, _status);
        WriteLine(Console.WindowHeight - 1, CommandParser.FooterSummary());
    }

    private void RenderGrid() {
        var top = HeaderLines;
        var visibleColumns = VisibleColumnCount();
        var visibleRows = VisibleRowCount();

        WriteGridCell(0, top, string.Empty, RowHeaderWidth, false, true, GridHighlightKind.None, true);

        var x = RowHeaderWidth;
        for (var column = _columnOffset; column < Math.Min(CellAddress.MaxColumns, _columnOffset + visibleColumns); column++) {
            WriteSeparator(x, top);
            x++;
            WriteGridCell(
                x,
                top,
                ((char)('A' + column)).ToString(),
                CellWidth,
                false,
                true,
                GetHeaderHighlight(column == _current.Column, _referencePreview?.Column == column),
                true);
            x += CellWidth;
        }

        for (var rowOffset = 0; rowOffset < visibleRows; rowOffset++) {
            var row = _rowOffset + rowOffset;
            var y = top + 1 + rowOffset;
            if (row >= CellAddress.MaxRows) {
                ClearLine(y);
                continue;
            }

            WriteGridCell(
                0,
                y,
                (row + 1).ToString(),
                RowHeaderWidth,
                false,
                true,
                GetHeaderHighlight(row == _current.Row, _referencePreview?.Row == row),
                true);
            x = RowHeaderWidth;

            for (var column = _columnOffset; column < Math.Min(CellAddress.MaxColumns, _columnOffset + visibleColumns); column++) {
                WriteSeparator(x, y);
                x++;
                var address = new CellAddress(row, column);
                var selectedAddress = _referencePreview ?? _current;
                var isSelected = address == selectedAddress;
                var value = _document.GetDisplayValue(address, _engine);
                var isNumeric = value.Kind == CalcValueKind.Number;
                var highlight = _referencePreview == address ? GridHighlightKind.ReferenceCell : GridHighlightKind.None;
                WriteGridCell(x, y, FormatDisplayValue(address), CellWidth, isSelected, false, highlight, isNumeric);
                x += CellWidth;
            }
        }
    }

    private string FormatDisplayValue(CellAddress address) {
        var cell = _document.GetCell(address);
        var value = _document.GetDisplayValue(address, _engine);

        if (value.IsError) {
            return value.Text;
        }

        if (value.Kind == CalcValueKind.Number) {
            return ResolveNumericFormat(cell.DisplayFormat) switch {
                CellDisplayFormat.NumberTwoDecimals => value.Number.ToString("N2", CultureInfo.CurrentCulture),
                CellDisplayFormat.NumberPercentage => (value.Number * 100m).ToString("N2", CultureInfo.CurrentCulture) + "%",
                CellDisplayFormat.NumberCurrency => "$" + value.Number.ToString("N2", CultureInfo.CurrentCulture),
                _ => value.Number.ToString("0.#############################", CultureInfo.CurrentCulture)
            };
        }

        var text = value.ToText();
        return ResolveTextFormat(cell.DisplayFormat) switch {
            CellDisplayFormat.TextUpper => text.ToUpper(CultureInfo.CurrentCulture),
            CellDisplayFormat.TextLower => text.ToLower(CultureInfo.CurrentCulture),
            CellDisplayFormat.TextTitle => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower(CultureInfo.CurrentCulture)),
            _ => text
        };
    }

    private void DrawCellEditor(string text, int cursor) {
        if (!TryGetVisibleCellPosition(_current, out var x, out var y)) {
            return;
        }

        var normalized = text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        var start = Math.Max(0, cursor - CellWidth + 1);
        if (cursor < start) {
            start = cursor;
        }

        var visible = normalized.Length <= start
            ? string.Empty
            : normalized.Substring(start, Math.Min(CellWidth, normalized.Length - start));

        WriteGridCell(x, y, visible, CellWidth, selected: true, header: false, highlight: GridHighlightKind.None, rightAlign: false);
        Console.SetCursorPosition(Math.Min(x + Math.Max(0, cursor - start), x + CellWidth - 1), y);
    }

    private bool TryCommitCellEdit(string text) {
        return TryCommitCellEdit(_current, text);
    }

    private bool TryCommitCellEdit(CellAddress address, string text) {
        try {
            _document.SetRaw(address, text);
            SetStatus($"Celda {address.ToA1()} actualizada.");
            return true;
        }
        catch (Exception ex) {
            SetStatus(string.IsNullOrWhiteSpace(ex.Message) ? "Expresion invalida." : ex.Message);
            return false;
        }
    }

    private static void InsertOrReplaceReference(
        StringBuilder editor,
        ref int cursor,
        CellAddress referenceAddress,
        ref int referenceStart,
        ref int referenceLength) {
        var referenceText = referenceAddress.ToA1();
        var insertionPoint = cursor;
        if (referenceStart >= 0 && referenceStart + referenceLength <= editor.Length) {
            editor.Remove(referenceStart, referenceLength);
            insertionPoint = referenceStart;
        }

        var insertion = BuildReferenceInsertion(editor, insertionPoint, referenceText);
        editor.Insert(insertionPoint, insertion);
        referenceStart = insertionPoint;
        referenceLength = insertion.Length;
        cursor = insertionPoint + insertion.Length;
    }

    private static void ResetReferenceTracking(ref int referenceStart, ref int referenceLength) {
        referenceStart = -1;
        referenceLength = 0;
    }

    private static string BuildReferenceInsertion(StringBuilder editor, int insertionPoint, string referenceText) {
        var prefix = insertionPoint > 0 && !char.IsWhiteSpace(editor[insertionPoint - 1]) ? " " : string.Empty;
        var suffix = insertionPoint >= editor.Length || !char.IsWhiteSpace(editor[insertionPoint]) ? " " : string.Empty;
        return prefix + referenceText + suffix;
    }

    private static bool SupportsPointMode(StringBuilder editor) {
        return editor.Length > 0 && editor[0] == '=';
    }

    private bool TryHandlePointSelection(
        ConsoleKeyInfo key,
        ref CellAddress referenceAddress,
        StringBuilder editor,
        ref int cursor,
        ref int referenceStart,
        ref int referenceLength) {
        if (!TryGetDirectionDelta(key.Key, out var rowDelta, out var columnDelta)) {
            return false;
        }

        referenceAddress = new CellAddress(
            Math.Clamp(referenceAddress.Row + rowDelta, 0, CellAddress.MaxRows - 1),
            Math.Clamp(referenceAddress.Column + columnDelta, 0, CellAddress.MaxColumns - 1));
        InsertOrReplaceReference(editor, ref cursor, referenceAddress, ref referenceStart, ref referenceLength);
        _referencePreview = referenceAddress;
        SetStatus($"Referencia {referenceAddress.ToA1()} insertada.");
        return true;
    }

    private void SetPointMode(ref bool pointMode, bool enabled, CellAddress referenceAddress) {
        pointMode = enabled;
        _referencePreview = enabled ? referenceAddress : null;
        SetStatus(enabled
            ? "Modo senalar. Flechas eligen celdas; Tab vuelve a editar."
            : "Modo editar.");
    }

    private void ExitPointMode(ref bool pointMode) {
        pointMode = false;
        _referencePreview = null;
    }

    private static bool ShouldLeavePointModeForEditing(ConsoleKeyInfo key) {
        if (!char.IsControl(key.KeyChar)) {
            return true;
        }

        return key.Key is ConsoleKey.Backspace or ConsoleKey.Delete or ConsoleKey.Home or ConsoleKey.End;
    }

    private void ClearCurrentCell() {
        try {
            _document.SetRaw(_current, string.Empty);
            SetStatus($"Celda {_current.ToA1()} borrada.");
        }
        catch (Exception ex) {
            SetStatus(string.IsNullOrWhiteSpace(ex.Message) ? "No se pudo borrar la celda." : ex.Message);
        }
    }

    private void MoveCurrent(int rowDelta, int columnDelta) {
        _current = new CellAddress(
            Math.Clamp(_current.Row + rowDelta, 0, CellAddress.MaxRows - 1),
            Math.Clamp(_current.Column + columnDelta, 0, CellAddress.MaxColumns - 1));
    }

    private ConsoleKeyInfo ReadNormalizedKey() {
        if (_pendingKeys.Count > 0) {
            return _pendingKeys.Dequeue();
        }

        var key = Console.ReadKey(intercept: true);
        if (key.Key != ConsoleKey.Escape || !Console.KeyAvailable) {
            return key;
        }

        if (TryReadEscapeSequence(out var translated)) {
            return translated;
        }

        return key;
    }

    private bool TryReadEscapeSequence(out ConsoleKeyInfo translated) {
        translated = default;
        if (!Console.KeyAvailable) {
            return false;
        }

        var captured = new List<ConsoleKeyInfo>
        {
            Console.ReadKey(intercept: true)
        };

        if (captured[0].KeyChar is '[' or 'O') {
            while (Console.KeyAvailable && captured.Count < 8) {
                var next = Console.ReadKey(intercept: true);
                captured.Add(next);
                if (IsEscapeSequenceTerminator(next)) {
                    break;
                }
            }
        }

        if (TryTranslateEscapeSequence(captured, out translated)) {
            return true;
        }

        foreach (var key in captured) {
            _pendingKeys.Enqueue(key);
        }

        return false;
    }

    private static bool IsEscapeSequenceTerminator(ConsoleKeyInfo key) {
        var character = key.KeyChar;
        return key.Key == ConsoleKey.Enter || character == '~' || char.IsLetter(character);
    }

    private static bool TryTranslateEscapeSequence(IReadOnlyList<ConsoleKeyInfo> captured, out ConsoleKeyInfo translated) {
        translated = default;
        if (captured.Count == 0) {
            return false;
        }

        if (captured.Count == 1) {
            var key = captured[0];
            if (key.Key == ConsoleKey.Enter || key.KeyChar is '\r' or '\n') {
                translated = CreateAltKey(ConsoleKey.Enter, '\r');
                return true;
            }

            switch (char.ToLowerInvariant(key.KeyChar)) {
                case 'b':
                    translated = CreateAltKey(ConsoleKey.LeftArrow);
                    return true;
                case 'f':
                    translated = CreateAltKey(ConsoleKey.RightArrow);
                    return true;
                case 'p':
                    translated = CreateAltKey(ConsoleKey.UpArrow);
                    return true;
                case 'n':
                    translated = CreateAltKey(ConsoleKey.DownArrow);
                    return true;
            }

            return false;
        }

        var sequence = new string(captured
            .Select(key => key.KeyChar)
            .Where(character => character != '\0')
            .ToArray());

        switch (sequence) {
            case "[A":
            case "[1;3A":
            case "[1;9A":
            case "OA":
                translated = CreateAltKey(ConsoleKey.UpArrow);
                return true;
            case "[B":
            case "[1;3B":
            case "[1;9B":
            case "OB":
                translated = CreateAltKey(ConsoleKey.DownArrow);
                return true;
            case "[C":
            case "[1;3C":
            case "[1;9C":
            case "OC":
                translated = CreateAltKey(ConsoleKey.RightArrow);
                return true;
            case "[D":
            case "[1;3D":
            case "[1;9D":
            case "OD":
                translated = CreateAltKey(ConsoleKey.LeftArrow);
                return true;
            default:
                return false;
        }
    }

    private static ConsoleKeyInfo CreateAltKey(ConsoleKey key, char character = '\0') {
        return new ConsoleKeyInfo(character, key, shift: false, alt: true, control: false);
    }

    private static bool TryGetDirectionDelta(ConsoleKey key, out int rowDelta, out int columnDelta) {
        rowDelta = 0;
        columnDelta = 0;

        switch (key) {
            case ConsoleKey.LeftArrow:
                columnDelta = -1;
                return true;
            case ConsoleKey.RightArrow:
                columnDelta = 1;
                return true;
            case ConsoleKey.UpArrow:
                rowDelta = -1;
                return true;
            case ConsoleKey.DownArrow:
                rowDelta = 1;
                return true;
            default:
                return false;
        }
    }

    private bool TryGetVisibleCellPosition(CellAddress address, out int x, out int y) {
        x = 0;
        y = 0;

        var visibleColumns = VisibleColumnCount();
        var visibleRows = VisibleRowCount();
        if (address.Column < _columnOffset || address.Column >= _columnOffset + visibleColumns ||
            address.Row < _rowOffset || address.Row >= _rowOffset + visibleRows) {
            return false;
        }

        x = RowHeaderWidth + 1 + (address.Column - _columnOffset) * (CellWidth + 1);
        y = HeaderLines + 1 + (address.Row - _rowOffset);
        return true;
    }

    private void EnsureVisible() {
        EnsureVisible(_current);
    }

    private void EnsureVisible(CellAddress address) {
        var visibleRows = VisibleRowCount();
        var visibleColumns = VisibleColumnCount();

        if (address.Row < _rowOffset) {
            _rowOffset = address.Row;
        }
        else if (address.Row >= _rowOffset + visibleRows) {
            _rowOffset = address.Row - visibleRows + 1;
        }

        if (address.Column < _columnOffset) {
            _columnOffset = address.Column;
        }
        else if (address.Column >= _columnOffset + visibleColumns) {
            _columnOffset = address.Column - visibleColumns + 1;
        }
    }

    private int VisibleRowCount() {
        return Math.Max(1, Console.WindowHeight - HeaderLines - FooterLines - 1);
    }

    private int VisibleColumnCount() {
        return Math.Max(1, (Console.WindowWidth - RowHeaderWidth) / (CellWidth + 1));
    }

    private string? PromptFormulaInput(string label, string initial) {
        var buffer = new StringBuilder(initial);
        var cursor = buffer.Length;
        var line = Console.WindowHeight - 1;
        var referenceAddress = _current;
        var referenceStart = -1;
        var referenceLength = 0;
        var pointMode = false;
        SetCursorVisibility(true);

        try {
            while (true) {
                EnsureVisible(_referencePreview ?? _current);
                Render();
                DrawPrompt($"{label} [{(pointMode ? "senalar" : "editar")}]: ", buffer.ToString(), cursor, line);

                var key = ReadNormalizedKey();
                if (pointMode &&
                    TryHandlePointSelection(key, ref referenceAddress, buffer, ref cursor, ref referenceStart, ref referenceLength)) {
                    continue;
                }

                if (pointMode && ShouldLeavePointModeForEditing(key)) {
                    ExitPointMode(ref pointMode);
                }

                switch (key.Key) {
                    case ConsoleKey.Enter:
                        ExitPointMode(ref pointMode);
                        return buffer.ToString();
                    case ConsoleKey.Escape:
                        ExitPointMode(ref pointMode);
                        return null;
                    case ConsoleKey.Tab:
                        SetPointMode(ref pointMode, !pointMode, referenceAddress);
                        break;
                    case ConsoleKey.LeftArrow:
                        cursor = Math.Max(0, cursor - 1);
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        break;
                    case ConsoleKey.RightArrow:
                        cursor = Math.Min(buffer.Length, cursor + 1);
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        break;
                    case ConsoleKey.Home:
                        cursor = 0;
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        break;
                    case ConsoleKey.End:
                        cursor = buffer.Length;
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        break;
                    case ConsoleKey.Backspace:
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        if (cursor > 0) {
                            buffer.Remove(cursor - 1, 1);
                            cursor--;
                        }
                        break;
                    case ConsoleKey.Delete:
                        ResetReferenceTracking(ref referenceStart, ref referenceLength);
                        if (cursor < buffer.Length) {
                            buffer.Remove(cursor, 1);
                        }
                        break;
                    default:
                        if (!char.IsControl(key.KeyChar)) {
                            ResetReferenceTracking(ref referenceStart, ref referenceLength);
                            buffer.Insert(cursor, key.KeyChar);
                            cursor++;
                        }
                        break;
                }
            }
        }
        finally {
            _referencePreview = null;
            SetCursorVisibility(false);
        }
    }

    private string? PromptInput(
        string label,
        string initial,
        TryCompleteInput? tryComplete = null) {
        var buffer = new StringBuilder(initial);
        var cursor = buffer.Length;
        var line = Console.WindowHeight - 1;
        SetCursorVisibility(true);

        try {
            while (true) {
                DrawPrompt(label, buffer.ToString(), cursor, line);
                var key = ReadNormalizedKey();
                switch (key.Key) {
                    case ConsoleKey.Enter:
                        return buffer.ToString();
                    case ConsoleKey.Escape:
                        return null;
                    case ConsoleKey.Tab when tryComplete is not null:
                        if (tryComplete(buffer.ToString(), cursor, out var completedText, out var completedCursor)) {
                            buffer.Clear();
                            buffer.Append(completedText);
                            cursor = Math.Clamp(completedCursor, 0, buffer.Length);
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        cursor = Math.Max(0, cursor - 1);
                        break;
                    case ConsoleKey.RightArrow:
                        cursor = Math.Min(buffer.Length, cursor + 1);
                        break;
                    case ConsoleKey.Home:
                        cursor = 0;
                        break;
                    case ConsoleKey.End:
                        cursor = buffer.Length;
                        break;
                    case ConsoleKey.Backspace:
                        if (cursor > 0) {
                            buffer.Remove(cursor - 1, 1);
                            cursor--;
                        }
                        break;
                    case ConsoleKey.Delete:
                        if (cursor < buffer.Length) {
                            buffer.Remove(cursor, 1);
                        }
                        break;
                    default:
                        if (!char.IsControl(key.KeyChar)) {
                            buffer.Insert(cursor, key.KeyChar);
                            cursor++;
                        }
                        break;
                }
            }
        }
        finally {
            SetCursorVisibility(false);
        }
    }

    private delegate bool TryCompleteInput(string text, int cursor, out string completedText, out int completedCursor);

    private void DrawPrompt(string label, string value, int cursor, int line) {
        var maxValueLength = Math.Max(10, Console.WindowWidth - label.Length - 1);
        var start = Math.Max(0, cursor - maxValueLength + 1);
        var visible = value.Length <= maxValueLength
            ? value
            : value.Substring(start, Math.Min(maxValueLength, value.Length - start));
        WriteLine(line, label + visible);
        Console.SetCursorPosition(Math.Min(label.Length + cursor - start, Console.WindowWidth - 1), line);
    }

    private void WriteLine(int y, string text) {
        if (y < 0 || y >= Console.WindowHeight) {
            return;
        }

        Console.SetCursorPosition(0, y);
        Console.Write(Fit(text, Console.WindowWidth).PadRight(Console.WindowWidth));
    }

    private void ClearLine(int y) => WriteLine(y, string.Empty);

    private void WriteSeparator(int x, int y) {
        if (x < 0 || x >= Console.WindowWidth || y < 0 || y >= Console.WindowHeight) {
            return;
        }

        Console.SetCursorPosition(x, y);
        Console.ResetColor();
        Console.Write("⏐");
    }

    private void WriteGridCell(int x, int y, string text, int width, bool selected, bool header, GridHighlightKind highlight, bool rightAlign) {
        if (x >= Console.WindowWidth || y >= Console.WindowHeight) {
            return;
        }

        Console.SetCursorPosition(x, y);
        if (selected) {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
        }
        else if (header && highlight == GridHighlightKind.ActiveHeader) {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkCyan;
        }
        else if (header && highlight == GridHighlightKind.ReferenceHeader) {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Yellow;
        }
        else if (header) {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }
        else if (highlight == GridHighlightKind.ReferenceCell) {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.DarkYellow;
        }
        else {
            Console.ResetColor();
        }

        var fitted = Fit(text, width);
        Console.Write(rightAlign ? fitted.PadLeft(width) : fitted.PadRight(width));
        Console.ResetColor();
    }

    private static GridHighlightKind GetHeaderHighlight(bool isCurrent, bool isReference) {
        if (isReference) {
            return GridHighlightKind.ReferenceHeader;
        }

        if (isCurrent) {
            return GridHighlightKind.ActiveHeader;
        }

        return GridHighlightKind.None;
    }

    private void SetStatus(string message) {
        _status = message;
    }

    private string FormatMode(CellAddress address) {
        var cell = _document.GetCell(address);
        var value = _document.GetDisplayValue(address, _engine);
        if (value.IsError || value.Kind == CalcValueKind.Empty) {
            return "general";
        }

        return value.Kind == CalcValueKind.Number
            ? DescribeFormat(ResolveNumericFormat(cell.DisplayFormat))
            : DescribeFormat(ResolveTextFormat(cell.DisplayFormat));
    }

    private static CellDisplayFormat NextNumericFormat(CellDisplayFormat current) {
        return ResolveNumericFormat(current) switch {
            CellDisplayFormat.NumberNormal => CellDisplayFormat.NumberTwoDecimals,
            CellDisplayFormat.NumberTwoDecimals => CellDisplayFormat.NumberPercentage,
            CellDisplayFormat.NumberPercentage => CellDisplayFormat.NumberCurrency,
            _ => CellDisplayFormat.NumberNormal
        };
    }

    private static CellDisplayFormat NextTextFormat(CellDisplayFormat current) {
        return ResolveTextFormat(current) switch {
            CellDisplayFormat.TextNormal => CellDisplayFormat.TextUpper,
            CellDisplayFormat.TextUpper => CellDisplayFormat.TextLower,
            CellDisplayFormat.TextLower => CellDisplayFormat.TextTitle,
            _ => CellDisplayFormat.TextNormal
        };
    }

    private static CellDisplayFormat ResolveNumericFormat(CellDisplayFormat format) {
        return format switch {
            CellDisplayFormat.NumberTwoDecimals => CellDisplayFormat.NumberTwoDecimals,
            CellDisplayFormat.NumberPercentage => CellDisplayFormat.NumberPercentage,
            CellDisplayFormat.NumberCurrency => CellDisplayFormat.NumberCurrency,
            _ => CellDisplayFormat.NumberNormal
        };
    }

    private static CellDisplayFormat ResolveTextFormat(CellDisplayFormat format) {
        return format switch {
            CellDisplayFormat.TextUpper => CellDisplayFormat.TextUpper,
            CellDisplayFormat.TextLower => CellDisplayFormat.TextLower,
            CellDisplayFormat.TextTitle => CellDisplayFormat.TextTitle,
            _ => CellDisplayFormat.TextNormal
        };
    }

    private static string DescribeFormat(CellDisplayFormat format) {
        return format switch {
            CellDisplayFormat.NumberNormal => "normal",
            CellDisplayFormat.NumberTwoDecimals => "2 decimales",
            CellDisplayFormat.NumberPercentage => "porcentaje",
            CellDisplayFormat.NumberCurrency => "monetario",
            CellDisplayFormat.TextUpper => "mayusculas",
            CellDisplayFormat.TextLower => "minusculas",
            CellDisplayFormat.TextTitle => "nombre propio",
            _ => "normal"
        };
    }

    private static string Fit(string text, int width) {
        if (width <= 0) {
            return string.Empty;
        }

        var normalized = text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        if (normalized.Length <= width) {
            return normalized;
        }

        return width <= 3 ? normalized[..width] : normalized[..(width - 3)] + "...";
    }

    private static void SetCursorVisibility(bool visible) {
        try {
            Console.CursorVisible = visible;
        }
        catch (PlatformNotSupportedException) {
        }
    }
}

internal enum GridHighlightKind {
    None,
    ActiveHeader,
    ReferenceHeader,
    ReferenceCell
}
