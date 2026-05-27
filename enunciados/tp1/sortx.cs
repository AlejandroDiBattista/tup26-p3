
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
    string delimiter = ",";
    bool noHeader = false;
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
            case "-d":
            case "--delimiter":
                delimiter = args[++i];
                continue;
            case "-nh":
            case "--no-header":
                noHeader = true;
                continue;
            case "-h":
            case "--help":
                showHelp();
                Environment.Exit(0);
                continue;
        }
    }
    if(positionals.Count > 0 && input == null) input = positionals[0];
    if(positionals.Count > 1 && output == null) output = positionals[1];
    return new AppConfig(input, output, delimiter, noHeader);
}

//ShowHelp
void ShowHelp()
{
    Console.WriteLine(@"
    Uso:
        sortx [input [output]] -b campo[:tipo[:orden]]...

    Opciones:
    -b, --by           Campo de ordenamiento
    -i, --input        Archivo de entrada
    -o, --output       Archivo de salida
    -d, --delimiter    Delimitador (default ,)
    -nh, --no-header   Sin encabezado
    -h, --help         Mostrar ayuda

    Ejemplos:
    sortx empleados.csv -b apellido
    sortx empleados.csv -b salario:num:desc
    ");
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