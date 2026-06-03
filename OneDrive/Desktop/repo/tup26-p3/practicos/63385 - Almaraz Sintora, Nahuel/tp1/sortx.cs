// sortx.cs — Herramienta CLI para ordenar archivos delimitados
// Uso: dotnet run sortx.cs -- [input [output]] [-b campo[:tipo[:orden]]]... [opciones]

try
{
    AppConfig ParseArgs(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string delimiter = ",";
        bool noHeader = false;
        bool showHelp = false;
        List<SortField> sortFields = new();
        int positional = 0;

        SortField ParseSortField(string spec)
        {
            var parts = spec.Split(':');
            string name = parts[0];
            bool numeric = parts.Length > 1 && parts[1].Equals("num", StringComparison.OrdinalIgnoreCase);
            bool descending = parts.Length > 2 && parts[2].Equals("desc", StringComparison.OrdinalIgnoreCase);
            return new SortField(name, numeric, descending);
        }

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "--ayuda" || arg == "-ay")
            {
                showHelp = true;
                continue;
            }
            if (arg == "--no-header" || arg == "-nh")
            {
                noHeader = true;
                continue;
            }
            if (arg == "--by" || arg == "-b")
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"La opción '{arg}' requiere un valor.");
                sortFields.Add(ParseSortField(args[++i]));
                continue;
            }
            if (arg == "--input" || arg == "-i")
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"La opción '{arg}' requiere un valor.");
                inputFile = args[++i];
                continue;
            }
            if (arg == "--output" || arg == "-o")
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"La opción '{arg}' requiere un valor.");
                outputFile = args[++i];
                continue;
            }
            if (arg == "--delimiter" || arg == "-d")
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"La opción '{arg}' requiere un valor.");
                string raw = args[++i];
                delimiter = raw == @"\t" ? "\t" : raw;
                continue;
            }
            if (!arg.StartsWith('-'))
            {
                if (positional == 0) { inputFile = arg; positional++; }
                else if (positional == 1) { outputFile = arg; positional++; }
                else throw new ArgumentException($"Argumento posicional inesperado: '{arg}'.");
                continue;
            }

            throw new ArgumentException($"Opción desconocida: '{arg}'.");
        }

        return new AppConfig(inputFile, outputFile, delimiter, noHeader, showHelp, sortFields);
    }


    string ReadInput(AppConfig cfg)
    {
        if (cfg.InputFile is not null)
            return File.ReadAllText(cfg.InputFile);
        return Console.In.ReadToEnd();
    }

    (List<Dictionary<string, string>> Rows, string[]? Headers) ParseDelimited(string text, AppConfig cfg)
    {
        var lines = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Where(l => l.Length > 0)
            .ToArray();

        if (lines.Length == 0)
            return (new List<Dictionary<string, string>>(), null);

        string[]? headers;
        int dataStart;

        if (!cfg.NoHeader)
        {
            headers = lines[0].Split(cfg.Delimiter);
            dataStart = 1;
        }
        else
        {
            headers = null;
            dataStart = 0;
        }

        var rows = new List<Dictionary<string, string>>();

        for (int lineIdx = dataStart; lineIdx < lines.Length; lineIdx++)
        {
            var values = lines[lineIdx].Split(cfg.Delimiter);
            var row = new Dictionary<string, string>();

            if (!cfg.NoHeader && headers is not null)
            {
                for (int col = 0; col < headers.Length; col++)
                    row[headers[col]] = col < values.Length ? values[col] : string.Empty;
            }
            else
            {
                for (int col = 0; col < values.Length; col++)
                    row[col.ToString()] = values[col];
            }

            rows.Add(row);
        }

        if (cfg.SortFields.Count > 0 && rows.Count > 0)
        {
            foreach (var sf in cfg.SortFields)
            {
                if (!rows[0].ContainsKey(sf.Name))
                {
                    string disponibles = string.Join(", ", rows[0].Keys);
                    throw new ArgumentException(
                        $"Campo '{sf.Name}' no existe. Columnas disponibles: {disponibles}");
                }
            }
        }

        return (rows, headers);
    }


    List<Dictionary<string, string>> SortRows(
        List<Dictionary<string, string>> rows,
        List<SortField> sortFields)
    {
        if (sortFields.Count == 0 || rows.Count == 0)
            return rows;

        double NumKey(Dictionary<string, string> row, string name) =>
            double.TryParse(row[name],
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double d) ? d : 0.0;

        SortField first = sortFields[0];

        IOrderedEnumerable<Dictionary<string, string>> ordered =
            (first.Numeric, first.Descending) switch
            {
                (true, true) => rows.OrderByDescending(r => NumKey(r, first.Name)),
                (true, false) => rows.OrderBy(r => NumKey(r, first.Name)),
                (false, true) => rows.OrderByDescending(r => r[first.Name], StringComparer.OrdinalIgnoreCase),
                (false, false) => rows.OrderBy(r => r[first.Name], StringComparer.OrdinalIgnoreCase),
            };

        for (int i = 1; i < sortFields.Count; i++)
        {
            SortField sf = sortFields[i];
            ordered = (sf.Numeric, sf.Descending) switch
            {
                (true, true) => ordered.ThenByDescending(r => NumKey(r, sf.Name)),
                (true, false) => ordered.ThenBy(r => NumKey(r, sf.Name)),
                (false, true) => ordered.ThenByDescending(r => r[sf.Name], StringComparer.OrdinalIgnoreCase),
                (false, false) => ordered.ThenBy(r => r[sf.Name], StringComparer.OrdinalIgnoreCase),
            };
        }

        return ordered.ToList();
    }


    string Serialize(
        List<Dictionary<string, string>> rows,
        string[]? headers,
        AppConfig cfg)
    {
        var sb = new System.Text.StringBuilder();

        if (!cfg.NoHeader && headers is not null)
        {
            sb.AppendLine(string.Join(cfg.Delimiter, headers));
            foreach (var row in rows)
            {
                var values = headers.Select(h => row.TryGetValue(h, out string? v) ? v : string.Empty);
                sb.AppendLine(string.Join(cfg.Delimiter, values));
            }
        }
        else
        {
            foreach (var row in rows)
            {
                var values = row.OrderBy(kv => int.Parse(kv.Key)).Select(kv => kv.Value);
                sb.AppendLine(string.Join(cfg.Delimiter, values));
            }
        }

        return sb.ToString();
    }


    void WriteOutput(string text, AppConfig cfg)
    {
        if (cfg.OutputFile is not null)
            File.WriteAllText(cfg.OutputFile, text);
        else
            Console.Write(text);
    }

    void ShowHelp()
    {
        Console.WriteLine("""
        sortx — Ordena archivos de texto delimitados (CSV, TSV, PSV, etc.)

        USO:
          sortx [input [output]] [-b campo[:tipo[:orden]]]... [opciones]
          dotnet run sortx.cs -- [input [output]] [-b campo[:tipo[:orden]]]... [opciones]

        OPCIONES:
          -b, --by campo[:tipo[:orden]]   Campo de ordenamiento (repetible).
                                            tipo  : alpha (default) | num
                                            orden : asc  (default)  | desc
          -i, --input  <archivo>          Archivo de entrada  (default: stdin).
          -o, --output <archivo>          Archivo de salida   (default: stdout).
          -d, --delimiter <delim>         Delimitador         (default: ,).
                                            Usar \t para tabulación.
          -nh, --no-header                Sin encabezado; campos por índice (0, 1, 2...).
          -ay,  --ayuda                     Muestra esta ayuda y termina.

        EJEMPLOS:
          sortx empleados.csv -b apellido
          sortx empleados.csv -b salario:num:desc
          sortx empleados.csv -b departamento -b salario:num:desc
          sortx empleados.csv resultado.csv -b apellido
          sortx -i empleados.csv -o resultado.csv -b apellido
          sortx datos.tsv -d "\t" -nh -b 1:alpha:asc
          cat empleados.csv | sortx -b apellido > ordenado.csv
        """);
    }

    var config = ParseArgs(args);

    if (config.ShowHelp)
    {
        ShowHelp();
        return; 
    }

    var rawText = ReadInput(config);
    var (rows, headers) = ParseDelimited(rawText, config);
    var sortedRows = SortRows(rows, config.SortFields);
    var output = Serialize(sortedRows, headers, config);
    WriteOutput(output, config);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Error de argumentos: {ex.Message}");
    Console.Error.WriteLine("Use --help para ver la ayuda.");
    Environment.Exit(1);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"Error: Archivo no encontrado — {ex.FileName}");
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error inesperado: {ex.Message}");
    Environment.Exit(1);
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    bool ShowHelp,
    List<SortField> SortFields
);