using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

try
{
    Console.WriteLine("Inicio del programa");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}

record Criterio(string Columna, bool EsNumero, bool EsDesc);

record Opciones(
    string? Entrada,
    string? Salida,
    string Separador,
    bool SinCabecera,
    List<Criterio> Criterios
);