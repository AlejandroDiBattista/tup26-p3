namespace NanoCalc;

internal static class CommandParser {
    private static readonly CommandDescriptor[] Commands =
    [
        new("Leer",     ['l'],      "leer"),
        new("Guardar",  ['g'],      "guardar"),
        new("Insertar", ['i'],      "insertar"),
        new("Eliminar", ['e'],      "eliminar"),
        new("Ordenar",  ['o'],      "ordenar"),
        new("Salir",    ['s', 'q'], "salir"),
        new("Ayuda",    ['a'],      "ayuda")
    ];

    public static SlashCommand Parse(string text, CellAddress currentAddress, SpreadsheetDocument document, EvaluationEngine engine) {
        var trimmed = text.Trim();
        if (trimmed == "/") {
            return new HelpCommand();
        }

        if (!trimmed.StartsWith('/')) {
            throw new InvalidOperationException("Los comandos deben comenzar con '/'.");
        }

        var parts = trimmed[1..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0) {
            return new HelpCommand();
        }

        var command = NormalizeCommand(parts[0]);
        return command switch {
            "leer" when parts.Length >= 2 => new ReadCommand(parts[1]),
            "guardar" when parts.Length >= 2 => new SaveCommand(parts[1]),
            "insertar" => ParseInsertOrDelete(parts.Skip(1).ToArray(), currentAddress, delete: false),
            "eliminar" => ParseInsertOrDelete(parts.Skip(1).ToArray(), currentAddress, delete: true),
            "ordenar" => ParseSort(parts.Skip(1).ToArray(), currentAddress, document, engine),
            "salir" => new ExitCommand(),
            "ayuda" => new HelpCommand(),
            _ => throw new InvalidOperationException("Comando desconocido.")
        };
    }

    public static string HelpSummary() {
        return "Comandos: " + string.Join(" | ", Commands.Select(command => "/" + command.DisplayName));
    }

    public static string FooterSummary() {
        return string.Join(" ", Commands.Select(command => "/" + command.DisplayName));
    }

    public static bool TryGetShortcut(ConsoleKeyInfo keyInfo, out KeyboardShortcut shortcut) {
        shortcut = default;
        var command = ResolveShortcutCommand(keyInfo);
        if (string.IsNullOrEmpty(command)) {
            return false;
        }

        shortcut = new KeyboardShortcut(
            command,
            RequiresArgument(command)
                ? "/" + command + " "
                : "/" + command,
            !RequiresArgument(command));
        return true;
    }

    private static string ResolveShortcutCommand(ConsoleKeyInfo keyInfo) {
        if (!keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) &&
            !(keyInfo.Key == ConsoleKey.Tab && keyInfo.Modifiers == 0)) {
            return string.Empty;
        }

        return keyInfo.Key switch {
            ConsoleKey.L => "leer",
            ConsoleKey.G => "guardar",
            ConsoleKey.I or ConsoleKey.Tab => "insertar",
            ConsoleKey.E or ConsoleKey.M => "eliminar",
            ConsoleKey.O => "ordenar",
            ConsoleKey.S or ConsoleKey.Q => "salir",
            ConsoleKey.A => "ayuda",
            _ => string.Empty
        };
    }

    public static bool TryComplete(string text, int cursor, out string completedText, out int completedCursor) {
        completedText = text;
        completedCursor = cursor;

        if (cursor < 0 || cursor > text.Length || text.Length == 0 || text[0] != '/') {
            return false;
        }

        var tokenStart = 1;
        var tokenEnd = text.IndexOf(' ');
        if (tokenEnd < 0) {
            tokenEnd = text.Length;
        }

        if (cursor < tokenStart || cursor > tokenEnd) {
            return false;
        }

        var typed = text[tokenStart..cursor];
        var suffix = text[cursor..];
        var matches = Commands
            .Where(command => MatchesCommand(command, typed))
            .Select(command => command.CanonicalName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matches.Count != 1) {
            return false;
        }

        var canonical = matches[0];
        var replacement = RequiresArgument(canonical)
            ? "/" + canonical + " "
            : "/" + canonical;

        completedText = replacement + suffix.TrimStart();
        completedCursor = replacement.Length;
        return true;
    }

    private static bool RequiresArgument(string canonicalName) {
        return canonicalName is "leer" or "guardar";
    }

    private static bool MatchesCommand(CommandDescriptor descriptor, string typed) {
        if (typed.Length == 0) {
            return true;
        }

        if (descriptor.CanonicalName.StartsWith(typed, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return descriptor.Shortcuts.Any(shortcut => string.Equals(shortcut.ToString(), typed, StringComparison.OrdinalIgnoreCase));
    }

    private static SlashCommand ParseInsertOrDelete(string[] args, CellAddress currentAddress, bool delete) {
        if (args.Length == 0) {
            return delete
                ? new DeleteRowCommand(currentAddress.Row)
                : new InsertRowCommand(currentAddress.Row);
        }

        var target = args[0].ToLowerInvariant();
        if (target is "fila" or "filas") {
            var row = args.Length >= 2 && int.TryParse(args[1], out var rowNumber)
                ? Math.Clamp(rowNumber - 1, 0, CellAddress.MaxRows - 1)
                : currentAddress.Row;
            return delete ? new DeleteRowCommand(row) : new InsertRowCommand(row);
        }

        if (target is "columna" or "columnas") {
            var column = currentAddress.Column;
            if (args.Length >= 2) {
                column = ParseColumn(args[1]);
            }

            return delete ? new DeleteColumnCommand(column) : new InsertColumnCommand(column);
        }

        throw new InvalidOperationException("Use fila o columna.");
    }

    private static SlashCommand ParseSort(string[] args, CellAddress currentAddress, SpreadsheetDocument document, EvaluationEngine engine) {
        if (args.Length == 0) {
            return new SortCommand([currentAddress.Column]);
        }

        var columns = new List<int>();
        foreach (var token in args.SelectMany(arg => arg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))) {
            columns.Add(ParseColumnOrHeader(token, document, engine));
        }

        return new SortCommand(columns);
    }

    private static int ParseColumn(string token) {
        var value = token.Trim().ToUpperInvariant();
        if (value.Length == 1 && value[0] is >= 'A' and <= 'Z') {
            return value[0] - 'A';
        }

        throw new InvalidOperationException("Columna invalida.");
    }

    private static int ParseColumnOrHeader(string token, SpreadsheetDocument document, EvaluationEngine engine) {
        try {
            return ParseColumn(token);
        }
        catch (InvalidOperationException) {
            for (var column = 0; column < CellAddress.MaxColumns; column++) {
                var header = document.GetDisplayValue(new CellAddress(0, column), engine).ToText();
                if (string.Equals(header, token, StringComparison.CurrentCultureIgnoreCase)) {
                    return column;
                }
            }

            throw new InvalidOperationException($"No se encontro la columna '{token}'.");
        }
    }

    private static string NormalizeCommand(string command) {
        var lowered = command.ToLowerInvariant();
        foreach (var descriptor in Commands) {
            if (string.Equals(descriptor.CanonicalName, lowered, StringComparison.OrdinalIgnoreCase)) {
                return descriptor.CanonicalName;
            }

            if (descriptor.Shortcuts.Any(shortcut => shortcut.ToString() == lowered)) {
                return descriptor.CanonicalName;
            }
        }

        return lowered;
    }
}

internal abstract record SlashCommand;

internal sealed record HelpCommand() : SlashCommand;

internal sealed record ReadCommand(string FileName) : SlashCommand;

internal sealed record SaveCommand(string FileName) : SlashCommand;

internal sealed record InsertRowCommand(int RowIndex) : SlashCommand;

internal sealed record DeleteRowCommand(int RowIndex) : SlashCommand;

internal sealed record InsertColumnCommand(int ColumnIndex) : SlashCommand;

internal sealed record DeleteColumnCommand(int ColumnIndex) : SlashCommand;

internal sealed record SortCommand(IReadOnlyList<int> Columns) : SlashCommand;

internal sealed record ExitCommand() : SlashCommand;

internal sealed record CommandDescriptor(string DisplayName, IReadOnlyList<char> Shortcuts, string CanonicalName);

internal readonly record struct KeyboardShortcut(string CanonicalName, string CommandLine, bool ExecuteDirectly);
