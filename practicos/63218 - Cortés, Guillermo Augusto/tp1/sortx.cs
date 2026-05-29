using System;
using System.Collections.Generic;
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

try
{
    var config = ParseArgs(args);
    var InputText = ReadInput(config);
    var rows = ParseDelimited(InputText, config);
    var sortedRows = SortRows(rows, config);
    var outputText = Serialize(sortedRows, config);
    WriteOutput(outputText, config);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Environment.Exit(1);
}
AppConfig ParseArgs(string[] args)
{
    Console.WriteLine($"sortx {string.Join(" ", args)}");

    return new AppConfig(
        null,
        null,
        ",",
        false,
        new List<SortOption>()
    );    
}

string ReadInput(AppConfig config)
{
    return "";
}

List<Dictionary<string, string>> ParseDelimited(string inputText, AppConfig config)
{
    return new List<Dictionary<string, string>>();
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    return rows;
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    return "";
}

void WriteOutput(string outputText, AppConfig config)
{
    
}
record SortOption(string Campo, string Tipo, string Orden);

record AppConfig(
    string? Input,
    string? Output,
    string Delimiter,
    bool NoHeader,
    List<SortOption> SortOptions
);
