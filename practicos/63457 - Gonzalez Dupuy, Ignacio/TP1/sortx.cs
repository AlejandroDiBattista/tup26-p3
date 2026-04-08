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
string LeerTexto(Opciones op)
{
    return op.Entrada != null
        ? File.ReadAllText(op.Entrada)
        : Console.In.ReadToEnd();
}
List<string[]> SepararFilas(string texto, Opciones op)
{
    return texto
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(l => l.Split(op.Separador))
        .ToList();
}
List<string[]> Ordenar(List<string[]> filas, Opciones op)
{
    var cabecera = filas[0];
    var datos = filas.Skip(1).ToList();
    int col = Array.IndexOf(cabecera, op.Criterios[0].Columna);
    var ordenadas = datos.OrderBy(f => f[col]).ToList();
    var resultado = new List<string[]> { cabecera };
    resultado.AddRange(ordenadas);
    return resultado;
}
try
{
    var opciones = LeerArgumentos(args);
    var texto = LeerTexto(opciones);
    var filas = SepararFilas(texto, opciones);
    var ordenadas = Ordenar(filas, opciones);
    foreach (var fila in ordenadas)
        Console.WriteLine(string.Join(",", fila));
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