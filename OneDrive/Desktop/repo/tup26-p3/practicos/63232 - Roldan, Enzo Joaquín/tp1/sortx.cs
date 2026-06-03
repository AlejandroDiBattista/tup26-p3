// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

try {
    var config = ParseArgs(args);
    var text   = ReadInput(config);
    var rows   = ParseDelimited(text, config);
    var sorted = SortRows(rows, config);
    var output = Serialize(sorted, config);
    WriteOutput(output, config);
} catch (Exception ex) {
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args) {
    string? inputFile  = null;
    string? outputFile = null;
    string  delimiter  = ",";
    bool    noHeader   = false;
    var     sortFields = new List<SortField>();
    int     positional = 0;

    for (int i = 0; i < args.Length; i++) {
        switch (args[i]) {
            case "-h" or "--help":
                Console.WriteLine("""
                    Uso: sortx [input [output]] [-b campo[:tipo[:orden]]]...
                         [-i input] [-o output] [-d delimitador] [-nh] [-h]

                    Opciones:
                      -b, --by        Campo de ordenamiento (repetible)
                      -i, --input     Archivo de entrada
                      -o, --output    Archivo de salida
                      -d, --delimiter Delimitador (default: ,)
                      -nh, --no-header Sin encabezado
                      -h, --help      Muestra esta ayuda

                    Formato de campo: nombre[:alpha|num[:asc|desc]]
                    """);
                Environment.Exit(0);
                break;
            case "-b" or "--by":
                sortFields.Add(ParseSortField(args[++i]));
                break;
            case "-i" or "--input":
                inputFile = args[++i];
                break;
            case "-o" or "--output":
                outputFile = args[++i];
                break;
            case "-d" or "--delimiter":
                var d = args[++i];
                delimiter = d == @"\t" ? "\t" : d;
                break;
            case "-nh" or "--no-header":
                noHeader = true;
                break;
            default:
                if (positional == 0) { inputFile  = args[i]; positional++; }
                else                 { outputFile = args[i]; positional++; }
                break;
        }
    }

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

SortField ParseSortField(string spec) {
    var parts      = spec.Split(':');
    var name       = parts[0];
    var numeric    = parts.Length > 1 && parts[1] == "num";
    var descending = parts.Length > 2 && parts[2] == "desc";
    return new SortField(name, numeric, descending);
}

string ReadInput(AppConfig config) =>
    config.InputFile is not null
        ? File.ReadAllText(config.InputFile)
        : new System.IO.StreamReader(Console.OpenStandardInput()).ReadToEnd();

List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config) {
    var lines = text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
    if (lines.Length == 0) return [];

    string[] headers;
    IEnumerable<string> dataLines;

    if (config.NoHeader) {
        headers   = Enumerable.Range(0, lines[0].Split(config.Delimiter).Length)
                              .Select(i => i.ToString()).ToArray();
        dataLines = lines;
    } else {
        headers   = lines[0].Split(config.Delimiter);
        dataLines = lines.Skip(1);
    }

    return dataLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Select(line => {
            var cols = line.Split(config.Delimiter);
            return headers.Select((h, i) => (h, v: i < cols.Length ? cols[i] : ""))
                          .ToDictionary(x => x.h, x => x.v);
        }).ToList();
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config) {
    if (config.SortFields.Count == 0 || rows.Count == 0) return rows;

    foreach (var field in config.SortFields)
        if (!rows[0].ContainsKey(field.Name))
            throw new Exception($"Campo '{field.Name}' no existe.");

    IOrderedEnumerable<Dictionary<string, string>> ordered;
    var first = config.SortFields[0];

    ordered = first.Descending
        ? (first.Numeric ? rows.OrderByDescending(r => double.Parse(r[first.Name]))
                         : rows.OrderByDescending(r => r[first.Name]))
        : (first.Numeric ? rows.OrderBy(r => double.Parse(r[first.Name]))
                         : rows.OrderBy(r => r[first.Name]));

    foreach (var field in config.SortFields.Skip(1)) {
        ordered = field.Descending
            ? (field.Numeric ? ordered.ThenByDescending(r => double.Parse(r[field.Name]))
                             : ordered.ThenByDescending(r => r[field.Name]))
            : (field.Numeric ? ordered.ThenBy(r => double.Parse(r[field.Name]))
                             : ordered.ThenBy(r => r[field.Name]));
    }

    return ordered.ToList();
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config) {
    if (rows.Count == 0) return "";
    var headers = rows[0].Keys.ToArray();
    var lines   = new List<string>();

    if (!config.NoHeader)
        lines.Add(string.Join(config.Delimiter, headers));

    lines.AddRange(rows.Select(r => string.Join(config.Delimiter, headers.Select(h => r[h]))));
    return string.Join("\n", lines) + "\n";
}

void WriteOutput(string text, AppConfig config) {
    if (config.OutputFile is not null)
        File.WriteAllText(config.OutputFile, text);
    else
        Console.Write(text);
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields
);
