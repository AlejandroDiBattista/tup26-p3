using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

try{
  var config = ParseArgs(args);
}
catch (Exception ex){
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
AppConfig ParseArgs(string[] args) {
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();
    for (int i = 0; i < args.Length; i++){
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
        else if (arg == "-h" || arg == "--help"){
            PrintHelp();
            Environment.Exit(0);
        }
    }
    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}