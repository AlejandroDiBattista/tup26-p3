
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

try
{
    var config = ObtenerConfiguracion(args);
    var lineas = LeerArchivo(config.Entrada);
    var ordenadas = OrdenarLineas(lineas, config);
    GuardarArchivo(config.Salida, ordenadas);

    Console.WriteLine("Archivo procesado correctamente.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

static Configuracion ObtenerConfiguracion(string[] args)
{
    string? entrada = null;
    string? salida = null;
    bool descendente = false;

    foreach (var arg in args)
    {
        if (arg.StartsWith("--entrada="))
            entrada = arg.Replace("--entrada=", "");

        else if (arg.StartsWith("--salida="))
            salida = arg.Replace("--salida=", "");

        else if (arg == "--desc")
            descendente = true;

        else if (arg == "--help" || arg == "-h")
        {
            MostrarAyuda();
            Environment.Exit(0);
        }
    }

    if (entrada == null || salida == null)
        throw new ArgumentException("Debe indicar --entrada y --salida");

    return new Configuracion(entrada, salida, descendente, new List<CampoOrden>());
}

static List<string> LeerArchivo(string ruta)
{
    if (!File.Exists(ruta))
        throw new FileNotFoundException("El archivo indicado no existe.");

    return File.ReadAllLines(ruta).ToList();
}

static void GuardarArchivo(string ruta, List<string> lineas)
{
    File.WriteAllLines(ruta, lineas);
}

static List<string> OrdenarLineas(List<string> lineas, Configuracion config)
{
    if (config.Descendente)
        return lineas.OrderByDescending(x => x).ToList();

    return lineas.OrderBy(x => x).ToList();
}

static void MostrarAyuda()
{
    Console.WriteLine("Uso del programa:");
    Console.WriteLine("  ordenador --entrada=archivo.txt --salida=resultado.txt [--desc]");
    Console.WriteLine();
    Console.WriteLine("Opciones:");
    Console.WriteLine("  --entrada=RUTA     Archivo de entrada");
    Console.WriteLine("  --salida=RUTA      Archivo de salida");
    Console.WriteLine("  --desc             Orden descendente");
    Console.WriteLine("  -h, --help         Mostrar ayuda");
}

record CampoOrden(string Nombre, bool Descendente);

record Configuracion(string Entrada, string Salida, bool Descendente, List<CampoOrden> Campos);
