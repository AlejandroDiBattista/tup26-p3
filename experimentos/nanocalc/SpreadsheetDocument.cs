using System.Globalization;

namespace NanoCalc;

internal sealed class SpreadsheetDocument {
    private readonly Cell[][] _grid;
    private readonly Dictionary<string, CellAddress> _variableDefinitions = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly Dictionary<string, CellAddress> _implicitVariableDefinitions = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly Dictionary<string, FunctionDefinitionInfo> _functionDefinitions = new(StringComparer.CurrentCultureIgnoreCase);

    public SpreadsheetDocument() {
        _grid = Enumerable.Range(0, CellAddress.MaxRows)
            .Select(_ => CreateEmptyRow())
            .ToArray();
        RebuildNamedDefinitions();
    }

    public string? CurrentFileName { get; private set; }
    public int Version { get; private set; }

    public Cell GetCell(CellAddress address) {
        return address.IsValid ? _grid[address.Row][address.Column] : new Cell();
    }

    public string GetRaw(CellAddress address) {
        return GetCell(address).GetEditableText(address);
    }

    public void SetRaw(CellAddress address, string rawText) {
        if (!address.IsValid) {
            return;
        }

        _grid[address.Row][address.Column].SetRaw(address, rawText);
        Touch();
    }

    public CalcValue GetDisplayValue(CellAddress address, EvaluationEngine engine) {
        var cell = GetCell(address);
        return cell.Content switch {
            EmptyCellContent => CalcValue.Empty,
            NumberCellContent number => CalcValue.FromNumber(number.Value),
            StringCellContent text => CalcValue.FromText(text.Value),
            VariableDefinitionCellContent variable => CalcValue.FromText(variable.Name),
            FunctionDefinitionCellContent function => CalcValue.FromText(function.Name),
            FormulaCellContent => engine.EvaluateCell(address),
            _ => CalcValue.Empty
        };
    }

    public bool TryGetVariableAddress(string name, out CellAddress address) {
        return _variableDefinitions.TryGetValue(name, out address);
    }

    public bool TryGetImplicitVariableAddress(string name, out CellAddress address) {
        return _implicitVariableDefinitions.TryGetValue(name, out address);
    }

    public bool TryGetFunction(string name, out FunctionDefinitionInfo info) {
        return _functionDefinitions.TryGetValue(name, out info);
    }

    public bool IsNumericColumn(int columnIndex, EvaluationEngine engine) {
        for (var row = 0; row < CellAddress.MaxRows; row++) {
            var address = new CellAddress(row, columnIndex);
            var cell = GetCell(address);
            if (cell.IsEmpty) {
                continue;
            }

            var value = GetDisplayValue(address, engine);
            if (value.IsError) {
                continue;
            }

            if (value.Kind == CalcValueKind.Number) {
                return true;
            }

            if (value.Kind == CalcValueKind.Text && !TryParseDisplayNumber(value.Text, out _)) {
                return false;
            }
        }

        return false;
    }

    public void InsertRow(int rowIndex) {
        if (rowIndex < 0 || rowIndex >= CellAddress.MaxRows) {
            return;
        }

        for (var row = CellAddress.MaxRows - 1; row > rowIndex; row--) {
            _grid[row] = _grid[row - 1];
        }

        _grid[rowIndex] = CreateEmptyRow();
        Touch();
    }

    public void DeleteRow(int rowIndex) {
        if (rowIndex < 0 || rowIndex >= CellAddress.MaxRows) {
            return;
        }

        for (var row = rowIndex; row < CellAddress.MaxRows - 1; row++) {
            _grid[row] = _grid[row + 1];
        }

        _grid[CellAddress.MaxRows - 1] = CreateEmptyRow();
        Touch();
    }

    public void InsertColumn(int columnIndex) {
        if (columnIndex < 0 || columnIndex >= CellAddress.MaxColumns) {
            return;
        }

        for (var row = 0; row < CellAddress.MaxRows; row++) {
            for (var column = CellAddress.MaxColumns - 1; column > columnIndex; column--) {
                _grid[row][column] = _grid[row][column - 1];
            }

            _grid[row][columnIndex] = new Cell();
        }

        Touch();
    }

    public void DeleteColumn(int columnIndex) {
        if (columnIndex < 0 || columnIndex >= CellAddress.MaxColumns) {
            return;
        }

        for (var row = 0; row < CellAddress.MaxRows; row++) {
            for (var column = columnIndex; column < CellAddress.MaxColumns - 1; column++) {
                _grid[row][column] = _grid[row][column + 1];
            }

            _grid[row][CellAddress.MaxColumns - 1] = new Cell();
        }

        Touch();
    }

    public SortOutcome SortByColumns(IReadOnlyList<int> columns, EvaluationEngine engine) {
        var lastUsedRow = GetLastUsedRow();
        if (lastUsedRow <= 1) {
            return SortOutcome.NotChanged;
        }

        var dataRows = Enumerable.Range(1, lastUsedRow - 1)
            .Select(row => _grid[row])
            .ToList();

        var currentOrder = IsSorted(dataRows, columns, engine, ascending: true)
            ? SortDirection.Ascending
            : IsSorted(dataRows, columns, engine, ascending: false)
                ? SortDirection.Descending
                : SortDirection.Unsorted;

        var nextDirection = currentOrder == SortDirection.Ascending
            ? SortDirection.Descending
            : SortDirection.Ascending;

        dataRows.Sort((left, right) => CompareRows(left, right, columns, engine, nextDirection == SortDirection.Ascending));

        for (var row = 1; row < lastUsedRow; row++) {
            _grid[row] = dataRows[row - 1];
        }

        Touch();
        return nextDirection == SortDirection.Ascending ? SortOutcome.Ascending : SortOutcome.Descending;
    }

    public void Save(string fileName) {
        var outputPath = Path.Combine(Environment.CurrentDirectory, fileName);
        using var writer = new StreamWriter(outputPath);

        for (var row = 0; row < CellAddress.MaxRows; row++) {
            for (var column = 0; column < CellAddress.MaxColumns; column++) {
                var address = new CellAddress(row, column);
                var raw = GetRaw(address);
                if (string.IsNullOrWhiteSpace(raw)) {
                    continue;
                }

                writer.Write(address.ToA1());
                writer.Write('\t');
                writer.WriteLine(raw);
            }
        }

        CurrentFileName = fileName;
    }

    public void Load(string fileName) {
        var inputPath = Path.Combine(Environment.CurrentDirectory, fileName);
        if (!File.Exists(inputPath)) {
            throw new FileNotFoundException("Archivo inexistente.", inputPath);
        }

        for (var row = 0; row < CellAddress.MaxRows; row++) {
            _grid[row] = CreateEmptyRow();
        }

        foreach (var line in File.ReadAllLines(inputPath)) {
            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }

            var tabIndex = line.IndexOf('\t');
            if (tabIndex <= 0) {
                continue;
            }

            var cellToken = line[..tabIndex].Trim();
            var raw = line[(tabIndex + 1)..];
            if (CellAddress.TryParse(cellToken, out var address)) {
                _grid[address.Row][address.Column].SetRaw(address, raw);
            }
        }

        CurrentFileName = fileName;
        Touch();
    }

    public int GetLastUsedRow() {
        for (var row = CellAddress.MaxRows - 1; row >= 0; row--) {
            if (_grid[row].Any(cell => !cell.IsEmpty)) {
                return row + 1;
            }
        }

        return 0;
    }

    public int GetLastUsedColumn() {
        for (var column = CellAddress.MaxColumns - 1; column >= 0; column--) {
            for (var row = 0; row < CellAddress.MaxRows; row++) {
                if (!_grid[row][column].IsEmpty) {
                    return column + 1;
                }
            }
        }

        return 0;
    }

    private void Touch() {
        Version++;
        RebuildNamedDefinitions();
    }

    private void RebuildNamedDefinitions() {
        _variableDefinitions.Clear();
        _implicitVariableDefinitions.Clear();
        _functionDefinitions.Clear();

        for (var row = 0; row < CellAddress.MaxRows; row++) {
            for (var column = 0; column < CellAddress.MaxColumns; column++) {
                var address = new CellAddress(row, column);
                var content = _grid[row][column].Content;

                switch (content) {
                    case VariableDefinitionCellContent variable when !_variableDefinitions.ContainsKey(variable.Name):
                        _variableDefinitions.Add(variable.Name, address);
                        break;

                    case StringCellContent text when !_implicitVariableDefinitions.ContainsKey(text.Value):
                        _implicitVariableDefinitions.Add(text.Value, address);
                        break;

                    case FunctionDefinitionCellContent function when !_functionDefinitions.ContainsKey(function.Name):
                        _functionDefinitions.Add(function.Name, new FunctionDefinitionInfo(address, function));
                        break;
                }
            }
        }
    }

    private static Cell[] CreateEmptyRow() {
        return Enumerable.Range(0, CellAddress.MaxColumns)
            .Select(_ => new Cell())
            .ToArray();
    }

    private bool IsSorted(List<Cell[]> rows, IReadOnlyList<int> columns, EvaluationEngine engine, bool ascending) {
        for (var index = 1; index < rows.Count; index++) {
            if (CompareRows(rows[index - 1], rows[index], columns, engine, ascending) > 0) {
                return false;
            }
        }

        return true;
    }

    private int CompareRows(Cell[] left, Cell[] right, IReadOnlyList<int> columns, EvaluationEngine engine, bool ascending) {
        foreach (var column in columns) {
            var leftIndex = Array.IndexOf(_grid, left);
            var rightIndex = Array.IndexOf(_grid, right);
            var leftValue = GetDisplayValue(new CellAddress(leftIndex, column), engine);
            var rightValue = GetDisplayValue(new CellAddress(rightIndex, column), engine);

            var comparison = CompareValues(leftValue, rightValue);
            if (comparison != 0) {
                return ascending ? comparison : -comparison;
            }
        }

        return 0;
    }

    private static int CompareValues(CalcValue left, CalcValue right) {
        if (left.IsError || right.IsError) {
            return string.Compare(left.ToText(), right.ToText(), StringComparison.CurrentCultureIgnoreCase);
        }

        if (left.Kind == CalcValueKind.Number && right.Kind == CalcValueKind.Number) {
            return left.Number.CompareTo(right.Number);
        }

        if (TryParseDisplayNumber(left.ToText(), out var leftNumber) &&
            TryParseDisplayNumber(right.ToText(), out var rightNumber)) {
            return leftNumber.CompareTo(rightNumber);
        }

        return string.Compare(left.ToText(), right.ToText(), StringComparison.CurrentCultureIgnoreCase);
    }

    private static bool TryParseDisplayNumber(string text, out decimal number) {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out number);
    }
}

internal readonly record struct FunctionDefinitionInfo(CellAddress Address, FunctionDefinitionCellContent Definition);

internal enum SortOutcome {
    NotChanged,
    Ascending,
    Descending
}

internal enum SortDirection {
    Unsorted,
    Ascending,
    Descending
}
