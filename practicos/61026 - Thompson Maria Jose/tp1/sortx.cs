using System.Globalization;

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);

try
{
    var config = ParseArgs(args);

    if (config == null)
        return;

    var text = ReadInput(config);

    var (header, rows) = ParseDelimited(text, config);

    var sorted = SortRows(rows, config);

    var output = Serialize(header, sorted, config);

    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig? ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;

    var sortFields = new List<SortField>();
    var positional = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h":
            case "--help":
                ShowHelp();
                return null;

            case "-i":
            case "--input":
                input = args[++i];
                break;

            case "-o":
            case "--output":
                output = args[++i];
                break;

            case "-d":
            case "--delimiter":
                delimiter = args[++i] == "\\t" ? "\t" : args[i];
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-b":
            case "--by":

                var parts = args[++i].Split(':');

                var field = parts[0];

                bool numeric = false;
                bool desc = false;

                if (parts.Length > 1)
                    numeric = parts[1] == "num";

                if (parts.Length > 2)
                    desc = parts[2] == "desc";

                sortFields.Add(new SortField(field, numeric, desc));

                break;

            default:
                positional.Add(args[i]);
                break;
        }
    }

    if (input == null && positional.Count > 0)
        input = positional[0];

    if (output == null && positional.Count > 1)
        output = positional[1];

    if (sortFields.Count == 0)
        throw new Exception("Debe especificar al menos un campo de ordenamiento");

    return new AppConfig(
        input,
        output,
        delimiter,
        noHeader,
        sortFields
    );
}

string ReadInput(AppConfig config)
{
    if (config.InputFile != null)
        return File.ReadAllText(config.InputFile);

    return Console.In.ReadToEnd();
}

(List<string>? Header, List<Dictionary<string, string>> Rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    if (lines.Length == 0)
        throw new Exception("Archivo vacío");

    List<string>? header = null;

    int start = 0;

    if (!config.NoHeader)
    {
        header = lines[0]
            .Split(config.Delimiter)
            .ToList();

        start = 1;
    }

    var rows = new List<Dictionary<string, string>>();

    for (int i = start; i < lines.Length; i++)
    {
        var values = lines[i].Split(config.Delimiter);

        var row = new Dictionary<string, string>();

        if (config.NoHeader)
        {
            for (int j = 0; j < values.Length; j++)
            {
                row[j.ToString()] = values[j];
            }
        }
        else
        {
            for (int j = 0; j < header!.Count; j++)
            {
                row[header[j]] = j < values.Length
                    ? values[j]
                    : "";
            }
        }

        rows.Add(row);
    }

    if (rows.Count == 0)
        return (header, rows);

    foreach (var sf in config.SortFields)
    {
        if (!rows[0].ContainsKey(sf.Name))
            throw new Exception($"Columna inexistente: {sf.Name}");
    }

    return (header, rows);
}

List<Dictionary<string, string>> SortRows(
    List<Dictionary<string, string>> rows,
    AppConfig config
)
{
    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    for (int i = 0; i < config.SortFields.Count; i++)
    {
        var sf = config.SortFields[i];

        Func<Dictionary<string, string>, object> selector = row =>
        {
            var value = row[sf.Name];

            if (sf.Numeric)
            {
                double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double number
                );

                return number;
            }

            return value;
        };

        if (i == 0)
        {
            ordered = sf.Descending
                ? rows.OrderByDescending(selector)
                : rows.OrderBy(selector);
        }
        else
        {
            ordered = sf.Descending
                ? ordered!.ThenByDescending(selector)
                : ordered!.ThenBy(selector);
        }
    }

    return ordered!.ToList();
}

string Serialize(
    List<string>? header,
    List<Dictionary<string, string>> rows,
    AppConfig config
)
{
    var lines = new List<string>();

    if (!config.NoHeader && header != null)
    {
        lines.Add(string.Join(config.Delimiter, header));
    }

    foreach (var row in rows)
    {
        IEnumerable<string> values;

        if (config.NoHeader)
        {
            values = row
                .OrderBy(x => int.Parse(x.Key))
                .Select(x => x.Value);
        }
        else
        {
            values = header!.Select(h => row[h]);
        }

        lines.Add(string.Join(config.Delimiter, values));
    }

    return string.Join(Environment.NewLine, lines);
}

void WriteOutput(string text, AppConfig config)
{
    if (config.OutputFile != null)
    {
        File.WriteAllText(config.OutputFile, text);
    }
    else
    {
        Console.WriteLine(text);
    }
}

void ShowHelp()
{
    Console.WriteLine("""
sortx [input [output]] [-b|--by campo[:tipo[:orden]]]
      [-i|--input input]
      [-o|--output output]
      [-d|--delimiter delimitador]
      [-nh|--no-header]
      [-h|--help]

""");
}