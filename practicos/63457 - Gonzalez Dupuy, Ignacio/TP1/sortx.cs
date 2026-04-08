using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

Opciones LeerArgumentos(string[] args)
{
    string? entrada = null;
    string? salida = null;
    var criterios = new List<Criterio>();
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "-b")
        {
            var col = args[++i];
            criterios.Add(new Criterio(col, false, false));
        }
        else if (entrada == null)
            entrada = args[i];
        else if (salida == null)
            salida = args[i];
    }
    return new Opciones(entrada, salida, ",", false, criterios);
}
try
{
    var opciones = LeerArgumentos(args);
    Console.WriteLine(opciones.Entrada);
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