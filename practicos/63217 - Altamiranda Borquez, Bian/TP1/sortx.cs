using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var config = ParseArgs(args);

            var text = ReadInput(config);

            var rows = ParseDelimited(text, config);

            var sorted = SortRows(rows, config);

            var output = Serialize(sorted, config);

            WriteOutput(output, config);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
  static AppConfig ParseArgs(string[] args)
    {
        string? input = null;
        string? output = null;
        string delimiter = ",";
        bool noHeader = false;
        var sortFields = new List<SortField>();
        var positionals = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
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
                        throw new Exception($"Opción desconocida: {arg}");

                    positionals.Add(arg);
                    break;
            }
        }

        if (positionals.Count > 0 && input == null)
            input = positionals[0];

        if (positionals.Count > 1 && output == null)
            output = positionals[1];

        if (sortFields.Count == 0)
            throw new Exception("Debe especificar al menos un campo de ordenamiento con -b");

        return new AppConfig(input, output, delimiter, noHeader, sortFields);
    }

    static void ShowHelp()
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
  static string ReadInput(AppConfig config)
    {
        if (!string.IsNullOrEmpty(config.InputFile))
        {
            if (!File.Exists(config.InputFile))
                throw new Exception($"Archivo no encontrado: {config.InputFile}");

            return File.ReadAllText(config.InputFile);
        }

        if (!Console.IsInputRedirected)
            throw new Exception("No se especificó archivo de entrada ni hay datos en stdin");

        using var reader = Console.In;
        return reader.ReadToEnd();
    }

