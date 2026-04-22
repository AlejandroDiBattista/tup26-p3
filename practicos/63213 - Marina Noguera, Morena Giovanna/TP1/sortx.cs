using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//Flujo Principal del Programa
try
{
    var config = ParseArgs(args);
    Console.WriteLine("Configuración cargada correctamente.");
    Console.WriteLine($" > Entrada: {config.InputFile ?? "(teclado/stdin)"}");
    Console.WriteLine($" > Salida:  {config.OutputFile ?? "(pantalla/stdout)"}");
    Console.WriteLine($" > Separador: '{config.Delimiter}'");
    Console.WriteLine($" > Reglas de orden: {config.SortFields.Count}");
    
    string textoCrudo = ReadInput(config);
    Console.WriteLine("Lectura exitosa.");
    Console.WriteLine($" > Caracteres leídos: {textoCrudo.Length}");
    
    var (headers, filas) = ParseDelimited(textoCrudo, config);
    Console.WriteLine(" Procesamiento exitoso.");
    Console.WriteLine($" > Columnas detectadas: {headers.Length} ({string.Join(", ", headers)})");
    Console.WriteLine($" > Filas de datos: {filas.Count}");

    var filasOrdenadas = SortRows(filas, config);
    Console.WriteLine("Ordenamiento completado.");
    Console.WriteLine($" > Registros ordenados: {filasOrdenadas.Count}");

    WriteOutput(headers, filasOrdenadas, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"\n¡Ups! Algo salió mal: {ex.Message}");
    Environment.Exit(1);
}

//funcion ParseArgs
AppConfig ParseArgs(string[] argumentos)
{
    string? input = null;
    string? output = null;
    string separador = ",";
    bool sinEncabezado = false;
    var reglas = new List<SortField>();

    for (int i = 0; i < argumentos.Length; i++)
    {
        string actual = argumentos[i];
        switch (actual)
        {
            case "-h":
            case "--help":
                MostrarAyuda();
                Environment.Exit(0);
                break;

            case "-d":
            case "--delimiter":
                separador = argumentos[++i].Replace("\\t", "\t");
                break;

            case "-nh":
            case "--no-header":
                sinEncabezado = true;
                break;

            case "-b":
            case "--by":
                // Desglosamos el formato campo:tipo:orden
                var partes = argumentos[++i].Split(':');
                string nombreColumna = partes[0];
                bool esNumerico = partes.Length > 1 && partes[1].ToLower() == "num";
                bool esDescendente = partes.Length > 2 && partes[2].ToLower() == "desc";

                reglas.Add(new SortField(nombreColumna, esNumerico, esDescendente));
                break;

            case "-i":
            case "--input":
                input = argumentos[++i];
                break;

            case "-o":
            case "--output":
                output = argumentos[++i];
                break;

            default:
                if (!actual.StartsWith("-"))
                {
                    if (input == null) input = actual;
                    else if (output == null) output = actual;
                }
                else
                {
                    throw new ArgumentException($"La opción '{actual}' no me suena de nada. Usá --help para ver qué puedo hacer.");
                }
                break;
        }
    }

    return new AppConfig(input, output, separador, sinEncabezado, reglas);

    // Una mini-función local para no ensuciar el switch
    void MostrarAyuda()
    {
        Console.WriteLine("Herramienta sortx");
        Console.WriteLine("Modo de uso: sortx [entrada] [salida] [opciones]");
        Console.WriteLine("\nOpciones:");
        Console.WriteLine("  -b, --by <campo:tipo:orden>");
        Console.WriteLine("  -d, --delimiter");
        Console.WriteLine("  -nh, --no-header");
        Console.WriteLine("  -h, --help");
    }
}

string ReadInput(AppConfig config)
{
    // Se pasó una ruta de archivo
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        if (!File.Exists(config.InputFile))
        {
            throw new FileNotFoundException($"No pude encontrar el archivo: {config.InputFile}");
        }
        return File.ReadAllText(config.InputFile);
    }
    
    // No se paso un archivo, pero si datos de consola
    if (Console.IsInputRedirected)
    {
        return Console.In.ReadToEnd();
    }
    
    // No paso nada para leer
    throw new InvalidOperationException("No se pasó ningún archivo ni datos para leer.");
}

(string[] Headers, List<Dictionary<string, string>> Rows) ParseDelimited(string texto, AppConfig config)
{
    // Separo el texto por cada salto de línea
    var lineas = texto.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    if (lineas.Length == 0) throw new Exception("El archivo está vacío.");

    string[] encabezados;
    int inicioDatos = 0;

    // ¿Tiene cabecera? Si no, invento nombres (0, 1, 2...)
    if (config.NoHeader) {
        var primera = lineas[0].Split(config.Delimiter);
        encabezados = primera.Select((_, i) => i.ToString()).ToArray();
        inicioDatos = 0;
    } else {
        encabezados = lineas[0].Split(config.Delimiter);
        inicioDatos = 1;
    }

    var listaFilas = new List<Dictionary<string, string>>();

    // Recorro cada línea y la convierto en un "Mapa" (Diccionario)
    for (int i = inicioDatos; i < lineas.Length; i++) {
        var celdas = lineas[i].Split(config.Delimiter);
        var fila = new Dictionary<string, string>();
        for (int j = 0; j < encabezados.Length; j++) {
            fila[encabezados[j]] = j < celdas.Length ? celdas[j] : "";
        }
        listaFilas.Add(fila);
    }
    return (encabezados, listaFilas);
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> filas, AppConfig config)
{
    if (config.SortFields.Count == 0) return filas;

    IOrderedEnumerable<Dictionary<string, string>> filasOrdenadas;

    var primera = config.SortFields[0];
    if (primera.Descending)
        filasOrdenadas = filas.OrderByDescending(f => GetValue(f, primera));
    else
        filasOrdenadas = filas.OrderBy(f => GetValue(f, primera));

    for (int i = 1; i < config.SortFields.Count; i++)
    {
        var regla = config.SortFields[i];
        if (regla.Descending)
            filasOrdenadas = filasOrdenadas.ThenByDescending(f => GetValue(f, regla));
        else
            filasOrdenadas = filasOrdenadas.ThenBy(f => GetValue(f, regla));
    }

    return filasOrdenadas.ToList();
}

//Función auxiliar para obtener un valor (Texto o Número)
object GetValue(Dictionary<string, string> fila, SortField regla)
{
    string valor = fila.ContainsKey(regla.Name) ? fila[regla.Name] : "";

    if (regla.Numeric && double.TryParse(valor, out double num))
        return num;

    return valor;
}

void WriteOutput(string[] encabezados, List<Dictionary<string, string>> filas, AppConfig config)
{
    var lineasSalida = new List<string>();

    //Agregamos encabezado
    if (!config.NoHeader)
    {
        lineasSalida.Add(string.Join(config.Delimiter, encabezados));
    }

    //Convertimos cada fila ordenada de nuevo a texto
    foreach (var fila in filas)
    {
        var valoresFila = new List<string>();
        foreach (var col in encabezados)
        {
            valoresFila.Add(fila.ContainsKey(col) ? fila[col] : "");
        }
        lineasSalida.Add(string.Join(config.Delimiter, valoresFila));
    }

    string resultadoFinal = string.Join(Environment.NewLine, lineasSalida);

    if (!string.IsNullOrEmpty(config.OutputFile))
    {
        File.WriteAllText(config.OutputFile, resultadoFinal);
        Console.WriteLine($"\n Los Datos estan guardados en: {config.OutputFile}");
    }
    else
    {
        Console.WriteLine("\nRESULTADO ORDENADO");
        Console.WriteLine(resultadoFinal);
    }
}

//Modelo de Datos
record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(string? InputFile, string? OutputFile, string Delimiter, bool NoHeader, List<SortField> SortFields);