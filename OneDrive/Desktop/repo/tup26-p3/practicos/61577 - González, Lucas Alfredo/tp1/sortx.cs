

// Programa sortx para ordenar archivos CSV
// Lucas Gonzalez - TP1

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Configuracion del programa
record Configuracion(
    string ArchivoEntrada,
    string ArchivoSalida,
    char Delimitador,
    bool TieneEncabezado,
    List<(string Campo, string Tipo, string Orden)> CriteriosOrdenamiento
);

class Program
{
    static void Main(string[] args)
    {
        // Mostrar ayuda si se pide
        if (args.Contains("-h") || args.Contains("--help"))
        {
            MostrarAyuda();
            return;
        }

        // Parsear argumentos
        var config = ParsearArgumentos(args);

        // Leer el archivo
        var lineas = LeerArchivo(config.ArchivoEntrada);

        // Ordenar las lineas
        var lineasOrdenadas = OrdenarLineas(lineas, config);

        // Escribir el resultado
        EscribirArchivo(config.ArchivoSalida, lineasOrdenadas);
    }

    static void MostrarAyuda()
    {
        Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
        Console.WriteLine("Opciones:");
        Console.WriteLine("  -b, --by campo[:tipo[:orden]]  Campo para ordenar");
        Console.WriteLine("  -i, --input archivo           Archivo de entrada");
        Console.WriteLine("  -o, --output archivo          Archivo de salida");
        Console.WriteLine("  -d, --delimiter char          Delimitador (default: ,)");
        Console.WriteLine("  -nh, --no-header              Sin encabezado");
        Console.WriteLine("  -h, --help                    Mostrar esta ayuda");
    }

    static Configuracion ParsearArgumentos(string[] args)
    {
        string entrada = null;
        string salida = null;
        char delimitador = ',';
        bool tieneEncabezado = true;
        var criterios = new List<(string, string, string)>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i":
                case "--input":
                    entrada = args[++i];
                    break;
                case "-o":
                case "--output":
                    salida = args[++i];
                    break;
                case "-d":
                case "--delimiter":
                    delimitador = args[++i][0];
                    break;
                case "-nh":
                case "--no-header":
                    tieneEncabezado = false;
                    break;
                case "-b":
                case "--by":
                    var partes = args[++i].Split(':');
                    string campo = partes[0];
                    string tipo = partes.Length > 1 ? partes[1] : "alpha";
                    string orden = partes.Length > 2 ? partes[2] : "asc";
                    criterios.Add((campo, tipo, orden));
                    break;
                default:
                    if (entrada == null) entrada = args[i];
                    else if (salida == null) salida = args[i];
                    break;
            }
        }

        return new Configuracion(entrada, salida, delimitador, tieneEncabezado, criterios);
    }

    static List<string> LeerArchivo(string archivo)
    {
        return File.ReadAllLines(archivo).ToList();
    }

    static void EscribirArchivo(string archivo, List<string> lineas)
    {
        File.WriteAllLines(archivo, lineas);
    }

    static List<string> OrdenarLineas(List<string> lineas, Configuracion config)
    {
        if (lineas.Count == 0) return lineas;

        var encabezado = config.TieneEncabezado ? lineas[0] : null;
        var datos = config.TieneEncabezado ? lineas.Skip(1).ToList() : lineas;
        var encabezadoArray = config.TieneEncabezado ? lineas[0].Split(config.Delimitador) : null;

        // Ordenar datos
        datos.Sort((a, b) => CompararLineas(a, b, config, encabezadoArray));

        var resultado = new List<string>();
        if (encabezado != null) resultado.Add(encabezado);
        resultado.AddRange(datos);
        return resultado;
    }

    static int CompararLineas(string a, string b, Configuracion config, string[] encabezadoArray)
    {
        var camposA = a.Split(config.Delimitador);
        var camposB = b.Split(config.Delimitador);

        foreach (var criterio in config.CriteriosOrdenamiento)
        {
            int indice = ObtenerIndiceCampo(criterio.Campo, encabezadoArray);
            if (indice < 0 || indice >= camposA.Length || indice >= camposB.Length) continue;

            int comparacion = CompararCampos(camposA[indice], camposB[indice], criterio.Tipo);
            if (comparacion != 0)
            {
                return criterio.Orden == "desc" ? -comparacion : comparacion;
            }
        }
        return 0;
    }

    static int ObtenerIndiceCampo(string campo, string[] encabezado)
    {
        if (encabezado == null)
        {
            return int.TryParse(campo, out int indice) ? indice : -1;
        }
        return Array.IndexOf(encabezado, campo);
    }

    static int CompararCampos(string a, string b, string tipo)
    {
        if (tipo == "num")
        {
            if (double.TryParse(a, out double numA) && double.TryParse(b, out double numB))
            {
                return numA.CompareTo(numB);
            }
        }
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
