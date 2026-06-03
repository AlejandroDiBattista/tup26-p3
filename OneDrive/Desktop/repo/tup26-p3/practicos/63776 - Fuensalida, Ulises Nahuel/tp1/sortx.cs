//TP 1 - Ulises Fuensalida

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System;

namespace SortApp
{
    public record SortField(string Field, string Type, string Order);
    public record Config(string? Input, string? Output, char Delimiter, bool HasHeader, List<SortField> SortFields);

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var config = ParseArgs(args);
                var (rows, headers) = ReadInput(config);
                var sorted = SortRows(rows, headers, config);
                WriteOutput(sorted, headers, config);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static Config ParseArgs(string[] args)
        {
            string? input = null;
            string? output = null;
            char delimiter = ',';
            bool hasHeader = true;
            var sortFields = new List<SortField>();
            var positionalArgs = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--input":
                    case "-i":
                        input = args[++i]; break;
                    case "--output":
                    case "-o":
                        output = args[++i]; break;
                    case "--delimiter":
                    case "-d":
                        var d = args[++i];
                        delimiter = d == "\\t" ? '\t' : d[0]; break;
                    case "--no-header":
                    case "-nh":
                        hasHeader = false; break;
                    case "--by":
                    case "-b":
                        var parts = args[++i].Split(':');
                        var field = parts[0];
                        var type = parts.Length > 1 ? parts[1] : "alpha";
                        var order = parts.Length > 2 ? parts[2] : "asc";
                        sortFields.Add(new SortField(field, type, order));
                        break;
                    case "--help":
                    case "-h":
                        ShowHelp();
                        Environment.Exit(0); break;
                    default:
                        if (!args[i].StartsWith("-")) positionalArgs.Add(args[i]);
                        break;
                }
            }

            if (input == null && positionalArgs.Count > 0) input = positionalArgs[0];
            if (output == null && positionalArgs.Count > 1) output = positionalArgs[1];

            if (sortFields.Count == 0)
                throw new Exception("Se debe especificar al menos un criterio de ordenamiento con -b");

            return new Config(input, output, delimiter, hasHeader, sortFields);
        }

        static (List<string[]> rows, string[]? headers) ReadInput(Config config)
        {
            List<string> lines = new();

            if (config.Input != null && File.Exists(config.Input))
                lines = File.ReadAllLines(config.Input).ToList();
            else if (Console.IsInputRedirected)
            {
                string? line;
                while ((line = Console.ReadLine()) != null) lines.Add(line);
            }
            else
            {
                // LISTA DE ALUMNOS
                lines = new List<string>
                {
                    "nombre,apellido,legajo",
                    "Juan,Perez,1001",
                    "Ana,Gomez,1002",
                    "Lucas,Ramirez,1003",
                    "Maria,Lopez,1004",
                    "Sofia,Martinez,1005"
                };
            }

            if (lines.Count == 0) return (new List<string[]>(), null);

            string[]? headers = null;
            int start = 0;

            if (config.HasHeader)
            {
                headers = lines[0].Split(config.Delimiter);
                start = 1;
            }

            var rows = lines.Skip(start)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Split(config.Delimiter))
                .ToList();

            return (rows, headers);
        }

        static List<string[]> SortRows(List<string[]> rows, string[]? headers, Config config)
        {
            IOrderedEnumerable<string[]>? ordered = null;

            foreach (var sf in config.SortFields)
            {
                int idx;

                if (config.HasHeader)
                {
                    idx = Array.IndexOf(headers!, sf.Field);
                    if (idx == -1) throw new Exception($"Columna '{sf.Field}' no encontrada.");
                }
                else idx = int.Parse(sf.Field);

                Func<string[], object> keySelector = row =>
                {
                    var val = row[idx];
                    if (sf.Type == "num" && double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
                        return n;
                    return val;
                };

                if (ordered == null)
                    ordered = sf.Order == "desc" ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);
                else
                    ordered = sf.Order == "desc" ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
            }

            return ordered!.ToList();
        }

        static void WriteOutput(List<string[]> rows, string[]? headers, Config config)
        {
            var lines = new List<string>();

            if (headers != null)
                lines.Add(string.Join(config.Delimiter, headers));

            lines.AddRange(rows.Select(r => string.Join(config.Delimiter, r)));

            if (config.Output != null)
                File.WriteAllLines(config.Output, lines);
            else
                lines.ForEach(Console.WriteLine);
        }

        static void ShowHelp()
        {
            Console.WriteLine("sortx [entrada] [salida] -b campo[:tipo[:orden]]");
        }
    }
}
