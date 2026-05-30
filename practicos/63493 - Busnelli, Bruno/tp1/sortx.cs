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
    throw new NotImplementedException();
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