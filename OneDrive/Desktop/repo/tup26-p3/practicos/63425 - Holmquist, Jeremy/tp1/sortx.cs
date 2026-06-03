using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

var listaArgumentos = Environment.GetCommandLineArgs().Skip(1).ToArray();

try
{
    var configuracion = ParseArgumentos(listaArgumentos);
    if (configuracion == null) return 0;
    
    string texto = LeerEntrada(configuracion.ArchivoEntrada);
    var (encabezado, filas) = ParseDelimitado(texto, configuracion);
    var filasOrdenadas = OrdenarFilas(filas, configuracion.CamposOrdenamiento, encabezado, configuracion.SinEncabezado);
    string salida = Serializar(encabezado, filasOrdenadas, configuracion);
    EscribirSalida(configuracion.ArchivoSalida, salida);
    
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

Configuracion? ParseArgumentos(string[] parametros)
{
    string? archivoEntrada = null;
    string? archivoSalida = null;
    string delimitador = ",";
    bool sinEncabezado = false;
    var camposOrdenamiento = new List<CampoOrdenamiento>();

    for (int i = 0; i < parametros.Length; i++)
    {
        string argumento = parametros[i];

        if (argumento == "--help" || argumento == "-h")
        {
            MostrarAyuda();
            return null;
        }
        else if (argumento == "--no-header" || argumento == "-nh")
        {
            sinEncabezado = true;
        }
        else if (argumento == "--delimiter" || argumento == "-d")
        {
            if (i + 1 >= parametros.Length)
                throw new ArgumentException("Se requiere valor para --delimiter");
            
            string delim = parametros[++i];
            delimitador = delim == "\\t" ? "\t" : delim;
        }
        else if (argumento == "--by" || argumento == "-b")
        {
            if (i + 1 >= parametros.Length)
                throw new ArgumentException("Se requiere valor para --by");
            
            var campo = ParseCampoOrdenamiento(parametros[++i]);
            camposOrdenamiento.Add(campo);
        }
        else if (argumento == "--input" || argumento == "-i")
        {
            if (i + 1 >= parametros.Length)
                throw new ArgumentException("Se requiere valor para --input");
            
            archivoEntrada = parametros[++i];
        }
        else if (argumento == "--output" || argumento == "-o")
        {
            if (i + 1 >= parametros.Length)
                throw new ArgumentException("Se requiere valor para --output");
            
            archivoSalida = parametros[++i];
        }
        else if (!argumento.StartsWith("-"))
        {
            if (archivoEntrada == null)
                archivoEntrada = argumento;
            else if (archivoSalida == null)
                archivoSalida = argumento;
        }
        else
        {
            throw new ArgumentException($"Opción desconocida: {argumento}");
        }
    }

    if (camposOrdenamiento.Count == 0)
        throw new ArgumentException("Se requiere al menos un campo de ordenamiento (--by)");

    return new Configuracion(archivoEntrada, archivoSalida, delimitador, sinEncabezado, camposOrdenamiento);
}

CampoOrdenamiento ParseCampoOrdenamiento(string especificacion)
{
    var partes = especificacion.Split(':');
    string nombre = partes[0];
    bool esNumerico = false;
    bool esDescendente = false;

    if (partes.Length > 1)
    {
        string tipo = partes[1].ToLower();
        if (tipo == "num")
            esNumerico = true;
        else if (tipo != "alpha")
            throw new ArgumentException($"Tipo inválido: {tipo}. Use 'alpha' o 'num'");
    }

    if (partes.Length > 2)
    {
        string orden = partes[2].ToLower();
        if (orden == "desc")
            esDescendente = true;
        else if (orden != "asc")
            throw new ArgumentException($"Orden inválido: {orden}. Use 'asc' o 'desc'");
    }

    return new CampoOrdenamiento(nombre, esNumerico, esDescendente);
}

void MostrarAyuda()
{
    Console.WriteLine(@"
sortx - Ordenar archivos delimitados

Sintaxis:
  sortx [entrada [salida]] [-b|--by campo[:tipo[:orden]]]...

Opciones:
  -b, --by CAMPO[:TIPO[:ORDEN]]
  -i, --input ARCHIVO
  -o, --output ARCHIVO
  -d, --delimiter DELIM
  -nh, --no-header
  -h, --help
");
}

string LeerEntrada(string? archivoEntrada)
{
    if (archivoEntrada == null)
        return Console.In.ReadToEnd();
    
    if (!File.Exists(archivoEntrada))
        throw new FileNotFoundException($"El archivo '{archivoEntrada}' no se encontró");
    
    return File.ReadAllText(archivoEntrada);
}

(List<string>? encabezados, List<Dictionary<string, string>> filas) ParseDelimitado(string texto, Configuracion configuracion)
{
    var lineas = texto.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    
    if (lineas.Length == 0)
        return (null, new List<Dictionary<string, string>>());

    List<string>? encabezados = null;
    var filas = new List<Dictionary<string, string>>();

    if (!configuracion.SinEncabezado)
    {
        encabezados = lineas[0].Split(configuracion.Delimitador.ToCharArray()).ToList();
        lineas = lineas.Skip(1).ToArray();
    }

    foreach (var linea in lineas)
    {
        if (string.IsNullOrWhiteSpace(linea))
            continue;

        var valores = linea.Split(configuracion.Delimitador.ToCharArray());
        var fila = new Dictionary<string, string>();

        if (encabezados != null)
        {
            for (int i = 0; i < encabezados.Count && i < valores.Length; i++)
                fila[encabezados[i]] = valores[i];
        }
        else
        {
            for (int i = 0; i < valores.Length; i++)
                fila[i.ToString()] = valores[i];
        }

        filas.Add(fila);
    }

    return (encabezados, filas);
}

List<Dictionary<string, string>> OrdenarFilas(
    List<Dictionary<string, string>> filas, 
    List<CampoOrdenamiento> camposOrdenamiento,
    List<string>? encabezados,
    bool sinEncabezado)
{
    foreach (var campo in camposOrdenamiento)
    {
        bool existe = false;

        if (sinEncabezado)
        {
            existe = int.TryParse(campo.Nombre, out _);
        }
        else
        {
            existe = encabezados != null && encabezados.Contains(campo.Nombre);
        }

        if (!existe)
            throw new ArgumentException($"El campo '{campo.Nombre}' no existe");
    }

    IOrderedEnumerable<Dictionary<string, string>> filasProcesadas = null!;

    for (int i = 0; i < camposOrdenamiento.Count; i++)
    {
        var campo = camposOrdenamiento[i];

        if (i == 0)
        {
            filasProcesadas = campo.EsNumerico
                ? (campo.EsDescendente
                    ? filas.OrderByDescending(f => ObtenerValor(f, campo.Nombre, true))
                    : filas.OrderBy(f => ObtenerValor(f, campo.Nombre, true)))
                : (campo.EsDescendente
                    ? filas.OrderByDescending(f => ObtenerValor(f, campo.Nombre, false))
                    : filas.OrderBy(f => ObtenerValor(f, campo.Nombre, false)));
        }
        else
        {
            filasProcesadas = campo.EsNumerico
                ? (campo.EsDescendente
                    ? filasProcesadas.ThenByDescending(f => ObtenerValor(f, campo.Nombre, true))
                    : filasProcesadas.ThenBy(f => ObtenerValor(f, campo.Nombre, true)))
                : (campo.EsDescendente
                    ? filasProcesadas.ThenByDescending(f => ObtenerValor(f, campo.Nombre, false))
                    : filasProcesadas.ThenBy(f => ObtenerValor(f, campo.Nombre, false)));
        }
    }

    return filasProcesadas.ToList();
}

dynamic ObtenerValor(Dictionary<string, string> fila, string nombreCampo, bool esNumerico)
{
    string valor = fila.ContainsKey(nombreCampo) ? fila[nombreCampo] : "0";
    return esNumerico 
        ? (decimal.TryParse(valor, out var v) ? v : 0)
        : (valor ?? "");
}

string Serializar(List<string>? encabezados, List<Dictionary<string, string>> filas, Configuracion configuracion)
{
    var lineas = new List<string>();

    if (encabezados != null)
        lineas.Add(string.Join(configuracion.Delimitador, encabezados));

    foreach (var fila in filas)
    {
        var valores = new List<string>();

        if (encabezados != null)
        {
            foreach (var encabezado in encabezados)
                valores.Add(fila.ContainsKey(encabezado) ? fila[encabezado] : "");
        }
        else
        {
            for (int i = 0; i < fila.Count; i++)
                valores.Add(fila.ContainsKey(i.ToString()) ? fila[i.ToString()] : "");
        }

        lineas.Add(string.Join(configuracion.Delimitador, valores));
    }

    return string.Join("\n", lineas);
}

void EscribirSalida(string? archivoSalida, string texto)
{
    if (archivoSalida == null)
        Console.Out.Write(texto);
    else
        File.WriteAllText(archivoSalida, texto);
}

record CampoOrdenamiento(string Nombre, bool EsNumerico, bool EsDescendente);
record Configuracion(string? ArchivoEntrada, string? ArchivoSalida, string Delimitador, bool SinEncabezado, List<CampoOrdenamiento> CamposOrdenamiento);