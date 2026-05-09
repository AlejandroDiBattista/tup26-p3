using System.Text;

try
{
    var config = ParseArgs(args);
    var text = ReadInput(config.InputFile);
    var (headers, rows) = ParseDelimited(text, config);
    var sorted = SortRows(rows, headers, config);
    var output = Serialize(headers, sorted, config);
    WriteOutput(config.OutputFile, output);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var fields = new List<SortField>();
    var positional = new List<string>();

    for (int i = 0; i < args.Length;)
    {
        var arg = args[i];

        switch (arg)
        {
            case "-h":
            case "--help":
                PrintHelp();
                Environment.Exit(0);
                break;

            case "-i":
            case "--input":
                input = Next(args, ref i, arg);
                break;

            case "-o":
            case "--output":
                output = Next(args, ref i, arg);
                break;

            case "-d":
            case "--delimiter":
                delimiter = Next(args, ref i, arg);
                if (delimiter == "\\t") delimiter = "\t";
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                i++;
                break;

            case "-b":
            case "--by":
                fields.Add(ParseSortField(Next(args, ref i, arg)));
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new Exception($"Opción desconocida: {arg}");

                positional.Add(arg);
                i++;
                break;
        }
    }

    if (positional.Count >= 1 && input == null) input = positional[0];
    if (positional.Count >= 2 && output == null) output = positional[1];

    if (fields.Count == 0)
        throw new Exception("Debe indicar al menos un campo con -b");

    return new AppConfig(input, output, delimiter, noHeader, fields);
}

void PrintHelp()
{
    Console.WriteLine("Uso: sortx [input [output]] -b campo[:tipo[:orden]]");
}

string Next(string[] args, ref int i, string opt)
{
    i++;
    if (i >= args.Length)
        throw new Exception($"{opt} requiere valor");
    return args[i++];
}

SortField ParseSortField(string text)
{
    var parts = text.Split(':');

    string name = parts[0];
    bool numeric = parts.Length > 1 && parts[1] == "num";
    bool desc = parts.Length > 2 && parts[2] == "desc";

    return new SortField(name, numeric, desc);
}


string ReadInput(string? path)
{
    if (path == null)
        return Console.In.ReadToEnd();

    if (!File.Exists(path))
        throw new Exception($"Archivo no encontrado: {path}");

    return File.ReadAllText(path);
}

void WriteOutput(string? path, string content)
{
    if (path == null)
        Console.Write(content);
    else
        File.WriteAllText(path, content);
}

(string[] headers, List<Dictionary<string, string>> rows)
ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    if (lines.Length == 0)
        return (new string[0], new List<Dictionary<string, string>>());

    string[] headers;
    int start = 0;

    if (!config.NoHeader)
    {
        var raw = lines[0].Split(config.Delimiter);
        headers = new string[raw.Length];

        for (int i = 0; i < raw.Length; i++)
            headers[i] = raw[i].Trim();

        start = 1;
    }
    else
    {
        int cols = lines[0].Split(config.Delimiter).Length;
        headers = new string[cols];

        for (int i = 0; i < cols; i++)
            headers[i] = i.ToString();
    }

    var rows = new List<Dictionary<string, string>>();

    for (int i = start; i < lines.Length; i++)
    {
        var values = lines[i].Split(config.Delimiter);
        var row = new Dictionary<string, string>();

        for (int j = 0; j < headers.Length; j++)
        {
            string val = j < values.Length ? values[j] : "";
            row[headers[j]] = val.Trim();
        }

        rows.Add(row);
    }

    return (headers, rows);
}

List<Dictionary<string, string>> SortRows(
    List<Dictionary<string, string>> rows,
    string[] headers,
    AppConfig config)
{
    for (int i = 0; i < rows.Count - 1; i++)
    {
        for (int j = 0; j < rows.Count - i - 1; j++)
        {
            if (Compare(rows[j], rows[j + 1], config) > 0)
            {
                var temp = rows[j];
                rows[j] = rows[j + 1];
                rows[j + 1] = temp;
            }
        }
    }

    return rows;
}

int Compare(
    Dictionary<string, string> a,
    Dictionary<string, string> b,
    AppConfig config)
{
    for (int i = 0; i < config.SortFields.Count; i++)
    {
        var f = config.SortFields[i];

        if (!a.ContainsKey(f.Name))
            throw new Exception($"Columna inexistente: {f.Name}");

        string va = a[f.Name];
        string vb = b[f.Name];

        int result;

        if (f.Numeric)
        {
            double na = 0, nb = 0;
            double.TryParse(va, out na);
            double.TryParse(vb, out nb);

            result = na.CompareTo(nb);
        }
        else
        {
            result = string.Compare(va, vb);
        }

        if (result != 0)
            return f.Descending ? -result : result;
    }

    return 0;
}

string Serialize(
    string[] headers,
    List<Dictionary<string, string>> rows,
    AppConfig config)
{
    var sb = new StringBuilder();

    if (!config.NoHeader)
        sb.AppendLine(string.Join(config.Delimiter, headers));

    for (int i = 0; i < rows.Count; i++)
    {
        var row = rows[i];
        var values = new List<string>();

        for (int j = 0; j < headers.Length; j++)
            values.Add(row[headers[j]]);

        sb.AppendLine(string.Join(config.Delimiter, values));
    }

    return sb.ToString();
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
