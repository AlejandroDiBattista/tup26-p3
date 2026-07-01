using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

try
{
    AjustesSistema ajustes = InterpretarParametros(args);
    List<string> textoBruto = CargarContenido(ajustes.RutaOrigen);

    if (textoBruto.Count == 0)
    {
        return 0;
    }

    var (coleccionDatos, lineaTitulos) = SegmentarLineas(textoBruto, ajustes);
    List<Dictionary<string, string>> tablaOrganizada = EjecutarOrdenamiento(coleccionDatos, ajustes.ReglasAsignadas);
    List<string> bloqueFinal = EmpaquetarTexto(tablaOrganizada, lineaTitulos, ajustes.Separador);
    DespacharResultado(bloqueFinal, ajustes.RutaDestino);

    return 0;
}
catch (Exception excepcion)
{
    Console.Error.WriteLine($"Error: {excepcion.Message}");
    return 1;
}

AjustesSistema InterpretarParametros(string[] opcionesTerminal)
{
    string? rutaOrigen = null;
    string? rutaDestino = null;
    string separador = ",";
    bool ignorarCabecera = false;
    List<ReglaClasificacion> reglasAsignadas = new List<ReglaClasificacion>();
    List<string> valoresSueltos = new List<string>();

    for (int i = 0; i < opcionesTerminal.Length; i++)
    {
        string itemActual = opcionesTerminal[i];

        if (itemActual == "-h" || itemActual == "--help")
        {
            MostrarAyuda();
            Environment.Exit(0);
        }
        else if (itemActual == "-nh" || itemActual == "--no-header")
        {
            ignorarCabecera = true;
        }
        else if (itemActual == "-d" || itemActual == "--delimiter")
        {
            if (i + 1 >= opcionesTerminal.Length) throw new ArgumentException("Falta el valor para el delimitador.");
            string textoExtraido = opcionesTerminal[++i];
            separador = textoExtraido == "\\t" ? "\t" : textoExtraido;
        }
        else if (itemActual == "-i" || itemActual == "--input")
        {
            if (i + 1 >= opcionesTerminal.Length) throw new ArgumentException("Falta el archivo de entrada.");
            rutaOrigen = opcionesTerminal[++i];
        }
        else if (itemActual == "-o" || itemActual == "--output")
        {
            if (i + 1 >= opcionesTerminal.Length) throw new ArgumentException("Falta el archivo de salida.");
            rutaDestino = opcionesTerminal[++i];
        }
        else if (itemActual == "-b" || itemActual == "--by")
        {
            if (i + 1 >= opcionesTerminal.Length) throw new ArgumentException("Falta la especificación del campo para ordenar.");
            string patronFormato = opcionesTerminal[++i];
            reglasAsignadas.Add(InterpretarRegla(patronFormato));
        }
        else if (itemActual.StartsWith("-"))
        {
            throw new ArgumentException($"Opción desconocida: {itemActual}");
        }
        else
        {
            valoresSueltos.Add(itemActual);
        }
    }

    if (valoresSueltos.Count > 0 && rutaOrigen == null)
    {
        rutaOrigen = valoresSueltos[0];
    }
    if (valoresSueltos.Count > 1 && rutaDestino == null)
    {
        rutaDestino = valoresSueltos[1];
    }
    if (valoresSueltos.Count > 2)
    {
        throw new ArgumentException("Demasiados argumentos posicionales.");
    }

    if (reglasAsignadas.Count == 0)
    {
        throw new ArgumentException("Debe especificar al menos un criterio de ordenamiento con -b o --by.");
    }

    return new AjustesSistema(rutaOrigen, rutaDestino, separador, ignorarCabecera, reglasAsignadas);

    ReglaClasificacion InterpretarRegla(string patronFormato)
    {
        string[] fragmentos = patronFormato.Split(':');
        string tagCampo = fragmentos[0];
        bool esNumerico = false;
        bool esInverso = false;

        if (fragmentos.Length > 1)
        {
            if (fragmentos[1] == "num") esNumerico = true;
            else if (fragmentos[1] != "alpha") throw new ArgumentException($"Tipo de ordenamiento inválido: {fragmentos[1]}");
        }
        if (fragmentos.Length > 2)
        {
            if (fragmentos[2] == "desc") esInverso = true;
            else if (fragmentos[2] != "asc") throw new ArgumentException($"Dirección de ordenamiento inválida: {fragmentos[2]}");
        }

        return new ReglaClasificacion(tagCampo, esNumerico, esInverso);
    }

    void MostrarAyuda()
    {
        Console.WriteLine("sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
        Console.WriteLine("      [-i|--input input] [-o|--output output]");
        Console.WriteLine("      [-d|--delimiter delimitador] [-nh|--no-header] [-h|--help]");
    }
}

List<string> CargarContenido(string? rutaOrigen)
{
    List<string> listaRenglones = new List<string>();
    
    if (rutaOrigen != null)
    {
        if (!File.Exists(rutaOrigen))
        {
            throw new FileNotFoundException($"El archivo de entrada no existe: {rutaOrigen}");
        }
        listaRenglones.AddRange(File.ReadAllLines(rutaOrigen));
    }
    else
    {
        string? renglonActual;
        while ((renglonActual = Console.ReadLine()) != null)
        {
            listaRenglones.Add(renglonActual);
        }
    }
    
    return listaRenglones;
}

(List<Dictionary<string, string>> ColeccionDatos, string? LineaTitulos) SegmentarLineas(List<string> textoBruto, AjustesSistema ajustes)
{
    List<Dictionary<string, string>> coleccionDatos = new List<Dictionary<string, string>>();
    string? lineaTitulos = null;
    List<string> listaColumnas = new List<string>();
    int puntoArranque = 0;

    if (!ajustes.IgnorarCabecera && textoBruto.Count > 0)
    {
        lineaTitulos = textoBruto[0];
        listaColumnas = lineaTitulos.Split(new[] { ajustes.Separador }, StringSplitOptions.None).Select(h => h.Trim()).ToList();
        puntoArranque = 1;
    }

    for (int i = puntoArranque; i < textoBruto.Count; i++)
    {
        if (string.IsNullOrWhiteSpace(textoBruto[i])) continue;

        string[] celdas = textoBruto[i].Split(new[] { ajustes.Separador }, StringSplitOptions.None);
        var mapaRegistro = new Dictionary<string, string>();

        for (int j = 0; j < celdas.Length; j++)
        {
            string textoCelda = celdas[j].Trim();
            if (ajustes.IgnorarCabecera)
            {
                mapaRegistro[j.ToString()] = textoCelda;
            }
            else
            {
                if (j < listaColumnas.Count)
                {
                    mapaRegistro[listaColumnas[j]] = textoCelda;
                }
            }
        }
        
        if (!ajustes.IgnorarCabecera)
        {
            foreach (var col in listaColumnas)
            {
                if (!mapaRegistro.ContainsKey(col)) mapaRegistro[col] = "";
            }
        }

        coleccionDatos.Add(mapaRegistro);
    }

    return (coleccionDatos, lineaTitulos);
}

List<Dictionary<string, string>> EjecutarOrdenamiento(List<Dictionary<string, string>> coleccionDatos, List<ReglaClasificacion> reglasAsignadas)
{
    if (coleccionDatos.Count == 0) return coleccionDatos;

    foreach (var regla in reglasAsignadas)
    {
        if (!coleccionDatos[0].ContainsKey(regla.TagCampo))
        {
            throw new ArgumentException($"El campo de ordenamiento '{regla.TagCampo}' no existe en los datos.");
        }
    }

    coleccionDatos.Sort((elementoX, elementoY) =>
    {
        foreach (var regla in reglasAsignadas)
        {
            string strX = elementoX[regla.TagCampo];
            string strY = elementoY[regla.TagCampo];
            int resultadoCmp = 0;

            if (regla.EsNumerico)
            {
                bool validoX = decimal.TryParse(strX, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decX);
                bool validoY = decimal.TryParse(strY, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decY);

                if (validoX && validoY)
                {
                    resultadoCmp = decX.CompareTo(decY);
                }
                else if (validoX) resultadoCmp = 1;
                else if (validoY) resultadoCmp = -1;
                else resultadoCmp = string.Compare(strX, strY, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                resultadoCmp = string.Compare(strX, strY, StringComparison.OrdinalIgnoreCase);
            }

            if (resultadoCmp != 0)
            {
                return regla.EsInverso ? -resultadoCmp : resultadoCmp;
            }
        }
        return 0;
    });

    return coleccionDatos;
}

List<string> EmpaquetarTexto(List<Dictionary<string, string>> tablaOrganizada, string? lineaTitulos, string separador)
{
    List<string> salidaFinal = new List<string>();

    if (lineaTitulos != null)
    {
        salidaFinal.Add(lineaTitulos);
    }

    foreach (var reg in tablaOrganizada)
    {
        var itemsProcesados = reg.Keys
            .Select(k => new { Clave = k, EsNum = int.TryParse(k, out int idx), Idx = idx })
            .OrderBy(k => k.EsNum ? k.Idx : 0)
            .Select(k => reg[k.Clave]);

        salidaFinal.Add(string.Join(separador, itemsProcesados));
    }

    return salidaFinal;
}

void DespacharResultado(List<string> bloqueFinal, string? rutaDestino)
{
    if (rutaDestino != null)
    {
        File.WriteAllLines(rutaDestino, bloqueFinal);
    }
    else
    {
        foreach (var itemTexto in bloqueFinal)
        {
            Console.WriteLine(itemTexto);
        }
    }
}

record ReglaClasificacion(string TagCampo, bool EsNumerico, bool EsInverso);

record AjustesSistema(
    string? RutaOrigen,
    string? RutaDestino,
    string Separador,
    bool IgnorarCabecera,
    List<ReglaClasificacion> ReglasAsignadas
);