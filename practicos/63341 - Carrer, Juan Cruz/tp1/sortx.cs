using   System;
using   System.Collections.Generic;
using   System.IO;
using   System.Linq;
using   System.Text;


record SortField(string Nombre, bool Numerico, bool Descendiente);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);