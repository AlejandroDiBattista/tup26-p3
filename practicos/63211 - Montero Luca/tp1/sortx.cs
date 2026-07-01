// sortx.cs — Herramienta CLI para ordenar archivos delimitados
// Uso: dotnet run sortx.cs -- [args]

// ─── Modelos de configuración ────────────────────────────────────────────────

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);

// ─── Pipeline principal ───────────────────────────────────────────────────────

try {
    var config = ParseArgs(args);
    var raw = ReadInput(config);
    var rows = ParseDelimited(raw, config);
    var sorted = SortRows(rows, config);
    var output = Serialize(sorted, config);
    WriteOutput(output, config);
} catch (Exception ex) {
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

// ─── 1. ParseArgs ─────────────────────────────────────────────────────────────

AppConfig ParseArgs(string[] args) {
    if (args.Length == 0 || args.Contains("-h") || args.Contains("--help")) {
        PrintHelp();
        Environment.Exit(0);
    }

    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    List<SortField> sortFields = new();
    int positional = 0;

    for (int i = 0; i < args.Length; i++) {
        switch (args[i]) {
            case "-b" or "--by":
                sortFields.Add(ParseSortField(Next()));
                break;

            case "-i" or "--input":
                inputFile = Next();
                break;

            case "-o" or "--output":
                outputFile = Next();
                break;

            case "-d" or "--delimiter":
                delimiter = Next().Replace("\\t", "\t");
                break;

            case "-nh" or "--no-header":
                noHeader = true;
                break;

            default:
                if (args[i].StartsWith('-'))
                    throw new ArgumentException($"Opción desconocida: {args[i]}");

                if (positional == 0) { inputFile = args[i]; positional++; } else if (positional == 1) { outputFile = args[i]; positional++; } else throw new ArgumentException("Demasiados argumentos posicionales.");
                break;
        }

        string Next() {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"Se esperaba un valor después de '{args[i]}'.");
            return args[++i];
        }
    }

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

SortField ParseSortField(string spec) {
    var parts = spec.Split(':');
    var name = parts[0];
    var numeric = parts.Length > 1 && parts[1].Equals("num", StringComparison.OrdinalIgnoreCase);
    var descending = parts.Length > 2 && parts[2].Equals("desc", StringComparison.OrdinalIgnoreCase);
    return new SortField(name, numeric, descending);
}

void PrintHelp() {
    Console.WriteLine("""
        sortx — Ordena archivos de texto delimitados (CSV, TSV, PSV, etc.)

        USO:
          sortx [input [output]] [-b campo[:tipo[:orden]]]... [opciones]

        OPCIONES:
          -b, --by campo[:tipo[:orden]]   Campo por el que ordenar (repetible)
          -i, --input  <archivo>          Archivo de entrada (default: stdin)
          -o, --output <archivo>          Archivo de salida  (default: stdout)
          -d, --delimiter <car>           Delimitador (default: ,). Usar \t para tab
          -nh, --no-header                Sin encabezado; campos por índice numérico
          -h, --help                      Muestra esta ayuda y termina

        TIPO:   alpha (default) | num
        ORDEN:  asc  (default)  | desc

        EJEMPLOS:
          sortx empleados.csv -b apellido
          sortx empleados.csv -b salario:num:desc
          sortx empleados.csv -b departamento -b salario:num:desc -o resultado.csv
          cat empleados.csv | sortx -b apellido > ordenado.csv
        """);
}

// ─── 2. ReadInput ─────────────────────────────────────────────────────────────

string ReadInput(AppConfig config) {
    if (config.InputFile is null)
        return Console.In.ReadToEnd();

    if (!File.Exists(config.InputFile))
        throw new FileNotFoundException($"Archivo no encontrado: {config.InputFile}");

    return File.ReadAllText(config.InputFile);
}

// ─── 3. ParseDelimited ───────────────────────────────────────────────────────

(string[]? Header, List<Dictionary<string, string>> Rows) ParseDelimited(string text, AppConfig config) {
    var lines = text.ReplaceLineEndings("\n")
                    .Split('\n')
                    .Where(l => l.Trim().Length > 0)
                    .ToList();

    if (lines.Count == 0)
        return (null, new());

    string[]? header = null;

    if (!config.NoHeader) {
        header = lines[0].Split(config.Delimiter);
        lines = lines.Skip(1).ToList();
    }

    var rows = lines.Select(line => {
        var fields = line.Split(config.Delimiter);
        var dict = new Dictionary<string, string>();

        if (header is not null) {
            for (int i = 0; i < header.Length; i++)
                dict[header[i]] = i < fields.Length ? fields[i] : "";
        } else {
            for (int i = 0; i < fields.Length; i++)
                dict[i.ToString()] = fields[i];
        }

        return dict;
    }).ToList();

    return (header, rows);
}

// ─── 4. SortRows ─────────────────────────────────────────────────────────────

(string[]? Header, List<Dictionary<string, string>> Rows) SortRows(
    (string[]? Header, List<Dictionary<string, string>> Rows) data,
    AppConfig config) {
    if (config.SortFields.Count == 0)
        return data;

    // Validar que todos los campos existan
    if (data.Rows.Count > 0) {
        foreach (var sf in config.SortFields) {
            if (!data.Rows[0].ContainsKey(sf.Name))
                throw new ArgumentException($"Campo no encontrado: '{sf.Name}'");
        }
    }

    IOrderedEnumerable<Dictionary<string, string>> ordered;

    var first = config.SortFields[0];
    ordered = first.Descending
        ? data.Rows.OrderByDescending(r => GetKey(r, first))
        : data.Rows.OrderBy(r => GetKey(r, first));

    foreach (var sf in config.SortFields.Skip(1)) {
        var capture = sf;
        ordered = capture.Descending
            ? ordered.ThenByDescending(r => GetKey(r, capture))
            : ordered.ThenBy(r => GetKey(r, capture));
    }

    return (data.Header, ordered.ToList());

    IComparable GetKey(Dictionary<string, string> row, SortField sf) {
        var val = row.TryGetValue(sf.Name, out var v) ? v : "";
        if (sf.Numeric)
            return double.TryParse(val, out var n) ? n : double.MinValue;
        return val;
    }
}

// ─── 5. Serialize ─────────────────────────────────────────────────────────────

string Serialize(
    (string[]? Header, List<Dictionary<string, string>> Rows) data,
    AppConfig config) {
    var sb = new System.Text.StringBuilder();

    if (data.Header is not null)
        sb.AppendLine(string.Join(config.Delimiter, data.Header));

    foreach (var row in data.Rows) {
        IEnumerable<string> keys = data.Header is not null
            ? (IEnumerable<string>)data.Header
            : row.Keys;

        sb.AppendLine(string.Join(config.Delimiter, keys.Select(k => row.TryGetValue(k, out var v) ? v : "")));
    }

    return sb.ToString();
}

// ─── 6. WriteOutput ───────────────────────────────────────────────────────────

void WriteOutput(string text, AppConfig config) {
    if (config.OutputFile is null) {
        Console.Write(text);
        return;
    }

    File.WriteAllText(config.OutputFile, text);
}
