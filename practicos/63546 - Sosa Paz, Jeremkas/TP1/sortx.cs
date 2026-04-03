using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// --- 1. PUNTO DE ENTRADA (PIPELINE) ---
try
{
    var config = ParseArgs(args);
    if (config.ShowHelp)
    {
        ShowHelp();
        return 0;
    }

    string input = ReadInput(config);
    var (header, rows) = ParseDelimited(input, config);
    var sortedRows = SortRows(rows, config);
    string output = Serialize(header, sortedRows, config);
    WriteOutput(output, config);
    
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

// --- 2. MODELO DE CONFIGURACIÓN ---
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields,
    bool ShowHelp
);
// --- 3. FUNCIONES LOCALES ---

AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    bool showHelp = false;
    var sortFields = new List<SortField>();

    int positionalCount = 0;

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];
        if (arg == "-h" || arg == "--help") showHelp = true;
        else if (arg == "-nh" || arg == "--no-header") noHeader = true;
        else if (arg == "-d" || arg == "--delimiter") delimiter = args[++i].Replace("\\t", "\t");
        else if (arg == "-i" || arg == "--input") input = args[++i];
        else if (arg == "-o" || arg == "--output") output = args[++i];
        else if (arg == "-b" || arg == "--by")
        {
            var parts = args[++i].Split(':');
            string name = parts[0];
            bool isNum = parts.Length > 1 && parts[1] == "num";
            bool isDesc = parts.Length > 2 && parts[2] == "desc";
            sortFields.Add(new SortField(name, isNum, isDesc));
        }
        else
        {
            if (positionalCount == 0) input = arg;
            else if (positionalCount == 1) output = arg;
            positionalCount++;
        }
    }

    return new AppConfig(input, output, delimiter, noHeader, sortFields, showHelp);
}

void ShowHelp()
{
    Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]]... [-d delimitador] [-nh] [-h]");
}