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

string ReadInput(AppConfig config)
{
    if (!string.IsNullOrEmpty(config.InputFile)) {
        if (!File.Exists(config.InputFile))
            throw new FileNotFoundException($"Archivo no encontrado: {config.InputFile}");

        return File.ReadAllText(config.InputFile);
    }

    if (Console.IsInputRedirected)
        throw new InvalidOperationException("No se puede leer de la entrada estándar redirigida sin un archivo de entrada especificado.");

    using var reader = Console.In;
    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string texto, AppConfig config)
{
    var rows = new List<Dictionary<string, string>>();
    var lines = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    if (lines.Length == 0)
        return rows;

    string[] headers;

    if (!config.NoHeader) {
        headers = lines[0].Split(config.Delimiter);

        for (int i = 1; i < lines.Length; i++) {
            var value = lines[i].Split(config.Delimiter);
            var dictio  = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length && j < value.Length; j++)
                dictio[headers[j]] = value[j];

            rows.Add(dictio);
        }   
    }
    else {
        var primero = lines[0].Split(config.Delimiter);
        headers  = Enumerable.Range(0, primero.Length).Select(i => $"col{i}").ToArray();

        foreach (var line in lines) {
            var value = line.Split(config.Delimiter);
            var dictio  = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length && j < value.Length; j++)
                dictio[headers[j]] = value[j];

            rows.Add(dictio);
        }
    }   

    return rows;
}
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields
);

