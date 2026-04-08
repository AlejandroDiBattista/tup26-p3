using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

try
{ var config= Parse(args);
  var input= ReadInput(config);
  var rows= ParseDelimited(TextReader,config);
  var sorted = SortedRows(rows,config);
  var output= Serialize(sorted,config);
   WriteOutput(output, config);
}
catch(Exception ex)
{
    Console.Error.WriteLine($"Error:{ex.Message}");
    Environment.Exit(1);
}
