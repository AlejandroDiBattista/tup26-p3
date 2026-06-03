using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

try
{
    AppConfig config= ParseArgs(args);
    if (config.ShowHelp)
    {
        ShowHelpMessage();
        return 0;
    }
    if (ConfiguredAsyncDisposable.SortFields.Count==0)
    {
        throw new ArgumentException("debe especificar al menos un campo para ordenar usando -b o --by.");
    }
    string rawText= ReadInput(config);
    var (headers, rows)=ParseDelimited(rawText,config);
    List<Dictionary<string, string>>sortedRows=SortRows(rows,headers,config);
    string outputText=OnSerializedAttribute(headers,sortedRows,config);
    WriteOutput(outputText,config);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error:{ex.Message}");
    return 1;
}
AppConfig ParseArgs(string[] arguments)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    bool showHelp = false;
    List<SortField> sortFields = new List<SortField>();
    List<string> positionalArgs = new List<string>();

    for (int i = 0; i < arguments.Length; i++)
    {
        string arg = arguments[i];

        if (arg == "-h" || arg == "--help")
        {
            showHelp = true;
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            noHeader = true;
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            if (i + 1 >= arguments.Length) throw new ArgumentException("Falta el valor del delimitador.");
            delimiter = arguments[++i];
            if (delimiter == "\\t") delimiter = "\t";
        }
        else if (arg == "-i" || arg == "--input")
        {
            if (i + 1 >= arguments.Length) throw new ArgumentException("Falta el archivo de entrada.");
            inputFile = arguments[++i];
        }
        else if (arg == "-o" || arg == "--output")
        {
            if (i + 1 >= arguments.Length) throw new ArgumentException("Falta el archivo de salida.");
            outputFile = arguments[++i];
        }
        else if (arg == "-b" || arg == "--by")
        {
            if (i + 1 >= arguments.Length) throw new ArgumentException("Falta la especificación del campo en --by.");
            string byVal = arguments[++i];
            sortFields.Add(ParseSortField(byVal));
        }
        else if (!arg.StartsWith("-"))
        {
            positionalArgs.Add(arg);
        }
        else
        {
            throw new ArgumentException($"Opción desconocida: {arg}");
        }
    }

    if (positionalArgs.Count > 0 && inputFile == null) inputFile = positionalArgs[0];
    if (positionalArgs.Count > 1 && outputFile == null) outputFile = positionalArgs[1];

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields, showHelp);

    SortField ParseSortField(string expression)
    {
        string[] parts = expression.Split(':');
        string name = parts[0];
        bool isNumeric = false;
        bool isDescending = false;

        if (parts.Length > 1)
        {
            if (parts[1] == "num") isNumeric = true;
            else if (parts[1] == "desc") isDescending = true;
        }
        if (parts.Length > 2)
        {
            if (parts[2] == "desc") isDescending = true;
        }

        return new SortField(name, isNumeric, isDescending);
    }
}

void ShowHelpMessage()
{
    Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
    Console.WriteLine("Opciones:");
    Console.WriteLine("  -b, --by          Campo por el que ordenar (campo[:tipo[:orden]])");
    Console.WriteLine("  -i, --input       Archivo de entrada");
    Console.WriteLine("  -o, --output      Archivo de salida");
    Console.WriteLine("  -d, --delimiter   Carácter delimitador (Default: ,)");
    Console.WriteLine("  -nh, --no-header  Indica que el archivo no tiene encabezado");
    Console.WriteLine("  -h, --help        Muestra esta ayuda");
}
