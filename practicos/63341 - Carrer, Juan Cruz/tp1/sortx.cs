using   System;
using   System.Collections.Generic;
using   System.IO;
using   System.Linq;
using   System.Text;

AppConfig ParseArgs(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        switch (args[i])
        {
            case "-i":
            case"--input":
                input = args[++i];
                break;

            case "-o":
            case "--output":
                outputFile = args[++i];
                break;

            case "-d":
            case "--delimiter":
                delimiter = args[++i];
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-h":
            case "--help":
                ShowHelp();
                Environment.Exit(0);
                break;

            case "-b":
            case "--by":
                var spec = args[++i];
                sortFields.Add(ParseSortField(spec));
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new ArgumentException($"Opción desconocida: {arg}");
                
                positionals.Add(arg);
                break;
        }
    }

    if (positionals.Count > 0 && inputFile == null)
        inputFile = positionals[0];

    if (positionals.Count > 1 && outputFile == null)
        outputFile = positionals[1];

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

void ShowHelp()
{
    Console.WriteLine(@"
    Uso: 
        sortx [input [output]] -b campo[ :tipo[:orden]]...

    Opciones:
      -b, --by                    Campo de ordenamiento (puede repetirse)
      -i, --input <archivo>       Archivo de entrada (CSV)
      -o, --output <archivo>      Archivo de salida (CSV)
      -d, --delimiter <carácter>  Delimitador (por defecto: ',')
      -nh, --no-header            Indica que el CSV no tiene fila de encabezado
      -h, --help                  Muestra esta ayuda

      Ejemplo:
        sortx empleados.csv -b apellido
        sortx empleados.csv -b salario:num:desc
    ");
}

