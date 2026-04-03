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