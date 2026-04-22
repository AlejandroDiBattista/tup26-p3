try
{
    AppConfig config                        = ParseArgs(args);                                              // Paso 1
    string rawText                          = ReadInput(config.InputFile);                                  // Paso 2
    var (headers, rows)                     = ParseDelimited(rawText, config.Delimiter, config.NoHeader);   // Paso 3
    List<Dictionary<string, string>> sorted = SortRows(rows, headers, config.SortFields);                  // Paso 4
    string output                           = Serialize(sorted, headers, config.Delimiter, config.NoHeader);// Paso 5
    WriteOutput(config.OutputFile, output);                                                                 // Paso 6
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine("Use --help para ver las opciones.");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string?         inputFile  = null;
    string?         outputFile = null;
    string          delimiter  = ",";
    bool            noHeader   = false;
    var             sortFields = new List<SortField>();
    var             positional = new List<string>();
    int i = 0;

    while (i < args.Length)
    {
        string arg = args[i];
        switch (arg)
        {
            case "--help" or "-h":
                PrintHelp();
                Environment.Exit(0);
                break;

            case "--input" or "-i":
                inputFile = Next(args, ref i, arg);
                break;

            case "--output" or "-o":
                outputFile = Next(args, ref i, arg);
                break;

            case "--delimiter" or "-d":
                delimiter = ProcessDelimiter(Next(args, ref i, arg));
                break;

            case "--no-header" or "-nh":
                noHeader = true;
                i++;
                break;

            case "--by" or "-b":
                sortFields.Add(ParseSortField(Next(args, ref i, arg)));
                break;

            default:
                if (arg.StartsWith('-'))
                    throw new Exception($"Opción desconocida: '{arg}'. Use --help.");
                positional.Add(arg);
                i++;
                break;
        }
    }

    if (positional.Count >= 1 && inputFile  == null) inputFile  = positional[0];
    if (positional.Count >= 2 && outputFile == null) outputFile = positional[1];
    if (positional.Count > 2)
        throw new Exception("Demasiados argumentos posicionales. Máximo: [input] [output].");

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

string Next(string[] args, ref int i, string option)
{
    i++;
    if (i >= args.Length)
        throw new Exception($"'{option}' requiere un valor.");
    return args[i++];
}

string ProcessDelimiter(string d) => d switch
{
    "\\t" => "\t",
    "\\n" => "\n",
    "\\|" => "|",
    _     => d
};

SortField ParseSortField(string spec)
{
    string[] parts = spec.Split(':');
    string name    = parts[0].Trim();

    if (string.IsNullOrWhiteSpace(name))
        throw new Exception($"Especificación inválida: '{spec}'. El nombre/índice no puede estar vacío.");

    if (parts.Length >= 2)
    {
        string tipo = parts[1].Trim().ToLower();
        if (tipo != "alpha" && tipo != "num")
            throw new Exception($"Tipo inválido en '{spec}'. Use 'alpha' o 'num'.");
    }

    if (parts.Length >= 3)
    {
        string orden = parts[2].Trim().ToLower();
        if (orden != "asc" && orden != "desc")
            throw new Exception($"Orden inválido en '{spec}'. Use 'asc' o 'desc'.");
    }

    bool numeric    = parts.Length >= 2 && parts[1].Trim().Equals("num",  StringComparison.OrdinalIgnoreCase);
    bool descending = parts.Length >= 3 && parts[2].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

    return new SortField(name, numeric, descending);
}

string ReadInput(string? filePath)
{
    if (filePath == null)
        return Console.In.ReadToEnd();

    if (!File.Exists(filePath))
        throw new FileNotFoundException($"Archivo no encontrado: '{filePath}'");

    return File.ReadAllText(filePath);
}

(string[] headers, List<Dictionary<string, string>> rows)
    ParseDelimited(string content, string delimiter, bool noHeader)
{
    string[] lines = content.Split('\n');

    int last = lines.Length - 1;
    while (last >= 0 && lines[last].Trim() == "") last--;

    if (last < 0) return ([], []);

    string[] headers;
    int dataStart;

    if (noHeader)
    {
        int colCount = lines[0].TrimEnd('\r').Split(delimiter).Length;
        headers   = Enumerable.Range(0, colCount).Select(n => n.ToString()).ToArray();
        dataStart = 0;
    }
    else
    {
        headers   = lines[0].TrimEnd('\r').Split(delimiter).Select(h => h.Trim()).ToArray();
        dataStart = 1;
    }

    var rows = new List<Dictionary<string, string>>();

    for (int i = dataStart; i <= last; i++)
    {
        string line = lines[i].Trim();
        if (line == "") continue;

        string[] values = line.Split(delimiter);
        var row = new Dictionary<string, string>();

        for (int col = 0; col < headers.Length; col++)
            row[headers[col]] = col < values.Length ? values[col].Trim() : "";

        rows.Add(row);
    }

    return (headers, rows);
}

List<Dictionary<string, string>> SortRows(
    List<Dictionary<string, string>> rows,
    string[]                         headers,
    List<SortField>                  sortFields)
{
    if (sortFields.Count == 0 || rows.Count == 0)
        return rows;

    foreach (var field in sortFields)
    {
        if (!headers.Contains(field.Name))
            throw new Exception($"Columna no encontrada: '{field.Name}'.");
    }

    int CompareRows(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        foreach (var field in sortFields)
        {
            string valA = a.GetValueOrDefault(field.Name, "");
            string valB = b.GetValueOrDefault(field.Name, "");

            int cmp;
            if (field.Numeric)
            {
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                var style   = System.Globalization.NumberStyles.Any;
                double numA = double.TryParse(valA, style, culture, out double na) ? na : 0;
                double numB = double.TryParse(valB, style, culture, out double nb) ? nb : 0;
                cmp = numA.CompareTo(numB);
            }
            else
            {
                cmp = string.Compare(valA, valB, StringComparison.CurrentCulture);
            }

            if (cmp != 0) return field.Descending ? -cmp : cmp;
        }
        return 0;
    }

    var sorted = new List<Dictionary<string, string>>(rows);
    sorted.Sort(CompareRows);
    return sorted;
}

string Serialize(
    List<Dictionary<string, string>> rows,
    string[]                         headers,
    string                           delimiter,
    bool                             noHeader)
{
    var sb = new System.Text.StringBuilder();

    if (!noHeader)
        sb.Append(string.Join(delimiter, headers)).Append('\n');

    foreach (var row in rows)
    {
        IEnumerable<string> values = headers.Select(h => row.GetValueOrDefault(h, ""));
        sb.Append(string.Join(delimiter, values)).Append('\n');
    }

    return sb.ToString();
}

void WriteOutput(string? filePath, string content)
{
    if (filePath == null)
    {
        Console.Write(content);
        return;
    }
    File.WriteAllText(filePath, content);
}

void PrintHelp()
{
    Console.WriteLine("""
        sortx — ordena filas de un archivo de texto delimitado

        USO
          sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
                [-i|--input input] [-o|--output output]
                [-d|--delimiter delim] [-nh|--no-header] [-h|--help]

        OPCIONES
          -b, --by campo[:tipo[:orden]]   Campo de ordenamiento. Repetible.
                                          tipo : alpha (default) | num
                                          orden: asc  (default) | desc
          -i, --input  archivo            Archivo de entrada (o 1er posicional)
          -o, --output archivo            Archivo de salida  (o 2do posicional)
          -d, --delimiter delim           Carácter delimitador (default: ,)
                                          Use \t para tabulación, | para pipe.
          -nh, --no-header                El archivo no tiene fila de encabezado.
                                          Los campos se identifican por índice (0, 1, 2...).
          -h, --help                      Muestra esta ayuda y termina.

        EJEMPLOS
          sortx empleados.csv -b apellido
          sortx empleados.csv resultado.csv -b salario:num:desc
          sortx empleados.csv -b departamento -b salario:num:desc -o resultado.csv
          sortx -i empleados.csv -o resultado.csv -b apellido
          sortx datos.tsv -d "\t" -nh -b 1:alpha:asc
          cat empleados.csv | sortx -b apellido > ordenado.csv
          sortx --help
        """);
} 

record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(
    string?        InputFile,
    string?        OutputFile,
    string         Delimiter,
    bool           NoHeader,
    List<SortField> SortFields
);