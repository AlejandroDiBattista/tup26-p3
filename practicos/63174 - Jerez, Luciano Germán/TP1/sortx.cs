using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//Modelos de datos
record SortField(string NameOrIndex, bool Numeric, bool Descending);
record AppConfig(string? InputFile, string? OutputFile, string Delimiter, bool NoHeader, List<SortField> SortFields);

class Program
{
    //Bloque principal
    static void Main(string[] args)
    {
        try
        { 
            //Declaración de variables
            //1. ParseArgs: Convierte los argumentos de la consola en un objeto de configuracion.
            var config = ParseArgs(args);
            //2. ReadInput: obtiene las lineas de texto (de archivos o de la entrada estandar).
            var rawLines = ReadInput(config.InputFile);
            //3. Parsedelimited: convierte el texto plano en una estructura de datos (filas y columnas).
            var (header, rows) = ParseDelimited(rawLines, config);
            // 4. SortRows: Aplica la lógica de ordenamiento (múltiples niveles, numérico o texto).
            var sortedRows = SortRows(rows, config);
            // 5. Serialize: Convierte los datos ordenados de nuevo a una cadena de texto con delimitadores.
            var output = Serialize(header, sortedRows, config);
            // 6. WriteOutput: Envía el resultado al archivo de destino o a la pantalla.
            WriteOutput(output, config.OutputFile);
        }
        catch(Exception ex)
        {
            // Manejo de errores: imprime el mensaje en el flujo de error y sale con código != 0.
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    //Funciones Locales 
    //Paso 1: Analizar argumentos
    static AppConfig ParseArgs(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string delimiter = ",";
        bool noHeader = false;
        var sortFields = new List<SortField>();
        var positionals = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h": case "--help":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
                case "-nh": case "--no-header":
                    noHeader = true;
                    break;
                case "-d": case "--delimiter":
                    // Soporte para caracteres de escape como \t (tabulador)
                    delimiter = args[++i].Replace("\\t", "\t");
                    break;
                case "-i": case "--input":
                    inputFile = args[++i];
                    break;
                case "-o": case "--output":
                    outputFile = args[++i];
                    break;
                case "-b": case "--by":
                    // Formato: campo:tipo:orden (ej. salario:num:desc)
                    var parts = args[++i].Split(':');
                    string name = parts[0];
                    bool isNum = parts.Length > 1 && parts[1] == "num";
                    bool isDesc = parts.Length > 2 && parts[2] == "desc";
                    sortFields.Add(new SortField(name, isNum, isDesc));
                    break;
                default:
                    // Si no empieza con '-', es un argumento posicional (archivo in/out)
                    if (!args[i].StartsWith("-")) positionals.Add(args[i]);
                    break;
            }
        }
        // Lógica para asignar archivos si se pasaron sin flags (ej: sortx in.csv out.csv)
        if (inputFile == null && positionals.Count > 0) inputFile = positionals[0];
        if (outputFile == null && positionals.Count > 1) outputFile = positionals[1];

        if (sortFields.Count == 0) throw new Exception("Debe especificar al menos un criterio de ordenamiento con -b.");

        return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
    }
 
    // PASO 2: Leer la fuente de datos
    static List<string> ReadInput(string? filePath)
    {
        // Si no hay archivo, leemos del flujo de entrada estándar (stdin)
        if (string.IsNullOrEmpty(filePath))
        {
            var lines = new List<string>();
            string? line;
            while ((line = Console.ReadLine()) != null) lines.Add(line);
            return lines;
        }
        return File.ReadAllLines(filePath).ToList();
    }

    // PASO 3: Parsear el texto a Diccionarios
    static (string? header, List<Dictionary<string, string>> rows) ParseDelimited(List<string> lines, AppConfig config)
    {
        if (lines.Count == 0) return (null, new List<Dictionary<string, string>>());

        // Si hay encabezado, es la primera línea; si no, el header es nulo.
        string? header = config.NoHeader ? null : lines[0];
        var dataLines = config.NoHeader ? lines : lines.Skip(1).ToList();
    
        // Si no hay header, usaremos los índices "0", "1", "2"... como nombres de columna.
        var columnNames = config.NoHeader 
            ? null 
            : header?.Split(config.Delimiter).Select(s => s.Trim()).ToArray();

        var resultRows = new List<Dictionary<string, string>>();

        foreach (var line in dataLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = line.Split(config.Delimiter);
            var row = new Dictionary<string, string>();

            for (int i = 0; i < values.Length; i++)
            {
                // Mapeamos el valor a su nombre de columna o a su índice
                string key = config.NoHeader ? i.ToString() : columnNames![i];
                row[key] = values[i].Trim();
            }
            resultRows.Add(row);
        }
        return (header, resultRows);
    }

    // PASO 4: Ordenamiento dinámico
    static List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
    {
        if (rows.Count == 0) return rows;

        // Validamos que todas las columnas pedidas para ordenar existan en los datos
        foreach (var field in config.SortFields)
        {
            if (!rows[0].ContainsKey(field.NameOrIndex))
                throw new Exception($"La columna '{field.NameOrIndex}' no existe en el archivo.");
        }

        IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

        for (int i = 0; i < config.SortFields.Count; i++)
        {
            var f = config.SortFields[i];
            
            // El Selector de Clave decide si tratamos el dato como número o como texto
            Func<Dictionary<string, string>, object> keySelector = r => 
            {
                if (f.Numeric)
                {
                    return double.TryParse(r[f.NameOrIndex], out double val) ? val : 0.0;
                }
                return r[f.NameOrIndex]; // Por defecto es string (alpha)
            };

            // El primer campo usa 'OrderBy', los siguientes usan 'ThenBy' para desempatar
            if (i == 0)
            {
                ordered = f.Descending ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);
            }
            else
            {
                ordered = f.Descending ? ordered!.ThenByDescending(keySelector) : ordered!.ThenBy(keySelector);
            }
        }

        return ordered!.ToList();
    }

    // PASO 5: Volver a convertir a texto
    static string Serialize(string? header, List<Dictionary<string, string>> rows, AppConfig config)
    {
        var outputLines = new List<string>();
        
        // Preservamos el encabezado original si existía
        if (header != null) outputLines.Add(header);

        foreach (var row in rows)
        {
            // Unimos los valores del diccionario usando el delimitador configurado
            outputLines.Add(string.Join(config.Delimiter, row.Values));
        }

        return string.Join(Environment.NewLine, outputLines);
    }

    // PASO 6: Escribir el resultado final
    static void WriteOutput(string content, string? filePath)
    {
        // Si no hay archivo de salida, escribimos en la consola (stdout)
        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine(content);
        }
        else
        {
            File.WriteAllText(filePath, content);
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("HERRAMIENTA SORTX - AYUDA");
        Console.WriteLine("Sintaxis: sortx [input] [output] -b campo:tipo:orden");
        // ... resto del texto de ayuda ...
    }
}

