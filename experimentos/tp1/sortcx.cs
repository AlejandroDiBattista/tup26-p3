using System.Globalization;

try {
    var config = ParseArgs(args);
    var table = ParseDelimited(config, ReadInput(config));
    var sorted = SortRows(config, table);
    var output = Serialize(config, sorted);
    WriteOutput(config, output);
} catch (Exception ex) {
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

AppConfig ParseArgs(string[] args) {
    string? inputFile = null;
    string? outputFile = null;
    var delimiter = ",";
    var noHeader = false;
    var sortFields = new List<SortField>();
    var positional = new List<string>();

    for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
            case "-h":
            case "--help":
                Console.WriteLine(HelpText());
                Environment.Exit(0);
                return default!;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-i":
            case "--input":
                if (inputFile is not null) throw new InvalidOperationException("El archivo de entrada se especifico mas de una vez.");
                inputFile = RequireValue(args, ref i);
                break;

            case "-o":
            case "--output":
                if (outputFile is not null) throw new InvalidOperationException("El archivo de salida se especifico mas de una vez.");
                outputFile = RequireValue(args, ref i);
                break;

            case "-d":
            case "--delimiter":
                delimiter = ParseDelimiter(RequireValue(args, ref i));
                break;

            case "-b":
            case "--by":
                sortFields.Add(ParseSortField(RequireValue(args, ref i)));
                break;

            default:
                if (args[i].StartsWith("-", StringComparison.Ordinal)) {
                    throw new InvalidOperationException($"Opcion desconocida: {args[i]}");
                }
                positional.Add(args[i]);
                break;
        }
    }

    if (positional.Count > 2) {
        throw new InvalidOperationException("Solo se permiten dos argumentos posicionales: input y output.");
    }

    if (positional.Count > 0) {
        if (inputFile is not null) throw new InvalidOperationException("El archivo de entrada se especifico tanto por posicion como por opcion.");
        inputFile = positional[0];
    }

    if (positional.Count > 1) {
        if (outputFile is not null) throw new InvalidOperationException("El archivo de salida se especifico tanto por posicion como por opcion.");
        outputFile = positional[1];
    }

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

string ReadInput(AppConfig config) {
    if (config.InputFile is null) return Console.In.ReadToEnd();
    if (!File.Exists(config.InputFile)) throw new FileNotFoundException($"No existe el archivo de entrada '{config.InputFile}'.");
    return File.ReadAllText(config.InputFile);
}

(List<string> Columns, List<Dictionary<string, string>> Rows) ParseDelimited(AppConfig config, string text) {
    var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace('\r', '\n')
                    .Split('\n')
                    .Where(line => line.Length > 0)
                    .ToList();

    if (lines.Count == 0) return ([], []);

    var rawRows = (config.NoHeader ? lines : lines.Skip(1))
        .Select(line => Split(line, config.Delimiter))
        .ToList();

    var columns = config.NoHeader
        ? Enumerable.Range(0, rawRows.DefaultIfEmpty([]).Max(parts => parts.Length)).Select(i => i.ToString()).ToList()
        : Split(lines[0], config.Delimiter).ToList();

    if (!config.NoHeader && columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Count) {
        throw new InvalidOperationException("El encabezado contiene columnas repetidas.");
    }

    var rows = rawRows.Select(parts => ToRow(parts, columns, config.NoHeader)).ToList();
    return (columns, rows);
}

(List<string> Columns, List<Dictionary<string, string>> Rows) SortRows(
    AppConfig config,
    (List<string> Columns, List<Dictionary<string, string>> Rows) table) {
    var fields = ResolveSortFields(config, table.Columns);
    table.Rows.Sort((a, b) => CompareRows(a, b, fields));
    return table;
}

string Serialize(AppConfig config, (List<string> Columns, List<Dictionary<string, string>> Rows) table) {
    var lines = table.Rows
        .Select(row => string.Join(config.Delimiter, table.Columns.Select(column => Cell(row, column))))
        .ToList();

    if (!config.NoHeader && table.Columns.Count > 0) {
        lines.Insert(0, string.Join(config.Delimiter, table.Columns));
    }

    return string.Join(Environment.NewLine, lines);
}

void WriteOutput(AppConfig config, string text) {
    if (config.OutputFile is null) {
        Console.Write(text);
        return;
    }

    File.WriteAllText(config.OutputFile, text);
}

string RequireValue(string[] args, ref int i) {
    if (i + 1 >= args.Length) throw new InvalidOperationException($"Falta un valor para la opcion {args[i]}.");
    return args[++i];
}

string ParseDelimiter(string value) {
    if (value.Length == 0) throw new InvalidOperationException("El delimitador no puede ser vacio.");
    return value == "\\t" ? "\t" : value;
}

SortField ParseSortField(string spec) {
    var parts = spec.Split(':');

    if (parts.Length is < 1 or > 3 || string.IsNullOrWhiteSpace(parts[0])) {
        throw new InvalidOperationException($"La especificacion de campo '{spec}' es invalida.");
    }

    var numeric = parts.Length > 1 && parts[1] switch {
        "alpha" => false,
        "num" => true,
        _ => throw new InvalidOperationException($"Tipo de ordenamiento invalido en '{spec}'.")
    };

    var descending = parts.Length > 2 && parts[2] switch {
        "asc" => false,
        "desc" => true,
        _ => throw new InvalidOperationException($"Orden invalido en '{spec}'.")
    };

    return new SortField(parts[0], numeric, descending);
}

string[] Split(string line, string delimiter) {
    return line.Split([delimiter], StringSplitOptions.None);
}

Dictionary<string, string> ToRow(string[] parts, List<string> columns, bool noHeader) {
    if (!noHeader && parts.Length > columns.Count) {
        throw new InvalidOperationException("Una fila tiene mas columnas que el encabezado.");
    }

    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < columns.Count; i++) {
        row[columns[i]] = i < parts.Length ? parts[i] : "";
    }
    return row;
}

List<SortField> ResolveSortFields(AppConfig config, List<string> columns) {
    var fields = new List<SortField>();

    foreach (var field in config.SortFields) {
        if (config.NoHeader) {
            if (!int.TryParse(field.Name, out var index) || index < 0 || index >= columns.Count) {
                throw new InvalidOperationException($"La columna '{field.Name}' no existe.");
            }
            fields.Add(field with { Name = columns[index] });
            continue;
        }

        var name = columns.FirstOrDefault(column => string.Equals(column, field.Name, StringComparison.OrdinalIgnoreCase));
        if (name is null) throw new InvalidOperationException($"La columna '{field.Name}' no existe.");
        fields.Add(field with { Name = name });
    }

    return fields;
}

int CompareRows(Dictionary<string, string> a, Dictionary<string, string> b, List<SortField> fields) {
    foreach (var field in fields) {
        var cmp = field.Numeric
            ? ParseNumber(Cell(a, field.Name), field.Name).CompareTo(ParseNumber(Cell(b, field.Name), field.Name))
            : StringComparer.OrdinalIgnoreCase.Compare(Cell(a, field.Name), Cell(b, field.Name));

        if (cmp != 0) return field.Descending ? -cmp : cmp;
    }

    return 0;
}

string Cell(Dictionary<string, string> row, string column) {
    return row.TryGetValue(column, out var value) ? value : "";
}

decimal ParseNumber(string value, string column) {
    if (decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var number)) {
        return number;
    }

    if (decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out number)) {
        return number;
    }

    throw new InvalidOperationException($"No se pudo interpretar '{value}' como numero en la columna '{column}'.");
}

string HelpText() {
    return """
sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
      [-i|--input input] [-o|--output output]
      [-d|--delimiter delimitador]
      [-nh|--no-header] [-h|--help]

Opciones:
  -b,  --by           Campo por el que ordenar. Puede repetirse.
  -i,  --input        Archivo de entrada. Si falta, usa stdin.
  -o,  --output       Archivo de salida. Si falta, usa stdout.
  -d,  --delimiter    Delimitador. Default: ','; usar \\t para tab.
  -nh, --no-header    Trata la primera fila como dato.
  -h,  --help         Muestra esta ayuda.

Especificacion de campo: campo[:tipo[:orden]]
  tipo  = alpha | num
  orden = asc   | desc
""";
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields);
