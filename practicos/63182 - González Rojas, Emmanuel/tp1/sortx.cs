using System;
using System.IO;
using System.Collections.Generic;


try
{
    var config = ParseArgs(args);
    var texto = ReadInput(config);
    var filas = ParseDelimited(texto, config);
    var filasOrdenadas = SortRows(filas, config);
    var resultado = Serialize(filasOrdenadas, config);
    WriteOutput(resultado, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}



AppConfig ParseArgs(string[] args)
{
    string? entrada = null, salida = null, delimitador = ",";
    bool sinEncabezado = false;
    var listaCampos = new List<SortField>();
    var posicionales = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-i": case "--input": entrada = args[++i]; break;
            case "-o": case "--output": salida = args[++i]; break;
            case "-d": case "--delimiter": delimitador = args[++i].Replace("\\t", "\t"); break;
            case "-nh": case "--no-header": sinEncabezado = true; break;
            case "-b": case "--by":
                var partes = args[++i].Split(':');
                listaCampos.Add(new SortField(
                    partes[0], 
                    partes.Length > 1 && partes[1] == "num", 
                    partes.Length > 2 && partes[2] == "desc"
                ));
                break;
            case "-h": case "--help":
                MostrarAyuda();
                break;
            default:
                if (!args[i].StartsWith("-")) posicionales.Add(args[i]);
                break;
        }
    }

    if (posicionales.Count >= 1 && entrada == null) entrada = posicionales[0];
    if (posicionales.Count >= 2 && salida == null) salida = posicionales[1];

    return new AppConfig(entrada, salida, delimitador, sinEncabezado, listaCampos);
}

string ReadInput(AppConfig config) 
{
    if (config.InputFile != null) return File.ReadAllText(config.InputFile);
    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string texto, AppConfig config)
{
    var lineas = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    var filas = new List<Dictionary<string, string>>();
    var encabezados = new List<string>();
    var inicioDatos = 0;

    if (config.NoHeader)
    {
        var partes = lineas[0].Split(config.Delimiter);
        for (int i = 0; i < partes.Length; i++) encabezados.Add(i.ToString());
    }
    else
    {
        var partes = lineas[0].Split(config.Delimiter);
        foreach (var p in partes) encabezados.Add(p);
        inicioDatos = 1;
    }

    for (int i = inicioDatos; i < lineas.Length; i++)
    {
        var valores = lineas[i].Split(config.Delimiter);
        var dict = new Dictionary<string, string>();
        for (int j = 0; j < encabezados.Count; j++)
        {
            dict[encabezados[j]] = j < valores.Length ? valores[j] : "";
        }
        filas.Add(dict);
    }
    return filas;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> filas, AppConfig config)
{
    for (int i = 0; i < filas.Count - 1; i++)
    {
        for (int j = 0; j < filas.Count - i - 1; j++)
        {
            if (CompareRows(filas[j], filas[j + 1], config.SortFields) > 0)
            {
                var temp = filas[j];
                filas[j] = filas[j + 1];
                filas[j + 1] = temp;
            }
        }
    }
    return filas;
}

int CompareRows(Dictionary<string, string> a, Dictionary<string, string> b, List<SortField> criterios)
{
    foreach (var campo in criterios)
    {
        if (!a.ContainsKey(campo.Name)) throw new Exception($"El campo '{campo.Name}' no existe.");
        
        string valA = a[campo.Name];
        string valB = b[campo.Name];
        int resultado;

        if (campo.Numeric)
        {
            double nA = double.TryParse(valA, out double rA) ? rA : 0;
            double nB = double.TryParse(valB, out double rB) ? rB : 0;
            resultado = nA.CompareTo(nB);
        }
        else
        {
            resultado = string.Compare(valA, valB, StringComparison.Ordinal);
        }

        if (resultado != 0) return campo.Descending ? -resultado : resultado;
    }
    return 0;
}

string Serialize(List<Dictionary<string, string>> filas, AppConfig config)
{
    var sb = new System.Text.StringBuilder();
    if (!config.NoHeader && filas.Count > 0)
    {
        var llaves = new List<string>(filas[0].Keys);
        sb.AppendLine(string.Join(config.Delimiter, llaves));
    }
    foreach (var fila in filas)
    {
        var valores = new List<string>();
        foreach (var llave in fila.Keys) valores.Add(fila[llave]);
        sb.AppendLine(string.Join(config.Delimiter, valores));
    }
    return sb.ToString().TrimEnd();
}

void WriteOutput(string texto, AppConfig config)
{
    if (config.OutputFile != null) File.WriteAllText(config.OutputFile, texto);
    else Console.Write(texto);
}

void MostrarAyuda()
{
    Console.WriteLine("Uso: sortx [input [output]] [-b campo[:tipo[:orden]]]... [-i input] [-o output] [-d delimitador] [-nh] [-h]");
    Console.WriteLine("\nOpciones:");
    Console.WriteLine("  -i, --input <archivo>        Archivo de entrada (default: stdin).");
    Console.WriteLine("  -o, --output <archivo>       Archivo de salida (default: stdout).");
    Console.WriteLine("  -d, --delimiter <delimitador> Delimitador (default: ,).");
    Console.WriteLine("  -nh, --no-header             Indica que el archivo no tiene encabezado.");
    Console.WriteLine("  -b, --by campo[:tipo[:orden]] Criterio de ordenamiento (repetible).");
    Console.WriteLine("  -h, --help                   Muestra esta ayuda.");
    Environment.Exit(0);
}

record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(
    string? InputFile, 
    string? OutputFile, 
    string Delimiter, 
    bool NoHeader, 
    List<SortField> SortFields);
