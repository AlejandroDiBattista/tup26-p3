#!/usr/bin/env -S dotnet run
// CSV Spreadsheet TUI (single file, C# 14 file-based app)
// Run: dotnet ./CsvSheetPro.cs sample.csv
// Requires .NET 10+
#:package Terminal.Gui@1.16.3

using System.Data;
using System.Text;
using Terminal.Gui;

if (args.Length == 0) {
    Console.WriteLine("Uso: dotnet ./CsvSheetPro.cs archivo.csv");
    return;
}

var path = args[0];

if (!File.Exists(path)) {
    File.WriteAllText(path, "Col1,Col2,Col3\n", Encoding.UTF8);
}

var table = LoadCsv(path);

Application.Init();
var top = Application.Top;

var win = new Window($"CSV Sheet Pro - {System.IO.Path.GetFileName(path)}") {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill() - 1
};

var tableView = new TableView() {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(1),
    Table = table,
    FullRowSelect = true
};

var editorLabel = new Label("Celda:") {
    X = 0,
    Y = Pos.Bottom(tableView),
    Width = 14
};

var editorField = new TextField("") {
    X = Pos.Right(editorLabel) + 1,
    Y = Pos.Top(editorLabel),
    Width = Dim.Fill(),
    ReadOnly = true
};

var editingRow = -1;
var editingCol = -1;
var originalValue = string.Empty;
var isEditing = false;

var status = new StatusBar(new StatusItem[] {
    new StatusItem(Key.CtrlMask | Key.S, "~Ctrl-S~ Guardar", () => { CommitInlineEdit(); SaveCsv(table, path); }),
    new StatusItem(Key.F2, "~F2~ Editar inline", () => EditSelected()),
    new StatusItem(Key.CtrlMask | Key.N, "~Ctrl-N~ Nueva fila", () => { CancelInlineEdit(); AddRow(); }),
    new StatusItem(Key.CtrlMask | Key.D, "~Ctrl-D~ Borrar fila", () => { CancelInlineEdit(); DeleteRow(); }),
    new StatusItem(Key.CtrlMask | Key.F, "~Ctrl-F~ Buscar", () => { CancelInlineEdit(); Search(); }),
    new StatusItem(Key.CtrlMask | Key.O, "~Ctrl-O~ Ordenar columna", () => { CancelInlineEdit(); SortColumn(); }),
    new StatusItem(Key.CtrlMask | Key.Q, "~Ctrl-Q~ Salir", () => Application.RequestStop())
});

win.Add(tableView, editorLabel, editorField);
top.Add(win);
top.Add(status);

tableView.CellActivated += a => EditCell(a.Row, a.Col);

tableView.SelectedCellChanged += _ => {
    if (!isEditing) {
        UpdateInlineEditor();
    }
};

tableView.KeyPress += e => {
    if (e.KeyEvent.Key == Key.F2 || e.KeyEvent.Key == Key.Enter) {
        EditSelected();
        e.Handled = true;
    }
};

editorField.KeyPress += e => {
    if (e.KeyEvent.Key == Key.Enter) {
        CommitInlineEdit();
        e.Handled = true;
    }

    if (e.KeyEvent.Key == Key.Esc) {
        CancelInlineEdit();
        e.Handled = true;
    }
};

UpdateInlineEditor();

Application.Run();
Application.Shutdown();

// ===============================
// FUNCIONES
// ===============================

void EditSelected() {
    EditCell(tableView.SelectedRow, tableView.SelectedColumn);
}

void EditCell(int row, int col) {
    if (row < 0 || col < 0) {
        return;
    }

    editingRow = row;
    editingCol = col;
    originalValue = table.Rows[row][col]?.ToString() ?? string.Empty;
    isEditing = true;

    editorLabel.Text = $"Editar [{row + 1},{col + 1}]:";
    editorField.ReadOnly = false;
    editorField.Text = originalValue;
    editorField.SetFocus();
}

void CommitInlineEdit() {
    if (!isEditing) {
        return;
    }

    table.Rows[editingRow][editingCol] = editorField.Text.ToString();
    FinishInlineEdit();
}

void CancelInlineEdit() {
    if (!isEditing) {
        return;
    }

    editorField.Text = originalValue;
    FinishInlineEdit();
}

void FinishInlineEdit() {
    isEditing = false;
    editorField.ReadOnly = true;
    tableView.SetFocus();
    tableView.Update();
    UpdateInlineEditor();
}

void UpdateInlineEditor() {
    var row = tableView.SelectedRow;
    var col = tableView.SelectedColumn;

    if (row < 0 || col < 0 || row >= table.Rows.Count || col >= table.Columns.Count) {
        editorLabel.Text = "Celda:";
        editorField.Text = string.Empty;
        return;
    }

    editorLabel.Text = $"[{row + 1},{col + 1}]:";
    editorField.Text = table.Rows[row][col]?.ToString() ?? string.Empty;
}

void AddRow() {
    var r = table.NewRow();
    table.Rows.Add(r);
    tableView.Update();
    UpdateInlineEditor();
}

void DeleteRow() {
    var r = tableView.SelectedRow;
    if (r >= 0 && r < table.Rows.Count) {
        table.Rows.RemoveAt(r);
        tableView.Update();
        UpdateInlineEditor();
    }
}

void Search() {
    var dialog = new Dialog("Buscar", 50, 7);
    var field = new TextField("") { X = 1, Y = 1, Width = Dim.Fill() - 2 };

    var ok = new Button("Buscar") { X = Pos.Center(), Y = 3 };

    ok.Clicked += () => {
        var txt = field.Text.ToString() ?? string.Empty;

        for (int i = 0; i < table.Rows.Count; i++) {
            foreach (var cell in table.Rows[i].ItemArray) {
                if (cell != null && cell.ToString()!.Contains(txt, StringComparison.OrdinalIgnoreCase)) {
                    tableView.SelectedRow = i;
                    Application.RequestStop();
                    return;
                }
            }
        }

        MessageBox.ErrorQuery("No encontrado", "Texto no encontrado", "OK");
    };

    dialog.Add(field, ok);
    Application.Run(dialog);
}

void SortColumn() {
    var col = tableView.SelectedColumn;
    if (col < 0) {
        return;
    }

    var columnName = table.Columns[col].ColumnName;
    var sortDirection = GetNextSortDirection(col);

    var view = new DataView(table);
    view.Sort = EscapeColumnName(columnName) + " " + sortDirection;

    table = view.ToTable();
    tableView.Table = table;
    tableView.Update();
    UpdateInlineEditor();
}

string GetNextSortDirection(int col) {
    if (IsSortedAscending(col)) {
        return "DESC";
    }

    return "ASC";
}

bool IsSortedAscending(int col) {
    for (int row = 1; row < table.Rows.Count; row++) {
        var previous = table.Rows[row - 1][col];
        var current = table.Rows[row][col];

        if (CompareCellValues(previous, current) > 0) {
            return false;
        }
    }

    return true;
}

int CompareCellValues(object? left, object? right) {
    var leftText = left?.ToString() ?? string.Empty;
    var rightText = right?.ToString() ?? string.Empty;
    return StringComparer.CurrentCultureIgnoreCase.Compare(leftText, rightText);
}

string EscapeColumnName(string columnName) {
    return "[" + columnName.Replace("]", "]]") + "]";
}

DataTable LoadCsv(string path) {
    var table = new DataTable();
    var lines = File.ReadAllLines(path);

    if (lines.Length == 0) {
        table.Columns.Add("Col1");
        table.Columns.Add("Col2");
        table.Columns.Add("Col3");
        return table;
    }

    var headers = lines[0].Split(',');
    foreach (var h in headers) {
        table.Columns.Add(h);
    }

    foreach (var l in lines.Skip(1)) {
        table.Rows.Add(l.Split(','));
    }

    return table;
}

void SaveCsv(DataTable table, string path) {
    using var w = new StreamWriter(path, false, Encoding.UTF8);

    var headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
    w.WriteLine(string.Join(",", headers));

    foreach (DataRow r in table.Rows) {
        var cells = r.ItemArray.Select(c => c?.ToString());
        w.WriteLine(string.Join(",", cells));
    }
}
