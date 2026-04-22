using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

if (args.Length == 0) {
    Console.WriteLine("Uso: dotnet run CsvSheet.cs archivo.csv");
    return;
}

string path = args[0];

if (!File.Exists(path)) {
    File.WriteAllText(path, "Col1,Col2,Col3\n");
}

DataTable table = LoadCsv(path);

while (true) {
    Console.Clear();
    Console.WriteLine($"CSV Editor - {Path.GetFileName(path)}");
    Console.WriteLine();
    PrintTable(table);
    Console.WriteLine();
    Console.WriteLine("Comandos: [E]ditar  [A]gregar fila  [D]elete fila  [S]ave  [Q]uit");
    Console.Write("> ");

    string? command = Console.ReadLine()?.Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(command)) {
        continue;
    }

    if (command[0] == 'q') {
        break;
    }

    if (command[0] == 's') {
        SaveCsv(table, path);
        Pause("Guardado.");
        continue;
    }

    if (command[0] == 'a') {
        table.Rows.Add(table.NewRow());
        continue;
    }

    if (command[0] == 'd') {
        int row = ReadInt($"Fila a borrar (1-{table.Rows.Count}): ", 1, table.Rows.Count);
        if (row > 0) {
            table.Rows.RemoveAt(row - 1);
        }
        continue;
    }

    if (command[0] == 'e') {
        if (table.Rows.Count == 0 || table.Columns.Count == 0) {
            Pause("No hay celdas para editar.");
            continue;
        }

        int row = ReadInt($"Fila (1-{table.Rows.Count}): ", 1, table.Rows.Count) - 1;
        int col = ReadInt($"Columna (1-{table.Columns.Count}): ", 1, table.Columns.Count) - 1;

        Console.Write("Nuevo valor: ");
        string value = Console.ReadLine() ?? string.Empty;
        table.Rows[row][col] = value;
        continue;
    }

    Pause("Comando no reconocido.");
}

DataTable LoadCsv(string csvPath) {
    DataTable result = new();
    string[] lines = File.ReadAllLines(csvPath);

    if (lines.Length == 0) {
        result.Columns.Add("Col1");
        return result;
    }

    string[] headers = lines[0].Split(',');

    foreach (string header in headers) {
        result.Columns.Add(header);
    }

    foreach (string line in lines.Skip(1)) {
        result.Rows.Add(line.Split(','));
    }

    return result;
}

void SaveCsv(DataTable data, string csvPath) {
    using StreamWriter writer = new(csvPath, false, Encoding.UTF8);

    string[] headers = data.Columns
        .Cast<DataColumn>()
        .Select(column => column.ColumnName)
        .ToArray();

    writer.WriteLine(string.Join(",", headers));

    foreach (DataRow row in data.Rows) {
        string[] cells = row.ItemArray.Select(cell => cell?.ToString() ?? string.Empty).ToArray();
        writer.WriteLine(string.Join(",", cells));
    }
}

void PrintTable(DataTable data) {
    if (data.Columns.Count == 0) {
        Console.WriteLine("(sin columnas)");
        return;
    }

    int[] widths = new int[data.Columns.Count];
    for (int c = 0; c < data.Columns.Count; c++) {
        widths[c] = Math.Max(3, data.Columns[c].ColumnName.Length);
    }

    foreach (DataRow row in data.Rows) {
        for (int c = 0; c < data.Columns.Count; c++) {
            string text = row[c]?.ToString() ?? string.Empty;
            widths[c] = Math.Max(widths[c], text.Length);
        }
    }

    Console.Write("    ");
    for (int c = 0; c < data.Columns.Count; c++) {
        Console.Write($" {data.Columns[c].ColumnName.PadRight(widths[c])} ");
    }
    Console.WriteLine();

    for (int r = 0; r < data.Rows.Count; r++) {
        Console.Write($"{r + 1,3} ");
        for (int c = 0; c < data.Columns.Count; c++) {
            string text = data.Rows[r][c]?.ToString() ?? string.Empty;
            Console.Write($" {text.PadRight(widths[c])} ");
        }
        Console.WriteLine();
    }
}

int ReadInt(string prompt, int min, int max) {
    while (true) {
        Console.Write(prompt);
        string? text = Console.ReadLine();

        if (int.TryParse(text, out int value) && value >= min && value <= max) {
            return value;
        }

        Console.WriteLine($"Valor inválido. Debe estar entre {min} y {max}.");
    }
}

void Pause(string message) {
    Console.WriteLine(message);
    Console.WriteLine("Presioná Enter para continuar...");
    Console.ReadLine();
}
