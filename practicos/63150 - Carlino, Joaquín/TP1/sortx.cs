using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var config = ParseArgs(args);
            var rawLines = ReadInput(config);
            var (headers, rows) = ParseDelimited(rawLines, config);
            var sortedRows = SortRows(rows, config);
            var outputText = Serialize(headers, sortedRows, config);
            WriteOutput(outputText, config);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    // --- Modelos de configuración ---
    record SortField(string NameOrIndex, bool Numeric, bool Descending);
    record AppConfig(
        string? InputFile,
        string? OutputFile,
        string Delimiter,
        bool NoHeader,
        List<SortField> SortFields);

    // --- Implementación de Funciones Locales ---

    static AppConfig ParseArgs(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help") || args.Length == 0)
        {
            Console.WriteLine("Uso: sortx [input [output]] [opciones]");
            Console.WriteLine("Opciones:");
            Console.WriteLine("  -b, --by campo[:tipo[:orden]]  Campo para ordenar (ej. apellido:alpha:asc)");
            Console.WriteLine("  -d, --delimiter DELIM          Delimitador (default: ,). Use \\t para tabs.");
            Console.WriteLine("  -nh, --no-header               Indica que no hay encabezado.");
            Console.WriteLine("  -i, --input FILE               Archivo de entrada.");
            Console.WriteLine("  -o, --output FILE              Archivo de salida.");
            Environment.Exit(0);
        }

        string? input = null;
        string? output = null;
        string delimiter = ",";
        bool noHeader = false;
        var sortFields = new List<SortField>();

        // Manejo de argumentos posicionales iniciales
        int i = 0;
        if (i < args.Length && !args[i].StartsWith("-")) input = args[i++];
        if (i < args.Length && !args[i].StartsWith("-")) output = args[i++];

        for (; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i": case "--input": input = args[++i]; break;
                case "-o": case "--output": output = args[++i]; break;
                case "-nh": case "--no-header": noHeader = true; break;
                case "-d": case "--delimiter": 
                    delimiter = args[++i].Replace("\\t", "\t"); 
                    break;
                case "-b": case "--by":
                    var parts = args[++i].Split(':');
                    string name = parts[0];
                    bool isNum = parts.Length > 1 && parts[1].Equals("num", StringComparison.OrdinalIgnoreCase);
                    bool isDesc = parts.Length > 2 && parts[2].Equals("desc", StringComparison.OrdinalIgnoreCase);
                    sortFields.Add(new SortField(name, isNum, isDesc));
                    break;
            }
        }

        if (sortFields.Count == 0) throw new Exception("Debe especificar al menos un criterio de ordenamiento con --by.");

        return new AppConfig(input, output, delimiter, noHeader, sortFields);
    }

    static List<string> ReadInput(AppConfig config)
    {
        if (!string.IsNullOrEmpty(config.InputFile))
        {
            return File.ReadAllLines(config.InputFile).ToList();
        }
        
        var lines = new List<string>();
        string? line;
        while ((line = Console.ReadLine()) != null) lines.Add(line);
        return lines;
    }

    static (List<string>? Headers, List<Dictionary<string, string>> Rows) ParseDelimited(List<string> lines, AppConfig config)
    {
        if (lines.Count == 0) return (null, new List<Dictionary<string, string>>());

        List<string>? headers = null;
        int startIndex = 0;

        if (!config.NoHeader)
        {
            headers = lines[0].Split(config.Delimiter).Select(h => h.Trim()).ToList();
            startIndex = 1;
        }

        var rows = new List<Dictionary<string, string>>();
        for (int i = startIndex; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cells = lines[i].Split(config.Delimiter);
            var rowDict = new Dictionary<string, string>();

            for (int j = 0; j < cells.Length; j++)
            {
                string key = config.NoHeader ? j.ToString() : (j < headers!.Count ? headers[j] : j.ToString());
                rowDict[key] = cells[j].Trim();
            }
            rows.Add(rowDict);
        }

        return (headers, rows);
    }

    static List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
    {
        if (rows.Count == 0) return rows;

        // Validar que las columnas existan antes de ordenar
        foreach (var field in config.SortFields)
        {
            if (!rows[0].ContainsKey(field.NameOrIndex))
                throw new Exception($"La columna '{field.NameOrIndex}' no existe en los datos.");
        }

        IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

        for (int i = 0; i < config.SortFields.Count; i++)
        {
            var f = config.SortFields[i];
            
            Func<Dictionary<string, string>, object> keySelector = r => 
            {
                string val = r[f.NameOrIndex];
                if (f.Numeric)
                {
                    if (double.TryParse(val, out double num)) return num;
                    return 0.0;
                }
                return val;
            };

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

    static string Serialize(List<string>? headers, List<Dictionary<string, string>> rows, AppConfig config)
    {
        var output = new List<string>();

        if (headers != null)
        {
            output.Add(string.Join(config.Delimiter, headers));
        }

        foreach (var row in rows)
        {
            // Reconstruimos la fila basada en las llaves del primer registro o encabezados
            var keys = headers ?? row.Keys.OrderBy(k => int.Parse(k)).Select(k => k.ToString()).ToList();
            output.Add(string.Join(config.Delimiter, keys.Select(k => row.GetValueOrDefault(k, ""))));
        }

        return string.Join(Environment.NewLine, output);
    }

    static void WriteOutput(string text, AppConfig config)
    {
        if (!string.IsNullOrEmpty(config.OutputFile))
        {
            File.WriteAllText(config.OutputFile, text);
        }
        else
        {
            Console.WriteLine(text);
        }
    }
}
