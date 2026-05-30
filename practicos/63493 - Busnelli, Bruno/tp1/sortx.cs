using System.Globalization;

try
{
    var config = ParseArgs(args);
    var input = ReadInput(config);
    var table = ParseDelimited(input, config);
    var sortedRows = SortRows(table.Rows, config);
    var output = Serialize(table.Headers, sortedRows, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    bool help = false;

    var sortFields = new List<SortField>();
    var positionals = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];

        switch (arg)
        {
            case "-h":
            case "--help":
                help = true;
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-i":
            case "--input":
                inputFile = RequireValue(args, ref i, arg);
                break;

            case "-o":
            case "--output":
                outputFile = RequireValue(args, ref i, arg);
                break;

            case "-d":
            case "--delimiter":
                delimiter = NormalizeDelimiter(RequireValue(args, ref i, arg));
                break;

            case "-b":
            case "--by":
                sortFields.Add(ParseSortField(RequireValue(args, ref i, arg)));
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new ArgumentException($"Opción desconocida: {arg}");

                positionals.Add(arg);
                break;
        }
    }

    if (help)
    {
        Console.WriteLine(GetHelpText());
        Environment.Exit(0);
    }

    if (positionals.Count > 2)
        throw new ArgumentException("Se esperaban como máximo dos argumentos posicionales.");

    inputFile ??= positionals.Count >= 1 ? positionals[0] : null;
    outputFile ??= positionals.Count >= 2 ? positionals[1] : null;

    return new AppConfig(
        inputFile,
        outputFile,
        delimiter,
        noHeader,
        sortFields,
        help
    );
}

string ReadInput(AppConfig config)
{
    throw new NotImplementedException();
}

ParsedTable ParseDelimited(string text, AppConfig config)
{
    throw new NotImplementedException();
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    throw new NotImplementedException();
}

string Serialize(List<string> headers, List<Dictionary<string, string>> rows, AppConfig config)
{
    throw new NotImplementedException();
}

void WriteOutput(string output, AppConfig config)
{
    throw new NotImplementedException();
}

string GetHelpText()
{
    return "sortx - ayuda";
}

string RequireValue(string[] args, ref int index, string option)
{
    if (index + 1 >= args.Length)
        throw new ArgumentException($"La opción {option} requiere un valor.");

    index++;
    return args[index];
}

SortField ParseSortField(string value)
{
    var parts = value.Split(':');

    string name = parts[0];
    string type = parts.Length >= 2 ? parts[1] : "alpha";
    string order = parts.Length >= 3 ? parts[2] : "asc";

    bool numeric = type == "num";
    bool descending = order == "desc";

    return new SortField(name, numeric, descending);
}

string NormalizeDelimiter(string value)
{
    return value == "\\t" ? "\t" : value;
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields,
    bool Help
);

record ParsedTable(
    List<string> Headers,
    List<Dictionary<string, string>> Rows
);