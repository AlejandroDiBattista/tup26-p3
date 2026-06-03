using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

try{
    var config = ParseArgs(args);

    var text = ReadInput(config);

    var rows = ParseDelimited(text, config);

    var sorted = SortRows(rows, config);

    var output = Serialize(sorted, config);

    WriteOutput(output, config);
}

catch (Exception ex){
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);    
}

AppConfig ParseArgs(string[] args){
   string? input = null;
   string? output = null;
   string delimiter = ",";
   bool noHeader = false;
   var sortFields = new List<SortField>();

   for(int i = 0; i <args.Length; i++){
        var arg = args[i];

        if (arg == "-i" || arg == "--input")
            input = args[++i];

        else if (arg == "-o" || arg == "--output")
            output = args[++i];

        else if (arg == "-d" || arg == "--delimiter")
        {
            var val = args[++i];
            delimiter = val == "\\t" ? "\t" : val;
        }

        else if (arg == "-nh" || arg == "--no-header")
            noHeader = true;

        else if (arg == "-b" || arg == "--by")
            sortFields.Add(ParseSortField(args[++i]));

        else if (arg == "-h" || arg == "--help")
        {
            PrintHelp();
            Environment.Exit(0);
        }

        else if (arg.StartsWith("-"))
            throw new Exception($"Argumento inválido: {arg}");

        else
        {
            if (input == null) input = arg;
            else if (output == null) output = arg;
        }
    }

    if (!sortFields.Any())
        throw new Exception("Debe especificar al menos un criterio de orden (-b)");

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

string ReadInput(AppConfig config){
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        if (!File.Exists(config.InputFile))
            throw new Exception("El archivo de entrada no existe");

        return File.ReadAllText(config.InputFile);
    }

    if (!Console.IsInputRedirected)
        throw new Exception("No hay entrada por stdin");

    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config){
      var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
         if (lines.Length == 0)
        return new();

      var table = new List<Dictionary<string, string>>();
          if (lines.Length == 0)
        return new();

    var table = new List<Dictionary<string, string>>();
}

