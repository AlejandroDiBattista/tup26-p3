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
}