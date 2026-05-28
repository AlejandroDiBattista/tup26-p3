using System.Globalization;
var runner = new DatasetProcessor();
runner.Execute(args);
public class DatasetProcessor
{
     public void Execute(string[] args)
    {
        try
        {
            var settings = CommandLineParser.Analyze(args);
            var (headers, records) = LoadAndParse(settings);
            
            var sortedRecords = ApplySorting(records, headers, settings);
            
            var formattedOutput = BuildOutputString(headers, sortedRecords, settings);
            DispatchOutput(formattedOutput, settings.TargetFile);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            Environment.ExitCode = 1;
        }
    }
      private (List<string> Headers, List<Dictionary<string, string>> Records) LoadAndParse(RuntimeSettings settings)
    {
        string rawData = settings.SourceFile is null 
            ? Console.In.ReadToEnd() 
            : (File.Exists(settings.SourceFile) ? File.ReadAllText(settings.SourceFile) : throw new FileNotFoundException($"No existe el archivo de entrada '{settings.SourceFile}'"));

        var textLines = rawData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (textLines.Count == 0) return ([], []);

        var structureLines = settings.HasNoHeader ? textLines : textLines.Skip(1);
        var Matrix = structureLines.Select(ln => ln.Split([settings.Separator], StringSplitOptions.None)).ToList();

        var headers = settings.HasNoHeader
            ? Enumerable.Range(0, Matrix.DefaultIfEmpty([]).Max(row => row.Length)).Select(idx => idx.ToString()).ToList()
            : textLines[0].Split([settings.Separator], StringSplitOptions.None).ToList();

        if (!settings.HasNoHeader && headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count)
        {
            throw new InvalidOperationException("El encabezado contiene columnas repetidas");
        }
        var records = Matrix.Select(parts => {
            if (!settings.HasNoHeader && parts.Length > headers.Count)
                throw new InvalidOperationException("Una fila tiene mas columnas que el encabezado");

            var rowMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                rowMap[headers[i]] = i < parts.Length ? parts[i] : string.Empty;
            }
            return rowMap;
        }).ToList();

        return (headers, records);
    }
     private List<Dictionary<string, string>> ApplySorting(List<Dictionary<string, string>> records, List<string> headers, RuntimeSettings settings)
    {
        var criteria = new List<SortCriteria>();

        foreach (var rule in settings.OrderingRules)
        {
            if (settings.HasNoHeader)
            {
                if (!int.TryParse(rule.Identifier, out var pos) || pos < 0 || pos >= headers.Count)
                    throw new InvalidOperationException($"La columna '{rule.Identifier}' no existe");
                
                criteria.Add(rule with { Identifier = headers[pos] });
            }
            else
            {
                var matchedHeader = headers.FirstOrDefault(h => string.Equals(h, rule.Identifier, StringComparison.OrdinalIgnoreCase)) 
                    ?? throw new InvalidOperationException($"La columna '{rule.Identifier}' no existe");
                
                criteria.Add(rule with { Identifier = matchedHeader });
            }
        }

        records.Sort((rowX, rowY) => {
            foreach (var rule in criteria)
            {
                var valX = rowX.TryGetValue(rule.Identifier, out var x) ? x : string.Empty;
                var valY = rowY.TryGetValue(rule.Identifier, out var y) ? y : string.Empty;

                int outcome = rule.IsNumerical
                    ? ConvertToDecimal(valX, rule.Identifier).CompareTo(ConvertToDecimal(valY, rule.Identifier))
                    : StringComparer.OrdinalIgnoreCase.Compare(valX, valY);

                if (outcome != 0) 
                    return rule.ReverseOrder ? -outcome : outcome;
            }
            return 0;
        });

        return records;
    }
     private string BuildOutputString(List<string> headers, List<Dictionary<string, string>> records, RuntimeSettings settings)
    {
        var outputLines = records
            .Select(row => string.Join(settings.Separator, headers.Select(h => row.TryGetValue(h, out var v) ? v : string.Empty)))
            .ToList();

        if (!settings.HasNoHeader && headers.Count > 0)
        {
            outputLines.Insert(0, string.Join(settings.Separator, headers));
        }

        return string.Join(Environment.NewLine, outputLines);
    }
    private void DispatchOutput(string data, string? targetPath)
    {
        if (targetPath is null) Console.Write(data);
        else File.WriteAllText(targetPath, data);
    }
}