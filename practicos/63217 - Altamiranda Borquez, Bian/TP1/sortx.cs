using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var config = ParseArgs(args);

            var text = ReadInput(config);

            var rows = ParseDelimited(text, config);

            var sorted = SortRows(rows, config);

            var output = Serialize(sorted, config);

            WriteOutput(output, config);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
