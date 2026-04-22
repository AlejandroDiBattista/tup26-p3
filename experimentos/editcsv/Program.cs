using EditCsv;

var options = CommandLineOptions.Parse(args);

if (options.ShowHelp || string.IsNullOrWhiteSpace(options.FilePath))
{
    CommandLineOptions.PrintHelp();
    return;
}

var document = CsvDocument.Load(options.FilePath, options.HasHeader, options.Delimiter);
var app = new CsvEditorApp(document);

app.Run();
