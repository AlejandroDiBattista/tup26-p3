
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

Console.WriteLine($"sortx {string.Join(" ", args)}");


try
{
    var config = ParsearArgumentos(args);

    if (config.ShowHelp)
    {
        Console.WriteLine("""
Uso:
  sortx [input [output]] [opciones]

Opciones:
  -b, --by campo[:tipo[:orden]]   Campo de ordenamiento
  -i, --input archivo             Archivo de entrada
  -o, --output archivo            Archivo de salida
  -d, --delimiter delimitador     Delimitador (default: ,)
  -nh, --no-header                Sin encabezado
  -ay, --ayuda                      Mostrar ayuda

Ejemplos:
  sortx empleados.csv -b apellido
  sortx empleados.csv -b salario:num:desc
  sortx datos.tsv -d "\\t" -nh -b 1:num:asc
""");
        return;
    }


    var textoEntrada = LeerEntrada(config);

    var (filas, encabezados) = ParsearDelimitado(textoEntrada, config);

    var filasOrdenadas = OrdenarFilas(filas, encabezados, config);

    var textoSalida = Serializar(filasOrdenadas, encabezados, config);

    EscribirSalida(textoSalida, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error: " + ex.Message);
    Environment.Exit(1);
}

AppConfig ParsearArgumentos(string[] args)
{
    string? archivoEntrada = null;
    string? archivoSalida = null;
    string delimitador = ",";
    bool sinEncabezado = false;
    bool mostrarAyuda = false;
    var camposOrden = new List<SortField>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        if (arg == "-i" || arg == "--input")
        {
            archivoEntrada = args[++i];
        }
        else if (arg == "-o" || arg == "--output")
        {
            archivoSalida = args[++i];
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            var valor = args[++i];
            delimitador = valor == "\\t" ? "\t" : valor;
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            sinEncabezado = true;
        }
        else if (arg == "-ay" || arg == "--ayuda")
        {
            mostrarAyuda = true;
        }
        else if (arg == "-b" || arg == "--by")
        {
            var especificacion = args[++i];
            camposOrden.Add(ParsearCampoOrden(especificacion));
        }
        else
        {
            if (archivoEntrada == null)
                archivoEntrada = arg;
            else if (archivoSalida == null)
                archivoSalida = arg;
        }
    }

    return new AppConfig(
        archivoEntrada,
        archivoSalida,
        delimitador,
        sinEncabezado,
        mostrarAyuda,
        camposOrden
    );
}


SortField ParsearCampoOrden(string especificacion)
{
    var partes = especificacion.Split(':');

    string nombre = partes[0];

    bool esNumerico = partes.Length > 1 && partes[1] == "num";
    bool descendente = partes.Length > 2 && partes[2] == "desc";

    return new SortField(nombre, esNumerico, descendente);
}


string LeerEntrada(AppConfig config)
{
    if (config.InputFile != null)
    {
        return File.ReadAllText(config.InputFile);
    }
    else
    {
        return Console.In.ReadToEnd();
    }
}

(List<string[]> filas, string[]? encabezados) ParsearDelimitado(string texto, AppConfig config)
{
    var lineas = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    string[]? encabezados = null;
    int inicioDatos = 0;

    if (!config.NoHeader)
    {
        encabezados = lineas[0].Trim().Split(config.Delimiter);
        inicioDatos = 1;
    }

    var filas = new List<string[]>();

    for (int i = inicioDatos; i < lineas.Length; i++)
    {
        var columnas = lineas[i].Trim().Split(config.Delimiter);
        filas.Add(columnas);
    }

    return (filas, encabezados);
}

List<string[]> OrdenarFilas(List<string[]> filas, string[]? encabezados, AppConfig config)
{
    if (config.SortFields.Count == 0)
        return filas;

    filas.Sort((a, b) =>
    {
        foreach (var campo in config.SortFields)
        {
            int indice = ObtenerIndiceCampo(campo.Name, encabezados);

            string valorA = a[indice];
            string valorB = b[indice];

            int comparacion;

            if (campo.Numeric)
            {
                double numA = double.Parse(valorA);
                double numB = double.Parse(valorB);
                comparacion = numA.CompareTo(numB);
            }
            else
            {
                comparacion = string.Compare(valorA, valorB, StringComparison.OrdinalIgnoreCase);
            }

            if (comparacion != 0)
            {
                return campo.Descending ? -comparacion : comparacion;
            }
        }

        return 0;
    });

    return filas;
}

int ObtenerIndiceCampo(string nombreCampo, string[]? encabezados)
{
    if (encabezados != null)
    {
        for (int i = 0; i < encabezados.Length; i++)
        {
            if (encabezados[i].Equals(nombreCampo, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new Exception($"Campo no encontrado: {nombreCampo}");
    }
    else
    {
        if (int.TryParse(nombreCampo, out int indice))
        {
            return indice;
        }

        throw new Exception($"Índice inválido: {nombreCampo}");
    }
}

string Serializar(List<string[]> filas, string[]? encabezados, AppConfig config)
{
    var lineas = new List<string>();

    if (!config.NoHeader && encabezados != null)
    {
        lineas.Add(string.Join(config.Delimiter, encabezados));
    }

    foreach (var fila in filas)
    {
        lineas.Add(string.Join(config.Delimiter, fila));
    }

    return string.Join('\n', lineas);
}

void EscribirSalida(string texto, AppConfig config)
{
    if (config.OutputFile != null)
    {
        File.WriteAllText(config.OutputFile, texto);
    }
    else
    {
        Console.WriteLine(texto);
    }
}



record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    bool ShowHelp,
    List<SortField> SortFields
);




