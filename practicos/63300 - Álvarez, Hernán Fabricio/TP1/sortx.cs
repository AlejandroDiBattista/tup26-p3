using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;






try
{
    var config = ParseArgs(args);
    var input = ReadInput(config);
    var rows = ParseDelimited(input, config, out string[] header);
    var sortedRows = SortRows(rows, header, config);
    var output = Serialize(sortedRows, header, config);
    WriteOutput(output, config);

} catch (Exception ex)
{
    Console.Error.WriteLine($"Error : {ex.Message}");
    Environment.Exit(1);
}


//* 1° Funcion ParseArgs → leer la configuración desde los argumentos

AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();
    var posArgs = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg is "-h" or "--help")
        {
            Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
            Environment.Exit(0);
        }
        else if (arg is "-i" or "--input" && i + 1 < args.Length)
        {
            input = args[++i];
        }
        else if (arg is "-o" or "--output" && i + 1 < args.Length)
        {
            output = args[++i];
        }
        else if (arg is "-d" or "--delimiter" && i + 1 < args.Length)
        {
            delimiter = args[++i];
            if (delimiter == "\\t") delimiter = "\t"; // Manejo de tabulación explícita
        }
        else if (arg is "-nh" or "--no-header")
        {
            noHeader = true;
        }
        else if (arg is "-b" or "--by" && i + 1 < args.Length)
        {
            var spec = args[++i].Split(':');
            string name = spec[0];
            bool numeric = spec.Length > 1 && spec[1] == "num";
            bool desc = spec.Length > 2 && spec[2] == "desc";
            sortFields.Add(new SortField(name, numeric, desc));
        }
        else if (!arg.StartsWith("-"))
        {
            posArgs.Add(arg); // Guardamos argumentos posicionales
        }
        else
        {
            throw new ArgumentException($"Opción desconocida: {arg}");
        }
    }

    // Asignación de argumentos posicionales si no se usaron -i o -o
    if (input == null && posArgs.Count > 0) input = posArgs[0];
    if (output == null && posArgs.Count > 1) output = posArgs[1];

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}


//* 2° Funcion ReadInput  → leer el texto desde el archivo o stdin


string ReadInput(AppConfig config)
{
    if (string.IsNullOrEmpty(config.InputFile))
    {
        // Si no hay archivo, leemos de stdin (útil para tuberías: cat datos.csv | sortx)
        if (Console.IsInputRedirected)
            return Console.In.ReadToEnd();
        
        throw new ArgumentException("No se especificó archivo de entrada y no hay datos en stdin.");
    }

    if (!File.Exists(config.InputFile)) 
        throw new FileNotFoundException("Archivo no encontrado.", config.InputFile);

    return File.ReadAllText(config.InputFile);
}

List<string[]> ParseDelimited(string input, AppConfig config, out string[] header)
{
    var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    var rows = new List<string[]>();
    header = Array.Empty<string>();

    if (lines.Length == 0) return rows;

    int startIndex = 0;
    if (!config.NoHeader)
    {
        header = lines[0].Split(config.Delimiter);
        startIndex = 1; // Empezamos a leer datos desde la fila 1
    }

    for (int i = startIndex; i < lines.Length; i++)
    {
        rows.Add(lines[i].Split(config.Delimiter));
    }

    return rows;
}

List<string[]> SortRows(List<string[]> rows, string[] header, AppConfig config)
{
    if (config.SortFields.Count == 0 || rows.Count == 0) return rows;

    IOrderedEnumerable<string[]>? sorted = null;

    for (int i = 0; i < config.SortFields.Count; i++)
    {
        var field = config.SortFields[i];
        int colIndex = GetColumnIndex(field.Name, header, config.NoHeader);

        // Función auxiliar para obtener el valor casteado si es numérico
        object GetKey(string[] row)
        {
            if (colIndex >= row.Length) return field.Numeric ? 0m : "";
            string val = row[colIndex];
            
            if (field.Numeric)
            {
                if (decimal.TryParse(val, out decimal num)) return num;
                return 0m;
            }
            return val;
        }

        // El primer ordenamiento usa OrderBy, los subsecuentes usan ThenBy
        if (i == 0)
        {
            sorted = field.Descending ? rows.OrderByDescending(GetKey) : rows.OrderBy(GetKey);
        }
        else
        {
            sorted = field.Descending ? sorted!.ThenByDescending(GetKey) : sorted!.ThenBy(GetKey);
        }
    }

    return sorted!.ToList();
}


//* 3° Funcion GetColumnIndex


int GetColumnIndex(string name, string[] header, bool noHeader)
{
    if (noHeader)
    {
        if (int.TryParse(name, out int idx)) return idx;
        throw new ArgumentException("Si no hay encabezado, el campo debe ser un índice numérico.");
    }
    
    int index = Array.IndexOf(header, name);
    if (index < 0) throw new ArgumentException($"La columna '{name}' no existe en el archivo.");
    return index;
}

string Serialize(List<string[]> rows, string[] header, AppConfig config)
{
    var lines = new List<string>();
    
    if (!config.NoHeader && header.Length > 0)
    {
        lines.Add(string.Join(config.Delimiter, header));
    }
    
    foreach (var row in rows)
    {
        lines.Add(string.Join(config.Delimiter, row));
    }
    
    //* Agregamos un salto de línea al final por convención en archivos de texto
    return string.Join(Environment.NewLine, lines) + Environment.NewLine;
}

//* 4° Funcion WriteOutput

void WriteOutput(string output, AppConfig config)
{
    if (string.IsNullOrEmpty(config.OutputFile))
    {
        Console.Write(output);
    }
    else
    {
        File.WriteAllText(config.OutputFile, output);
    }
}

record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields);


