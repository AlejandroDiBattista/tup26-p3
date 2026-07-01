
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]
using System;
using System.Collections.Generic;
using System.IO;

//  obligatorio 
Console.WriteLine($"sortx {string.Join(" ", args)}");

try
{
    // Paso1: Leer la configuración
    AppConfig ParseArgs(string[] argumentos)
{
    
    string? archivoEntrada = null;
    string? archivoSalida = null;
    string delimitador = ",";
    bool sinCabecera = false;
    List<SortField> camposParaOrdenar = new List<SortField>();

    for (int i = 0; i < argumentos.Length; i++)
    {
        string actual = argumentos[i];

        if (actual == "-d" || actual == "--delimiter")
        {
            delimitador = argumentos[++i];
        }
        else if (actual == "-nh" || actual == "--no-header")
        {
            sinCabecera = true;
        }
        else if (actual == "-b" || actual == "--by")
        {
            string[] partes = argumentos[++i].Split(':');
            string nombreColumna = partes[0];
            
            // Verificamos si pidió que sea numérico (int) o descendente (desc)
            bool esNumerico = partes.Length > 1 && partes[1] == "int";
            bool esDescendente = partes.Length > 2 && partes[2] == "desc";

            camposParaOrdenar.Add(new SortField(nombreColumna, esNumerico, esDescendente));
        }
        else if (!actual.StartsWith("-"))
        {
            // Si no empieza con "-", es un nombre de archivo
            if (archivoEntrada == null) archivoEntrada = actual;
            else if (archivoSalida == null) archivoSalida = actual;
        }
    }

    // c. Devolvemos la "ficha" AppConfig con todo lo que recolectamos
    return new AppConfig(archivoEntrada, archivoSalida, delimitador, sinCabecera, camposParaOrdenar);
}

    // Paso2: Leer el texto de entrada
    string ReadInput(AppConfig config)
{
    // Si el usuario nos pasó un nombre de archivo
    if (config.InputFile != null)
    {
        if (!File.Exists(config.InputFile))
            throw new Exception($"El archivo '{config.InputFile}' no existe.");
            
        return File.ReadAllText(config.InputFile);
    }

    // Si no hay archivo, leemos de la consola hasta que termine
    return Console.In.ReadToEnd();
}

    // Paso3: Convertir texto a lista de filas 
    List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config)
{
    var listaDeFilas = new List<Dictionary<string, string>>();
    
    // 1. Dividimos el texto por líneas 
    string[] lineas = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    if (lineas.Length == 0) return listaDeFilas;

    // 2. Identificamos la cabecera 
    string[] cabecera = lineas[0].Split(config.Delimiter);

    // 3. Empezamos a procesar desde la segunda línea 
    for (int i = 1; i < lineas.Length; i++)
    {
        string[] valores = lineas[i].Split(config.Delimiter);
        var fila = new Dictionary<string, string>();

        // Emparejamos cada nombre de columna con su valor en esta fila
        for (int j = 0; j < cabecera.Length; j++)
        {
            // Verificamos que no nos pasemos del número de valores en la línea
            string nombreColumna = cabecera[j];
            string valorCelda = j < valores.Length ? valores[j] : "";
            
            fila.Add(nombreColumna, valorCelda);
        }

        listaDeFilas.Add(fila);
    }

    return listaDeFilas;
}

    // Paso4: Ordenar las filas
    void SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    // Si el usuario no especificó campos para ordenar, no hacemos nada
    if (config.SortFields.Count == 0) return;

    // .Sort() compara las filas de dos en dos (fila A y fila B)
    rows.Sort((a, b) =>
    {
        int resultado = 0;

        // Revisamos cada criterio de ordenamiento que pidió el usuario
        foreach (var campo in config.SortFields)
        {
            string valorA = a.ContainsKey(campo.Name) ? a[campo.Name] : "";
            string valorB = b.ContainsKey(campo.Name) ? b[campo.Name] : "";

            if (campo.Numeric)
            {
                // Si es numérico, convertimos el texto a decimal para comparar bien
                decimal.TryParse(valorA, out decimal numA);
                decimal.TryParse(valorB, out decimal numB);
                resultado = numA.CompareTo(numB);
            }
            else
            {
                // Si es texto, comparación alfabética normal
                resultado = string.Compare(valorA, valorB, StringComparison.OrdinalIgnoreCase);
            }

            // Si es descendente, invertimos el resultado
            if (campo.Descending) resultado *= -1;

            // Si las celdas son diferentes, ya sabemos cuál va primero.
            // Si son iguales (resultado == 0), pasamos al siguiente criterio del -b.
            if (resultado != 0) break;
        }

        return resultado;
    });
}

    // Paso5: Convertir filas a texto otra vez
    string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0) return "";

    // 1. Reconstruir la cabecera a partir de las llaves del primer diccionario
    var columnas = new List<string>(rows[0].Keys);
    string cabecera = string.Join(config.Delimiter, columnas);
    
    // 2. Usar un StringWriter para ir pegando las filas de forma eficiente
    var writer = new StringWriter();
    
    // Si el usuario NO pidió ocultar la cabecera, la escribimos
    if (!config.NoHeader) writer.WriteLine(cabecera);

    // 3. Escribir cada fila
    foreach (var fila in rows)
    {
        var valores = new List<string>();
        foreach (var col in columnas)
        {
            valores.Add(fila[col]);
        }
        writer.WriteLine(string.Join(config.Delimiter, valores));
    }

    return writer.ToString();
}

    // Paso6: Escribir el resultado
    void WriteOutput(string text, AppConfig config)
{
    if (config.OutputFile != null)
    {
        File.WriteAllText(config.OutputFile, text);
        Console.WriteLine($"Resultado guardado en: {config.OutputFile}");
    }
    else
    {
        // Si no hay archivo de salida, lo tiramos a la consola (stdout)
        Console.Write(text);
    }
}
    
    Console.WriteLine("Proceso finalizado con éxito.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}

// FUNCIONES LOCALES 

AppConfig ParseArgs(string[] argumentos)
{    
    return new AppConfig(null, null, ",", false, new List<SortField>());
}

string ReadInput(AppConfig config)
{       return "";
}

List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config)
{    
    return new List<Dictionary<string, string>>();
}

void SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{  
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    return "";
}

void WriteOutput(string text, AppConfig config)
{
}

// records obligatorios

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields);