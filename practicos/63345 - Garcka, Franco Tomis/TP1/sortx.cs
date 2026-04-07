using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

try
{
    var config = ParseArgs(args);
    var input = ReadInput(config);
    var rows = ParseDelimited(input, config);
    var sorted = SortRows(rows, config);
    var output = Serialize(sorted, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}
//ParseArgs
AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();
    int i = 0;
    if (i < args.Length && !args[i].StartsWith("-"))
    {
        input = args[i++];
    }

    if (i < args.Length && !args[i].StartsWith("-"))
    {
        output = args[i++];
    }

    while (i < args.Length)
    {
        var arg = args[i];
        if (arg == "-b" || arg == "--by")
        {
            var value = args[++i];
            var parts = value.Split(':');
            string name = parts[0];
            bool numeric = parts.Length > 1 && parts[1] == "num";
            bool descending = parts.Length > 2 && parts[2] == "desc";
            sortFields.Add(new SortField(name, numeric, descending));
        }
        i++;
    }
    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}
//ReadInput
string ReadInput(AppConfig config)
{
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        return File.ReadAllText(config.InputFile);
    }

    return Console.In.ReadToEnd();
}

//ParseDelimited
List<Dictionary<string, string>> ParseDelimited(string input, AppConfig config)
{
    throw new NotImplementedException();
}

//SortRows
List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    throw new NotImplementedException();
}

//Serialize
string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    throw new NotImplementedException();
}

//WriteOutput
void WriteOutput(string output, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile))
    {
        File.WriteAllText(config.OutputFile, output);
    }
    else
    {
        Console.WriteLine(output);
    }
}
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);