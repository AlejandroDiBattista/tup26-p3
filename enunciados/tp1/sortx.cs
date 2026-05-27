
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// 1. ParseArgs      → leer la configuración desde los argumentos
AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    var positionals = new List<string>();
    for(int i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "-i":
            case "--input":
                input = args[++i];
                continue;
            case "-o":
            case "--output":
                output = args[++i];
                continue;
        }
    }
    if(positionals.Count > 0 && input == null) input = positionals[0];
    if(positionals.Count > 1 && output == null) output = positionals[1];
    return new AppConfig(input, output);
}
//Modelo de configuración
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
record SortField(string Name, bool Numeric, bool Descending);