
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
    var sortFields = new List<SortField>();
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
            case "-b":
            case "--by":
                var spec = args[++i];
                sortFields.Add(ParseField(args[++i]));
                continue;
            default:
                if (arg.StartsWith("-")) throw new ArgumentException($"Opcion no valida: {arg}");
                positionals.Add(arg);
                continue;
        }
    }
    if(positionals.Count > 0 && input == null) input = positionals[0];
    if(positionals.Count > 1 && output == null) output = positionals[1];
    if(sortFields.Count == 0) throw new Exception("Debe indicar al menos un criterio con -b|--by.");
    return new AppConfig(input, output, delimiter, noHeader, sortFields);
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
//SortField
Sortfield ParseField(string spec)
{
    var segments = input.Split(':');
    var fieldName = segments[0];
    bool isNumeric = false;
    bool isDescending = false;
    if(segments.Length > 1) isNumeric = segments[1] switch { "num" => true, "alpha" => false, _ => throw new Exception($"Tipo de campo desconocido: {segments[1]}")};
    if(segments.Length > 2) isDescending = segments[2] switch { "asc" => false, "desc" => true, _ => throw new Exception($"Orden de campo desconocido: {segments[2]}")};
    return new SortField(fieldName, isNumeric, isDescending);
}
//ReadInput
string ReadInput(AppConfig config)
{
    if(!string.IsNullOrEmpty(config.InputFile));
    {
        if(!File.Exists(config.InputFile))
        {
            throw new FileNotFoundException($"No pude encontrar el archivo: {config.InputFile}");
        }
        return File.ReadAllText(config.InputFile);
    }
    if(!Console.IsInputRedirected)
    {
        throw new Exception("No se pasó ningun archivo ni datos para leer.");
    }
    return Console.In.ReadToEnd();
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