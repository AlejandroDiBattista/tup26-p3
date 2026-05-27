
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

//Modelo de configuración
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
record SortField(string Name, bool Numeric, bool Descending);