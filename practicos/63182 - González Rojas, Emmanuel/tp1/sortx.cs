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


record CampoOrden(string Nombre, bool EsNumerico, bool EsDescendente);
record ConfiguracionApp(
    string? ArchivoEntrada, 
    string? ArchivoSalida, 
    string Delimitador, 
    bool SinEncabezado, 
    List<CampoOrden> CamposParaOrdenar);