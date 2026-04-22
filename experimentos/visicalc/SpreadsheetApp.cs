namespace VisiCalc;

internal static class SpreadsheetApp {
    public static int Run(string[] args) {
        if (args.Length > 0) {
            string option = args[0].Trim();

            if (string.Equals(option, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(option, "-h", StringComparison.OrdinalIgnoreCase)) {
                PrintUsage();
                return 0;
            }

            if (string.Equals(option, "--self-test", StringComparison.OrdinalIgnoreCase)) {
                return SelfTest.Run();
            }

            if (string.Equals(option, "--demo", StringComparison.OrdinalIgnoreCase)) {
                Spreadsheet demo = CreateDemoSheet();
                Console.WriteLine("VisiCalc demo");
                Console.WriteLine();
                Console.WriteLine(demo.RenderSnapshot());
                return 0;
            }
        }

        Spreadsheet sheet = new();
        string? currentFile = null;

        if (args.Length > 0) {
            currentFile = args[0];
            if (File.Exists(currentFile)) {
                string text = File.ReadAllText(currentFile);
                sheet.LoadText(text);
            } else {
                Console.WriteLine($"No existe el archivo: {currentFile}");
                return 1;
            }
        }

        if (Console.IsInputRedirected || Console.IsOutputRedirected) {
            Console.WriteLine("Modo interactivo requiere una terminal real. Usa --demo o --self-test.");
            return 1;
        }

        RunInteractive(sheet, ref currentFile);
        return 0;
    }

    private static void RunInteractive(Spreadsheet sheet, ref string? currentFile) {
        int selectedRow = 0;
        int selectedColumn = 0;
        int topRow = 0;
        int leftColumn = 0;
        bool showRawValues = false;
        bool running = true;
        string status = "Flechas para moverte. Enter o F2 para editar.";

        while (running) {
            KeepSelectionInBounds(sheet, ref selectedRow, ref selectedColumn);
            KeepViewportVisible(sheet, selectedRow, selectedColumn, ref topRow, ref leftColumn);
            Render(sheet, selectedRow, selectedColumn, topRow, leftColumn, currentFile, showRawValues, status);

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            switch (key.Key) {
                case ConsoleKey.LeftArrow:
                    selectedColumn = Math.Max(0, selectedColumn - 1);
                    status = $"Celda {new CellAddress(selectedRow, selectedColumn)}.";
                    break;

                case ConsoleKey.RightArrow:
                    if (selectedColumn == sheet.ColumnCount - 1) {
                        sheet.Resize(sheet.RowCount, sheet.ColumnCount + 1);
                    }

                    selectedColumn++;
                    status = $"Celda {new CellAddress(selectedRow, selectedColumn)}.";
                    break;

                case ConsoleKey.UpArrow:
                    selectedRow = Math.Max(0, selectedRow - 1);
                    status = $"Celda {new CellAddress(selectedRow, selectedColumn)}.";
                    break;

                case ConsoleKey.DownArrow:
                    if (selectedRow == sheet.RowCount - 1) {
                        sheet.Resize(sheet.RowCount + 1, sheet.ColumnCount);
                    }

                    selectedRow++;
                    status = $"Celda {new CellAddress(selectedRow, selectedColumn)}.";
                    break;

                case ConsoleKey.PageUp:
                    selectedRow = Math.Max(0, selectedRow - 10);
                    status = $"Celda {new CellAddress(selectedRow, selectedColumn)}.";
                    break;

                case ConsoleKey.PageDown:
                    selectedRow = Math.Min(sheet.RowCount - 1, selectedRow + 10);
                    status = $"Celda {new CellAddress(selectedRow, selectedColumn)}.";
                    break;

                case ConsoleKey.Home:
                    selectedRow = 0;
                    selectedColumn = 0;
                    status = "Volviste a A1.";
                    break;

                case ConsoleKey.Enter:
                case ConsoleKey.F2:
                case ConsoleKey.E:
                    EditCell(sheet, new CellAddress(selectedRow, selectedColumn), ref status);
                    break;

                case ConsoleKey.Delete:
                case ConsoleKey.Backspace:
                    sheet.Clear(new CellAddress(selectedRow, selectedColumn));
                    status = $"Se limpio {new CellAddress(selectedRow, selectedColumn)}.";
                    break;

                case ConsoleKey.G:
                    JumpToCell(sheet, ref selectedRow, ref selectedColumn, ref status);
                    break;

                case ConsoleKey.O:
                    OpenSheet(sheet, ref currentFile, ref status);
                    selectedRow = 0;
                    selectedColumn = 0;
                    topRow = 0;
                    leftColumn = 0;
                    break;

                case ConsoleKey.S:
                    SaveSheet(sheet, ref currentFile, ref status);
                    break;

                case ConsoleKey.R:
                    ResizeSheet(sheet, ref status, ref selectedRow, ref selectedColumn);
                    break;

                case ConsoleKey.T:
                    showRawValues = !showRawValues;
                    status = showRawValues ? "Mostrando formulas y texto crudo." : "Mostrando resultados.";
                    break;

                case ConsoleKey.N:
                    sheet = CreateDemoSheet();
                    selectedRow = 0;
                    selectedColumn = 0;
                    topRow = 0;
                    leftColumn = 0;
                    currentFile = null;
                    status = "Se cargo una planilla demo.";
                    break;

                case ConsoleKey.F1:
                case ConsoleKey.H:
                case ConsoleKey.Oem2:
                    ShowHelp();
                    status = "Ayuda cerrada.";
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    running = false;
                    break;
            }
        }
    }

    private static void Render( Spreadsheet sheet, int selectedRow, int selectedColumn, int topRow, int leftColumn, string? currentFile, bool showRawValues, string status) {
        Console.Clear();
        Console.CursorVisible = false;

        const int rowHeaderWidth = 5;
        const int cellWidth = 12;

        int windowWidth = SafeGetWindowWidth();
        int windowHeight = SafeGetWindowHeight();
        int visibleColumns = Math.Max(1, (windowWidth - rowHeaderWidth - 1) / (cellWidth + 1));
        int visibleRows = Math.Max(4, windowHeight - 10);

        CellAddress selected = new(selectedRow, selectedColumn);
        CellValue selectedValue = sheet.Evaluate(selected);
        string fileLabel = currentFile ?? "(sin guardar)";

        Console.WriteLine("VisiCalc para consola");
        Console.WriteLine("F2/Enter editar | Del borrar | G ir | O abrir | S guardar | R tamano | T vista | N demo | ? ayuda | Q salir");
        Console.WriteLine($"Archivo: {fileLabel} | Vista: {(showRawValues ? "formulas" : "resultados")}");
        Console.WriteLine();

        Console.Write("".PadLeft(rowHeaderWidth));
        for (int column = 0; column < visibleColumns && leftColumn + column < sheet.ColumnCount; column++) {
            string name = CellAddress.FormatColumnName(leftColumn + column);
            WriteCell(name, cellWidth, selected: false, alignRight: false, isError: false);
        }
        Console.WriteLine();

        for (int row = 0; row < visibleRows && topRow + row < sheet.RowCount; row++) {
            int absoluteRow = topRow + row;
            Console.Write((absoluteRow + 1).ToString().PadLeft(rowHeaderWidth - 1));
            Console.Write(' ');

            for (int column = 0; column < visibleColumns && leftColumn + column < sheet.ColumnCount; column++) {
                int absoluteColumn = leftColumn + column;
                CellView view = sheet.GetView(new CellAddress(absoluteRow, absoluteColumn), showRawValues);
                bool isSelected = absoluteRow == selectedRow && absoluteColumn == selectedColumn;
                WriteCell(view.DisplayText, cellWidth, isSelected, view.AlignRight, view.IsError);
            }

            Console.WriteLine();
        }

        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"Celda actual: {selected}");
        Console.WriteLine($"Contenido: {sheet.GetRaw(selected)}");
        Console.WriteLine($"Valor: {DescribeValue(selectedValue)}");
        Console.WriteLine($"Estado: {status}");
    }

    private static void EditCell(Spreadsheet sheet, CellAddress address, ref string status) {
        string current = sheet.GetRaw(address);
        string? input = Prompt($"Nuevo contenido para {address} (actual: {current})");
        if (input is null) {
            status = "Edicion cancelada.";
            return;
        }

        sheet.SetRaw(address, input);
        status = $"Actualizada {address}.";
    }

    private static void JumpToCell(Spreadsheet sheet, ref int selectedRow, ref int selectedColumn, ref string status) {
        string? input = Prompt("Ir a celda");
        if (input is null) {
            status = "Salto cancelado.";
            return;
        }

        if (!CellAddress.TryParse(input, out CellAddress address)) {
            status = "Direccion invalida.";
            return;
        }

        if (address.Row >= sheet.RowCount || address.Column >= sheet.ColumnCount) {
            sheet.Resize(Math.Max(sheet.RowCount, address.Row + 1), Math.Max(sheet.ColumnCount, address.Column + 1));
        }

        selectedRow = address.Row;
        selectedColumn = address.Column;
        status = $"Ahora estas en {address}.";
    }

    private static void OpenSheet(Spreadsheet sheet, ref string? currentFile, ref string status) {
        string? input = Prompt("Ruta de texto para abrir");
        if (string.IsNullOrWhiteSpace(input)) {
            status = "Apertura cancelada.";
            return;
        }

        if (!File.Exists(input)) {
            status = $"No existe {input}.";
            return;
        }

        string text = File.ReadAllText(input);
        sheet.LoadText(text);
        currentFile = input;
        status = $"Archivo cargado: {input}.";
    }

    private static void SaveSheet(Spreadsheet sheet, ref string? currentFile, ref string status) {
        if (string.IsNullOrWhiteSpace(currentFile)) {
            currentFile = Prompt("Ruta de texto para guardar");
            if (string.IsNullOrWhiteSpace(currentFile)) {
                status = "Guardado cancelado.";
                currentFile = null;
                return;
            }
        }

        File.WriteAllText(currentFile, sheet.ToText());
        status = $"Guardado en {currentFile}.";
    }

    private static void ResizeSheet(Spreadsheet sheet, ref string status, ref int selectedRow, ref int selectedColumn) {
        string? input = Prompt("Nuevo tamano filas,columnas");
        if (input is null) {
            status = "Cambio de tamano cancelado.";
            return;
        }

        string[] parts = input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out int rows) || !int.TryParse(parts[1], out int columns)) {
            status = "Formato invalido. Usa filas,columnas";
            return;
        }

        sheet.Resize(rows, columns);
        selectedRow = Math.Min(selectedRow, sheet.RowCount - 1);
        selectedColumn = Math.Min(selectedColumn, sheet.ColumnCount - 1);
        status = $"Planilla redimensionada a {sheet.RowCount}x{sheet.ColumnCount}.";
    }

    private static void ShowHelp() {
        Console.Clear();
        Console.WriteLine("VisiCalc - ayuda");
        Console.WriteLine();
        Console.WriteLine("Edicion");
        Console.WriteLine("  Enter/F2/E    editar la celda actual");
        Console.WriteLine("  Delete        borrar la celda actual");
        Console.WriteLine("  T             alternar entre formulas y resultados");
        Console.WriteLine();
        Console.WriteLine("Navegacion");
        Console.WriteLine("  Flechas       mover seleccion");
        Console.WriteLine("  PageUp/Down   saltar 10 filas");
        Console.WriteLine("  Home          volver a A1");
        Console.WriteLine("  G             ir a una celda");
        Console.WriteLine();
        Console.WriteLine("Archivos");
        Console.WriteLine("  O             abrir archivo de texto (<direccion> : <texto>)");
        Console.WriteLine("  S             guardar archivo de texto");
        Console.WriteLine();
        Console.WriteLine("Planilla");
        Console.WriteLine("  R             cambiar tamano filas,columnas");
        Console.WriteLine("  N             cargar una demo con formulas");
        Console.WriteLine();
        Console.WriteLine("Formulas");
        Console.WriteLine("  Usa =A1+B2, =(A1+B1)/2, =SUM(A1:B3), =AVG(A1:A10), =MIN(...), =MAX(...), =COUNT(...)");
        Console.WriteLine();
        Console.WriteLine("Presiona cualquier tecla para volver.");
        Console.ReadKey(intercept: true);
    }

    private static Spreadsheet CreateDemoSheet() {
        Spreadsheet sheet = new(12, 8);
        sheet.SetRaw(CellAddress.Parse("A1"), "Ventas");
        sheet.SetRaw(CellAddress.Parse("B1"), "Q1");
        sheet.SetRaw(CellAddress.Parse("C1"), "Q2");
        sheet.SetRaw(CellAddress.Parse("D1"), "Q3");
        sheet.SetRaw(CellAddress.Parse("E1"), "Q4");
        sheet.SetRaw(CellAddress.Parse("A2"), "Norte");
        sheet.SetRaw(CellAddress.Parse("A3"), "Centro");
        sheet.SetRaw(CellAddress.Parse("A4"), "Sur");
        sheet.SetRaw(CellAddress.Parse("B2"), "120");
        sheet.SetRaw(CellAddress.Parse("C2"), "130");
        sheet.SetRaw(CellAddress.Parse("D2"), "125");
        sheet.SetRaw(CellAddress.Parse("E2"), "150");
        sheet.SetRaw(CellAddress.Parse("B3"), "80");
        sheet.SetRaw(CellAddress.Parse("C3"), "95");
        sheet.SetRaw(CellAddress.Parse("D3"), "105");
        sheet.SetRaw(CellAddress.Parse("E3"), "115");
        sheet.SetRaw(CellAddress.Parse("B4"), "60");
        sheet.SetRaw(CellAddress.Parse("C4"), "70");
        sheet.SetRaw(CellAddress.Parse("D4"), "90");
        sheet.SetRaw(CellAddress.Parse("E4"), "110");
        sheet.SetRaw(CellAddress.Parse("F1"), "Total");
        sheet.SetRaw(CellAddress.Parse("F2"), "=SUM(B2:E2)");
        sheet.SetRaw(CellAddress.Parse("F3"), "=SUM(B3:E3)");
        sheet.SetRaw(CellAddress.Parse("F4"), "=SUM(B4:E4)");
        sheet.SetRaw(CellAddress.Parse("A6"), "Promedio anual");
        sheet.SetRaw(CellAddress.Parse("B6"), "=AVG(F2:F4)");
        return sheet;
    }

    private static string DescribeValue(CellValue value) => value.Kind switch {
        CellValueKind.Empty => "(vacia)",
        CellValueKind.Number => value.Number.ToString("0.########"),
        CellValueKind.Text => value.Text,
        CellValueKind.Error => $"ERROR: {value.Text}",
        _ => "?"
    };

    private static void KeepSelectionInBounds(Spreadsheet sheet, ref int selectedRow, ref int selectedColumn) {
        selectedRow = Math.Clamp(selectedRow, 0, sheet.RowCount - 1);
        selectedColumn = Math.Clamp(selectedColumn, 0, sheet.ColumnCount - 1);
    }

    private static void KeepViewportVisible(
        Spreadsheet sheet,
        int selectedRow,
        int selectedColumn,
        ref int topRow,
        ref int leftColumn) {
        int visibleColumns = Math.Max(1, (SafeGetWindowWidth() - 6) / 13);
        int visibleRows = Math.Max(4, SafeGetWindowHeight() - 10);

        if (selectedRow < topRow) {
            topRow = selectedRow;
        }

        if (selectedRow >= topRow + visibleRows) {
            topRow = selectedRow - visibleRows + 1;
        }

        if (selectedColumn < leftColumn) {
            leftColumn = selectedColumn;
        }

        if (selectedColumn >= leftColumn + visibleColumns) {
            leftColumn = selectedColumn - visibleColumns + 1;
        }

        topRow = Math.Clamp(topRow, 0, Math.Max(0, sheet.RowCount - visibleRows));
        leftColumn = Math.Clamp(leftColumn, 0, Math.Max(0, sheet.ColumnCount - visibleColumns));
    }

    private static void WriteCell(string text, int width, bool selected, bool alignRight, bool isError) {
        string clipped = text ?? string.Empty;
        if (clipped.Length > width) {
            clipped = clipped[..width];
        }

        string padded = alignRight ? clipped.PadLeft(width) : clipped.PadRight(width);

        ConsoleColor previousForeground = Console.ForegroundColor;
        ConsoleColor previousBackground = Console.BackgroundColor;

        if (selected) {
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.Black;
        } else if (isError) {
            Console.ForegroundColor = ConsoleColor.Red;
        }

        Console.Write(padded);
        Console.BackgroundColor = previousBackground;
        Console.ForegroundColor = previousForeground;
        Console.Write(' ');
    }

    private static string? Prompt(string label) {
        Console.WriteLine();
        Console.Write($"{label}: ");
        Console.CursorVisible = true;
        string? input = Console.ReadLine();
        Console.CursorVisible = false;
        return input;
    }

    private static int SafeGetWindowWidth() {
        try {
            return Console.WindowWidth;
        } catch {
            return 120;
        }
    }

    private static int SafeGetWindowHeight() {
        try {
            return Console.WindowHeight;
        } catch {
            return 30;
        }
    }

    private static void PrintUsage() {
        Console.WriteLine("visicalc [archivo.txt]");
        Console.WriteLine("visicalc --demo");
        Console.WriteLine("visicalc --self-test");
    }
}
