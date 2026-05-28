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
    
}