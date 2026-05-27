using System;
using System.IO;
using System.Collections.Generic;

ConfiguracionApp ParsearArgumentos(string[] args)
{
    string? entrada = null, salida = null, delimitador = ",";
    bool sinEncabezado = false;
    var listaCampos = new List<CampoOrden>();

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
                listaCampos.Add(new CampoOrden(
                    partes[0], 
                    partes.Length > 1 && partes[1] == "num", 
                    partes.Length > 2 && partes[2] == "desc"
                ));
                break;
            case "-h": case "--help":
                Console.WriteLine("Uso: sortx [entrada] [salida] [-b campo:tipo:orden]...");
                Environment.Exit(0);
                break;
        }
    }
    return new ConfiguracionApp(entrada, salida, delimitador, sinEncabezado, listaCampos);
}

string LeerEntrada(ConfiguracionApp config) 
{
    if (config.ArchivoEntrada != null) return File.ReadAllText(config.ArchivoEntrada);
    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParsearDelimitado(string texto, ConfiguracionApp config)
{
    var lineas = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    var filas = new List<Dictionary<string, string>>();
    
    var encabezados = new List<string>();
    var inicioDatos = 0;

    if (config.SinEncabezado)
    {
        var partes = lineas[0].Split(config.Delimitador);
        for (int i = 0; i < partes.Length; i++) encabezados.Add(i.ToString());
    }
    else
    {
        var partes = lineas[0].Split(config.Delimitador);
        foreach (var p in partes) encabezados.Add(p);
        inicioDatos = 1;
    }

    for (int i = inicioDatos; i < lineas.Length; i++)
    {
        var valores = lineas[i].Split(config.Delimitador);
        var dict = new Dictionary<string, string>();
        for (int j = 0; j < encabezados.Count; j++)
        {
            dict[encabezados[j]] = j < valores.Length ? valores[j] : "";
        }
        filas.Add(dict);
    }
    return filas;
}

List<Dictionary<string, string>> OrdenarFilas(List<Dictionary<string, string>> filas, ConfiguracionApp config)
{
    for (int i = 0; i < filas.Count - 1; i++)
    {
        for (int j = 0; j < filas.Count - i - 1; j++)
        {
            if (CompararFilas(filas[j], filas[j + 1], config.CamposParaOrdenar) > 0)
            {
                var temp = filas[j];
                filas[j] = filas[j + 1];
                filas[j + 1] = temp;
            }
        }
    }
    return filas;
}

int CompararFilas(Dictionary<string, string> a, Dictionary<string, string> b, List<CampoOrden> criterios)
{
    foreach (var campo in criterios)
    {
        string valA = a[campo.Nombre];
        string valB = b[campo.Nombre];
        int resultado;

        if (campo.EsNumerico)
        {
            double nA = double.TryParse(valA, out double rA) ? rA : 0;
            double nB = double.TryParse(valB, out double rB) ? rB : 0;
            resultado = nA.CompareTo(nB);
        }
        else
        {
            resultado = string.Compare(valA, valB, StringComparison.Ordinal);
        }

        if (resultado != 0) return campo.EsDescendente ? -resultado : resultado;
    }
    return 0;
}

string Serializar(List<Dictionary<string, string>> filas, ConfiguracionApp config)
    {
        var sb = new System.Text.StringBuilder();
        if (!config.SinEncabezado && filas.Count > 0)
        {
            var llaves = new List<string>(filas[0].Keys);
            sb.AppendLine(string.Join(config.Delimitador, llaves));
        }
        foreach (var fila in filas)
        {
            var valores = new List<string>();
            foreach (var llave in fila.Keys) valores.Add(fila[llave]);
            sb.AppendLine(string.Join(config.Delimitador, valores));
        }
        return sb.ToString().TrimEnd();
    }

record CampoOrden(string Nombre, bool EsNumerico, bool EsDescendente);
record ConfiguracionApp(
    string? ArchivoEntrada, 
    string? ArchivoSalida, 
    string Delimitador, 
    bool SinEncabezado, 
    List<CampoOrden> CamposParaOrdenar);