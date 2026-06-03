using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var config = ParsearArgumentos(args);
            var texto = LeerEntrada(config);
            var filas = ParsearDelimitado(texto, config);
            var filasOrdenadas = OrdenarFilas(filas, config);
            var salida = Serializar(filasOrdenadas, config);
            EscribirSalida(config, salida);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }
    }

    static ConfiguracionApp ParsearArgumentos(string[] args)
    {
        string archivoEntrada = null;
        string archivoSalida = null;
        string delimitador = ",";
        bool sinEncabezado = false;
        var camposOrden = new List<CampoOrden>();

        int cantidadPosicionales = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i":
                case "--input":
                    ValidarSiguiente(args, i);
                    archivoEntrada = args[++i];
                    break;

                case "-o":
                case "--output":
                    ValidarSiguiente(args, i);
                    archivoSalida = args[++i];
                    break;

                case "-d":
                case "--delimiter":
                    ValidarSiguiente(args, i);
                    var d = args[++i];
                    delimitador = d == "\\t" ? "\t" : d;
                    break;

                case "-nh":
                case "--no-header":
                    sinEncabezado = true;
                    break;

                case "-h":
                case "--help":
                    MostrarAyuda();
                    Environment.Exit(0);
                    break;

                case "-b":
                case "--by":
                    ValidarSiguiente(args, i);
                    var partes = args[++i].Split(':');

                    string nombre = partes[0];
                    bool esNumerico = partes.Length > 1 && partes[1] == "num";
                    bool descendente = partes.Length > 2 && partes[2] == "desc";

                    camposOrden.Add(new CampoOrden(nombre, esNumerico, descendente));
                    break;

                default:
                    if (!args[i].StartsWith("-"))
                    {
                        if (cantidadPosicionales == 0)
                            archivoEntrada = args[i];
                        else if (cantidadPosicionales == 1)
                            archivoSalida = args[i];

                        cantidadPosicionales++;
                    }
                    break;
            }
        }

        if (camposOrden.Count == 0)
            throw new Exception("Debe especificar al menos un campo de ordenamiento (-b)");

        return new ConfiguracionApp(
            archivoEntrada,
            archivoSalida,
            delimitador,
            sinEncabezado,
            camposOrden
        );
    }

    static void ValidarSiguiente(string[] args, int i)
    {
        if (i + 1 >= args.Length)
            throw new Exception("Falta valor para argumento " + args[i]);
    }

    static void MostrarAyuda()
    {
        Console.WriteLine("Uso:");
        Console.WriteLine("-i | --input <archivo>");
        Console.WriteLine("-o | --output <archivo>");
        Console.WriteLine("-d | --delimiter <delimitador>");
        Console.WriteLine("-nh | --no-header");
        Console.WriteLine("-b | --by campo[:num][:desc]");
    }

    static string LeerEntrada(ConfiguracionApp config)
    {
        if (config.ArchivoEntrada != null)
            return File.ReadAllText(config.ArchivoEntrada);

        return Console.In.ReadToEnd();
    }

    static string LeerEntrada(ConfiguracionApp config)
    {
        if (config.ArchivoEntrada != null)
            return File.ReadAllText(config.ArchivoEntrada);

        return Console.In.ReadToEnd();
    }

    static List<Dictionary<string, string>> ParsearDelimitado(string texto, ConfiguracionApp config)
    {
        var lineas = texto
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToArray();

        if (lineas.Length == 0)
            throw new Exception("No se encontraron datos de entrada.");

        string[] encabezados;
        int inicio = 0;

        if (!config.SinEncabezado)
        {
            encabezados = lineas[0].Split(new[] { config.Delimitador }, StringSplitOptions.None);
            inicio = 1;
        }
        else
        {
            int cantidadCampos = lineas[0].Split(new[] { config.Delimitador }, StringSplitOptions.None).Length;
            encabezados = Enumerable.Range(0, cantidadCampos).Select(i => i.ToString()).ToArray();
        }

        var filas = new List<Dictionary<string, string>>();

        for (int i = inicio; i < lineas.Length; i++)
        {
            var valores = lineas[i].Split(new[] { config.Delimitador }, StringSplitOptions.None);

            if (valores.Length != encabezados.Length)
                throw new Exception("La fila " + (i + 1) + " tiene una cantidad de campos diferente al encabezado.");

            var fila = new Dictionary<string, string>();

            for (int j = 0; j < encabezados.Length; j++)
            {
                fila[encabezados[j]] = valores[j];
            }

            filas.Add(fila);
        }

        return filas;
    }

    static List<Dictionary<string, string>> OrdenarFilas(List<Dictionary<string, string>> filas, ConfiguracionApp config)
    {
        if (filas.Count == 0)
            return filas;

        IOrderedEnumerable<Dictionary<string, string>> filasOrdenadas = null;

        foreach (var campo in config.CamposOrden)
        {
            if (filas.Any(f => !f.ContainsKey(campo.Nombre)))
                throw new Exception("El campo '" + campo.Nombre + "' no existe en todas las filas.");

            Func<Dictionary<string, string>, object> selector = fila =>
            {
                var valor = fila[campo.Nombre];

                if (campo.EsNumerico)
                {
                    double numero;
                    if (!double.TryParse(valor, out numero))
                        throw new Exception("Valor '" + valor + "' no es numérico en campo '" + campo.Nombre + "'");

                    return numero;
                }

                return valor;
            };

            if (filasOrdenadas == null)
            {
                filasOrdenadas = campo.Descendente
                    ? filas.OrderByDescending(selector)
                    : filas.OrderBy(selector);
            }
            else
            {
                filasOrdenadas = campo.Descendente
                    ? filasOrdenadas.ThenByDescending(selector)
                    : filasOrdenadas.ThenBy(selector);
            }
        }

        return filasOrdenadas != null ? filasOrdenadas.ToList() : filas;
    }

    static string Serializar(List<Dictionary<string, string>> filas, ConfiguracionApp config)
    {
        if (filas.Count == 0)
            return string.Empty;

        var encabezados = filas[0].Keys.ToArray();
        var lineas = new List<string>();

        if (!config.SinEncabezado)
        {
            lineas.Add(string.Join(config.Delimitador, encabezados));
        }

        foreach (var fila in filas)
        {
            lineas.Add(string.Join(config.Delimitador, encabezados.Select(e => fila[e])));
        }

        return string.Join("\n", lineas);
    }

    static void EscribirSalida(ConfiguracionApp config, string salida)
    {
        if (config.ArchivoSalida != null)
            File.WriteAllText(config.ArchivoSalida, salida);
        else
            Console.WriteLine(salida);
    }
}

class CampoOrden
{
    public string Nombre { get; private set; }
    public bool EsNumerico { get; private set; }
    public bool Descendente { get; private set; }

    public CampoOrden(string nombre, bool esNumerico, bool descendente)
    {
        Nombre = nombre;
        EsNumerico = esNumerico;
        Descendente = descendente;
    }
}

class ConfiguracionApp
{
    public string ArchivoEntrada { get; private set; }
    public string ArchivoSalida { get; private set; }
    public string Delimitador { get; private set; }
    public bool SinEncabezado { get; private set; }
    public List<CampoOrden> CamposOrden { get; private set; }

    public ConfiguracionApp(
        string archivoEntrada,
        string archivoSalida,
        string delimitador,
        bool sinEncabezado,
        List<CampoOrden> camposOrden)
    {
        ArchivoEntrada = archivoEntrada;
        ArchivoSalida = archivoSalida;
        Delimitador = delimitador;
        SinEncabezado = sinEncabezado;
        CamposOrden = camposOrden;
    }
}

    