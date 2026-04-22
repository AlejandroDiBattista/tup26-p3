using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
try
{
    var config = LeerArgumentos(args);

    if (config == null) return;

    var texto = LeerEntrada(config);
    var (filas, encabezado) = ParsearTexto(texto, config);
    var ordenado = OrdenarFilas(filas, config);
    var salida = ConvertirTexto(ordenado, encabezado, config);
    EscribirSalida(salida, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}
Configuracion LeerArgumentos(string[] args)
{
    string? entrada = null;
    string? salida = null;
    string delimitador = ",";
    bool sinEncabezado = false;
    bool ayuda = false;
    var campos = new List<CampoOrden>();
    int i = 0;
    while (i < args.Length)
    {
        var arg = args[i];

        if (arg == "-h" || arg == "--help")
        {
            ayuda = true;
            MostrarAyuda();
            return null;
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            sinEncabezado = true;
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            i++;
            delimitador = args[i] == "\\t" ? "\t" : args[i];
        }
        else if (arg == "-i" || arg == "--input")
        {
            i++;
            entrada = args[i];
        }
        else if (arg == "-o" || arg == "--output")
        {
            i++;
            salida = args[i];
        }
        else if (arg == "-b" || arg == "--by")
        {
            i++;
            campos.Add(ParsearCampo(args[i]));
        }
        else if (!arg.StartsWith("-"))
        {
            if (entrada == null) entrada = arg;
            else if (salida == null) salida = arg;
        }

        i++;
    }

    if (campos.Count == 0)
        throw new Exception("Debe usar -b para ordenar");

    return new Configuracion(entrada, salida, delimitador, sinEncabezado, campos, ayuda);
}
CampoOrden ParsearCampo(string texto)
{
    var partes = texto.Split(':');
    string nombre = partes[0];
    bool esNumero = false;
    bool desc = false;

    if (partes.Length > 1)
        esNumero = partes[1] == "num";
    if (partes.Length > 2)
        desc = partes[2] == "desc";
    return new CampoOrden(nombre, esNumero, desc);
}
void MostrarAyuda()
{
    Console.WriteLine("Uso: sortx [entrada [salida]] -b campo[:tipo[:orden]]");
}
string LeerEntrada(Configuracion config)
{
    if (config.Entrada != null)
        return File.ReadAllText(config.Entrada);

    return Console.In.ReadToEnd();
}
(List<Dictionary<string, string>>, List<string>?) ParsearTexto(string texto, Configuracion config)
{
    var lineas = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Select(l => l.Trim()).ToList();

    if (lineas.Count == 0)
        throw new Exception("Archivo vacío");

    List<string> encabezados;
    int inicio = 0;
    if (!config.SinEncabezado)
    {
        encabezados = lineas[0].Split(config.Delimitador).ToList();
        inicio = 1;
    }
    else
    {
        int cantidad = lineas[0].Split(config.Delimitador).Length;
        encabezados = Enumerable.Range(0, cantidad).Select(i => i.ToString()).ToList();
    }
    var filas = new List<Dictionary<string, string>>();
    for (int i = inicio; i < lineas.Count; i++)
    {
        var valores = lineas[i].Split(config.Delimitador);

        var dic = new Dictionary<string, string>();

        for (int j = 0; j < encabezados.Count; j++)
        {
            dic[encabezados[j]] = j < valores.Length ? valores[j] : "";
        }
        filas.Add(dic);
    }
    return (filas, config.SinEncabezado ? null : encabezados);
}
List<Dictionary<string, string>> OrdenarFilas(List<Dictionary<string, string>> filas, Configuracion config)
{
    IOrderedEnumerable<Dictionary<string, string>>? ordenado = null;
    foreach (var campo in config.Campos)
    {
        Func<Dictionary<string, string>, object> clave = fila =>
        {
            if (!fila.ContainsKey(campo.Nombre))
                throw new Exception($"Columna inexistente: {campo.Nombre}");

            var valor = fila[campo.Nombre];

            if (campo.EsNumero)
                return double.TryParse(valor, out var num) ? num : 0;
            return valor;
        };

        if (ordenado == null)
        { ordenado = campo.Desc
                ? filas.OrderByDescending(clave)
                : filas.OrderBy(clave); }
        else
        { ordenado = campo.Desc
                ? ordenado.ThenByDescending(clave)
                : ordenado.ThenBy(clave);  }
    }
    return ordenado!.ToList();
}
string ConvertirTexto(List<Dictionary<string, string>> filas, List<string>? encabezado, Configuracion config)
{
    var lineas = new List<string>();

    if (encabezado != null)
        lineas.Add(string.Join(config.Delimitador, encabezado));

    foreach (var fila in filas)
    {
        var linea = string.Join(config.Delimitador, fila.Values);
        lineas.Add(linea);
    }

    return string.Join("\n", lineas);
}
void EscribirSalida(string texto, Configuracion config)
{
    if (config.Salida != null)
        File.WriteAllText(config.Salida, texto);
    else
        Console.WriteLine(texto);
}
record CampoOrden(string Nombre, bool EsNumero, bool Desc);
record Configuracion( string? Entrada,string? Salida,string Delimitador,bool SinEncabezado,List<CampoOrden> Campos,bool Ayuda);