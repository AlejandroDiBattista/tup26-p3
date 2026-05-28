
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

try
{
    var config = ParseArgs(args);
    var text = ReadInput(config);
    var rows = ParseDelimited(text, config);
    var sorted = SortRows(rows, config);
    var output = Serialize(sorted, config);
    WriteOutput(output, config);
}
catch (Exception error)
{
    Console.Error.WriteLine($"Error: {error.Message}");
    Environment.Exit(1);
}

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
                ShowHelp();
                Environment.Exit(0);
                continue;
            case "-b":
            case "--by":
                var spec = args[++i];
                sortFields.Add(ParseField(spec));
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
SortField ParseField(string spec)
{
    var segments = spec.Split(':');
    var fieldName = segments[0];
    bool isNumeric = false;
    bool isDescending = false;
    if(segments.Length > 1) isNumeric = segments[1] switch { "num" => true, "alpha" => false, _ => throw new Exception($"Tipo de campo desconocido: {segments[1]}")};
    if(segments.Length > 2) isDescending = segments[2] switch { "asc" => false, "desc" => true, _ => throw new Exception($"Orden de campo desconocido: {segments[2]}")};
    return new SortField(fieldName, isNumeric, isDescending);
}
// 2. ReadInput      → leer el texto desde el archivo o stdin
string ReadInput(AppConfig config)
{
    if(!string.IsNullOrEmpty(config.InputFile))
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

//3. ParseDelimited → convertir el texto en una lista de filas (lista de diccionarios)
List<Dictionary<string, string>> ParseDelimited(string text, AppConfig settings)
{
    var result = new List<Dictionary<string, string>>();

    var records = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    if (records.Length == 0)
        return result;

    string[] columns;

    if (!settings.NoHeader)
    {
        columns = records[0].Split(settings.Delimiter);

        for (int rowIndex = 1; rowIndex < records.Length; rowIndex++)
        {
            var fields = records[rowIndex].Split(settings.Delimiter);

            var map = new Dictionary<string, string>();

            for (int colIndex = 0; colIndex < columns.Length; colIndex++)
                map[columns[colIndex]] = fields[colIndex];

            result.Add(map);
        }
    }
    else
    {
        var sample = records[0].Split(settings.Delimiter);
        columns = Enumerable.Range(0, sample.Length).Select(i => i.ToString()).ToArray();

        foreach (var record in records)
        {
            var fields = record.Split(settings.Delimiter);
            var map = new Dictionary<string, string>();

            for (int colIndex = 0; colIndex < columns.Length; colIndex++)
                map[columns[colIndex]] = fields[colIndex];

            result.Add(map);
        }
    }

    return result;
}
//4. SortRows       → ordenar las filas según los criterios configurados
List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
        return rows;

    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    foreach (var field in config.SortFields)
    {
        Func<Dictionary<string, string>, object> key = row =>
        {
            if (!row.ContainsKey(field.Name))
                throw new Exception($"Columna inexistente: {field.Name}");

            var value = row[field.Name];
            if (field.Numeric)
            {
                if (!double.TryParse(value, out var num))
                    throw new Exception($"Valor no numérico: {value}");

                return num;
            }
            return value;
        };
        if (ordered == null)
        {
            ordered = field.Descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
        }
        else
        {
            ordered = field.Descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
        }
    }
    return (ordered ?? Enumerable.Empty<Dictionary<string, string>>()).ToList();
}
//5. Serialize      → convertir las filas ordenadas de vuelta a texto
string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
        return "";

    var builder = new StringBuilder();
    var columnNames = rows[0].Keys.ToList();

    if (!config.NoHeader)
        builder.AppendLine(string.Join(config.Delimiter, columnNames));

    foreach (var item in rows)
    {
        var fieldValues = columnNames.Select(col => item[col]);
        builder.AppendLine(string.Join(config.Delimiter, fieldValues));
    }

    return builder.ToString();
}
//6. WriteOutput    → escribir en el archivo de salida o stdout
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
//Modelo de configuración
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
record SortField(string Name, bool Numeric, bool Descending);