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
            var partes = args[++i].Split(':');
            string columna = partes[0];
            bool esNumero = partes.Length > 1 && partes[1] == "num";
            bool esDesc = partes.Length > 2 && partes[2] == "desc";
            criterios.Add(new Criterio(columna, esNumero, esDesc));
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
    IOrderedEnumerable<string[]>? ordenadas = null;
    foreach (var criterio in op.Criterios)
    {
        int col = Array.FindIndex(cabecera, c => c.Trim() == criterio.Columna);
        if (col == -1)
            throw new Exception($"La columna '{criterio.Columna}' no existe");
        if (criterio.EsNumero)
        {
            if (ordenadas == null)
            {
                ordenadas = criterio.EsDesc
                    ? datos.OrderByDescending(f => int.Parse(f[col]))
                    : datos.OrderBy(f => int.Parse(f[col]));
            }
            else
            {
                ordenadas = criterio.EsDesc
                    ? ordenadas.ThenByDescending(f => int.Parse(f[col]))
                    : ordenadas.ThenBy(f => int.Parse(f[col]));
            }
        }
        else
        {
            if (ordenadas == null)
            {
                ordenadas = criterio.EsDesc
                    ? datos.OrderByDescending(f => f[col])
                    : datos.OrderBy(f => f[col]);
            }
            else
            {
                ordenadas = criterio.EsDesc
                    ? ordenadas.ThenByDescending(f => f[col])
                    : ordenadas.ThenBy(f => f[col]);
            }
        }
    }
    var resultado = new List<string[]> { cabecera };
    resultado.AddRange(ordenadas != null ? ordenadas.ToList() : datos);
    return resultado;
}
string ArmarTexto(List<string[]> filas, Opciones op)
{
    return string.Join("\n", filas.Select(f => string.Join(op.Separador, f)));
}
void MostrarResultado(string texto, Opciones op)
{
    if (op.Salida != null)
        File.WriteAllText(op.Salida, texto);
    else
        Console.WriteLine(texto);
}
try
{
    var opciones = LeerArgumentos(args);
    var texto = LeerTexto(opciones);
    var filas = SepararFilas(texto, opciones);
    var ordenadas = Ordenar(filas, opciones);
    var salida = ArmarTexto(ordenadas, opciones);
    MostrarResultado(salida, opciones);
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