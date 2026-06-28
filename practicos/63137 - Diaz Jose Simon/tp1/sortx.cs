
try
{
    ConfiguracionApp configuracion = ProcesarArgumentos(args);

    string textoEntrada = LeerEntrada(configuracion.ArchivoEntrada);

    var datosParseados = ParsearDelimitado(textoEntrada, configuracion.Delimitador, configuracion.SinEncabezado);

    var filasOrdenadas = OrdenarFilas(datosParseados, configuracion.CamposOrden, configuracion.SinEncabezado);
    string salidaSerializada = Serializar((filasOrdenadas, datosParseados.Encabezados), configuracion.Delimitador, configuracion.SinEncabezado);

    EscribirSalida(salidaSerializada, configuracion.ArchivoSalida);
}
catch (Exception excepcion)
{
    Console.Error.WriteLine($"Error: {excepcion.Message}");
    Environment.Exit(1);
}

ConfiguracionApp ProcesarArgumentos(string[] argumentos)
{
    if (argumentos.Any(a => a is "-h" or "--help"))
    {
        MostrarAyuda();
        Environment.Exit(0);
    }

    string? rutaArchivoEntrada = null;
    string? rutaArchivoSalida = null;
    string delimitador = ",";
    bool sinEncabezado = false;
    List<CampoOrden> camposOrden = new();

    List<string> argumentosPosicionales = argumentos
        .TakeWhile(argumento => !argumento.StartsWith("-"))
        .ToList();

    if (argumentosPosicionales.Count >= 1) rutaArchivoEntrada = argumentosPosicionales[0];
    if (argumentosPosicionales.Count >= 2) rutaArchivoSalida = argumentosPosicionales[1];

    for (int indiceActual = argumentosPosicionales.Count; indiceActual < argumentos.Length; indiceActual++)
    {
        string argumentoActual = argumentos[indiceActual];

        switch (argumentoActual)
        {
            case "-b" or "--by":
                camposOrden.Add(ParsearCampoOrden(ObtenerSiguienteArgumento(argumentos, indiceActual, argumentoActual)));
                indiceActual++;
                break;

            case "-i" or "--input":
                rutaArchivoEntrada = ObtenerSiguienteArgumento(argumentos, indiceActual, argumentoActual);
                indiceActual++;
                break;

            case "-o" or "--output":
                rutaArchivoSalida = ObtenerSiguienteArgumento(argumentos, indiceActual, argumentoActual);
                indiceActual++;
                break;

            case "-d" or "--delimiter":
                string delimitadorCrudo = ObtenerSiguienteArgumento(argumentos, indiceActual, argumentoActual);
                delimitador = delimitadorCrudo == @"\t" ? "\t" : delimitadorCrudo;
                indiceActual++;
                break;

            case "-nh" or "--no-header":
                sinEncabezado = true;
                break;

            default:
                throw new ArgumentException($"Opción desconocida: '{argumentoActual}'");
        }
    }

    return new ConfiguracionApp(rutaArchivoEntrada, rutaArchivoSalida, delimitador, sinEncabezado, camposOrden);
}

string LeerEntrada(string? rutaArchivoEntrada)
{
    if (rutaArchivoEntrada is null)
        return Console.In.ReadToEnd();

    if (!File.Exists(rutaArchivoEntrada))
        throw new FileNotFoundException($"El archivo de entrada no existe: '{rutaArchivoEntrada}'");

    return File.ReadAllText(rutaArchivoEntrada);
}

(List<Dictionary<string, string>> Filas, List<string> Encabezados) ParsearDelimitado(
    string textoCompleto,
    string delimitador,
    bool sinEncabezado)
{
    List<string[]> todasLasLineas = textoCompleto
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(linea => linea.TrimEnd('\r').Split(delimitador))
        .ToList();

    if (todasLasLineas.Count == 0)
        throw new InvalidDataException("El archivo de entrada está vacío.");

    List<string> encabezados = sinEncabezado
        ? Enumerable.Range(0, todasLasLineas[0].Length).Select(indice => indice.ToString()).ToList()
        : todasLasLineas[0].ToList();

    IEnumerable<string[]> lineasDatos = sinEncabezado ? todasLasLineas : todasLasLineas.Skip(1);

    List<Dictionary<string, string>> filas = lineasDatos
        .Select(campos => MapearCamposAEncabezados(campos, encabezados))
        .ToList();

    return (filas, encabezados);
}

List<Dictionary<string, string>> OrdenarFilas(
    (List<Dictionary<string, string>> Filas, List<string> Encabezados) datosParseados,
    List<CampoOrden> camposOrden,
    bool sinEncabezado)
{
    if (camposOrden.Count == 0)
        return datosParseados.Filas;

    ValidarCamposOrden(camposOrden, datosParseados.Encabezados, sinEncabezado);

    IOrderedEnumerable<Dictionary<string, string>> filasOrdenadas =
        AplicarOrdenPrimario(datosParseados.Filas, camposOrden[0]);

    foreach (CampoOrden campoOrden in camposOrden.Skip(1))
        filasOrdenadas = AplicarOrdenSecundario(filasOrdenadas, campoOrden);

    return filasOrdenadas.ToList();
}

string Serializar(
    (List<Dictionary<string, string>> Filas, List<string> Encabezados) datosOrdenados,
    string delimitador,
    bool sinEncabezado)
{
    List<string> lineasSalida = new();

    if (!sinEncabezado)
        lineasSalida.Add(string.Join(delimitador, datosOrdenados.Encabezados));

    foreach (var fila in datosOrdenados.Filas)
    {
        var valoresOrdenados = datosOrdenados.Encabezados.Select(encabezado =>
            fila.TryGetValue(encabezado, out string? valor) ? valor : string.Empty);
        lineasSalida.Add(string.Join(delimitador, valoresOrdenados));
    }

    return string.Join(Environment.NewLine, lineasSalida);
}

void EscribirSalida(string contenido, string? rutaArchivoSalida)
{
    if (rutaArchivoSalida is null)
        Console.Write(contenido);
    else
        File.WriteAllText(rutaArchivoSalida, contenido);
}

string ObtenerSiguienteArgumento(string[] argumentos, int indiceActual, string bandera)
{
    int indiceSiguiente = indiceActual + 1;

    if (indiceSiguiente >= argumentos.Length)
        throw new ArgumentException($"La opción '{bandera}' requiere un valor.");

    return argumentos[indiceSiguiente];
}

CampoOrden ParsearCampoOrden(string especificacionCampo)
{
    string[] partes = especificacionCampo.Split(':');

    string nombreCampo = partes[0];

    string tipoComparacion = partes.Length >= 2
        ? partes[1].ToLower()
        : "alpha";

    string direccionOrden = partes.Length >= 3
        ? partes[2].ToLower()
        : "asc";

    bool esNumerico = tipoComparacion == "num";
    bool esDescendente = direccionOrden == "desc";

    return new CampoOrden(nombreCampo, esNumerico, esDescendente);
}

Dictionary<string, string> MapearCamposAEncabezados(string[] campos, List<string> encabezados)
{
    var fila = new Dictionary<string, string>();

    for (int indiceColumna = 0; indiceColumna < encabezados.Count; indiceColumna++)
    {
        string valor = indiceColumna < campos.Length ? campos[indiceColumna].Trim() : string.Empty;
        fila[encabezados[indiceColumna]] = valor;
    }

    return fila;
}

void ValidarCamposOrden(List<CampoOrden> camposOrden, List<string> encabezados, bool sinEncabezado)
{
    foreach (CampoOrden campoOrden in camposOrden)
    {
        if (!encabezados.Contains(campoOrden.Nombre))
        {
            string contexto = sinEncabezado ? "índice de columna" : "nombre de columna";
            throw new ArgumentException($"El campo '{campoOrden.Nombre}' no existe como {contexto}.");
        }
    }
}

IOrderedEnumerable<Dictionary<string, string>> AplicarOrdenPrimario(
    List<Dictionary<string, string>> filas,
    CampoOrden campoOrden)
{
    return campoOrden.Descendente
        ? filas.OrderByDescending(fila => ExtraerClaveOrden(fila, campoOrden))
        : filas.OrderBy(fila => ExtraerClaveOrden(fila, campoOrden));
}

IOrderedEnumerable<Dictionary<string, string>> AplicarOrdenSecundario(
    IOrderedEnumerable<Dictionary<string, string>> filasOrdenadas,
    CampoOrden campoOrden)
{
    return campoOrden.Descendente
        ? filasOrdenadas.ThenByDescending(fila => ExtraerClaveOrden(fila, campoOrden))
        : filasOrdenadas.ThenBy(fila => ExtraerClaveOrden(fila, campoOrden));
}

IComparable ExtraerClaveOrden(Dictionary<string, string> fila, CampoOrden campoOrden)
{
    string valorCrudo = fila.TryGetValue(campoOrden.Nombre, out string? valor)
        ? valor ?? string.Empty
        : string.Empty;

    if (!campoOrden.Numerico)
        return valorCrudo;

    return double.TryParse(valorCrudo, out double numeroParseado)
        ? numeroParseado
        : double.MinValue;
}

void MostrarAyuda()
{
    Console.WriteLine("""
        sortx — Ordenador de archivos de texto delimitados

        USO:
          sortx [entrada [salida]] [-b campo[:tipo[:orden]]]... [opciones]

        OPCIONES:
          -b, --by campo[:tipo[:orden]]   Campo por el que ordenar. Se puede repetir.
                                          tipo:  alpha (default) | num
                                          orden: asc (default)   | desc
          -i, --input archivo             Archivo de entrada (o stdin si se omite).
          -o, --output archivo            Archivo de salida (o stdout si se omite).
          -d, --delimiter delimitador     Carácter delimitador. Default: ','
                                          Usar \t para tabulación.
          -nh, --no-header                El archivo no tiene fila de encabezado.
                                          Los campos se identifican por índice (0, 1, 2...).
          -h, --help                      Muestra esta ayuda y termina.

        EJEMPLOS:
          sortx empleados.csv -b apellido
          sortx empleados.csv -b salario:num:desc
          sortx empleados.csv -b departamento -b salario:num:desc
          sortx empleados.csv resultado.csv -b apellido
          sortx datos.tsv -d "\t" -nh -b 1:alpha:asc
          cat empleados.csv | sortx -b apellido > ordenado.csv
        """);
}

record CampoOrden(string Nombre, bool Numerico, bool Descendente);

record ConfiguracionApp(
    string? ArchivoEntrada,
    string? ArchivoSalida,
    string Delimitador,
    bool SinEncabezado,
    List<CampoOrden> CamposOrden
);
