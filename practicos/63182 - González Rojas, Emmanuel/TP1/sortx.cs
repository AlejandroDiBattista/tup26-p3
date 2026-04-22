try
{
    var config = ParsearArgumentos(args);
    var texto = LeerEntrada(config);
    var datos = ParsearDelimitado(texto, config);
    var ordenado = OrdenarFilas(datos, config);
    var salida = Serializar(ordenado, config);
    EscribirSalida(salida, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

//Parseo de argumentos
AppConfig ParsearArgumentos(string[] args)
{
    string? archivoEntrada = null;
    string? archivoSalida = null;
    string delimitador = ",";
    bool sinEncabezado = false;

    var camposOrden = new List<SortField>();

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-i":
            case "--input":
                archivoEntrada = args[++i];
                break;

            case "-o":
            case "--output":
                archivoSalida = args[++i];
                break;

            case "-d":
            case "--delimiter":
                var d = args[++i];
                delimitador = d == "\\t" ? "\t" : d;
                break;

            case "-nh":
            case "--no-header":
                sinEncabezado = true;
                break;

            case "-b":
            case "--by":
                camposOrden.Add(ParsearCampoOrden(args[++i]));
                break;

            case "-h":
            case "--help":
                MostrarAyuda();
                Environment.Exit(0);
                break;

            default:
                if (archivoEntrada == null) archivoEntrada = args[i];
                else if (archivoSalida == null) archivoSalida = args[i];
                break;
        }
    }

    if (camposOrden.Count == 0)
        throw new Exception("Debe especificar un campo de ordenamiento");

    return new AppConfig(archivoEntrada, archivoSalida, delimitador, sinEncabezado, camposOrden);
}
//Parseo de campo de ordenamiento
SortField ParsearCampoOrden(string expresion)
{
    var partes = expresion.Split(':');

    string nombre = partes[0];
    bool esNumerico = partes.Length > 1 && partes[1] == "num";
    bool descendente = partes.Length > 2 && partes[2] == "desc";

    return new SortField(nombre, esNumerico, descendente);
}

//LECTURA de archivos de entrada
string LeerEntrada(AppConfig config)
{
    if (config.InputFile != null)
        return File.ReadAllText(config.InputFile);

    return Console.In.ReadToEnd();
}

//Parseo de datos sin ordenar
(List<Dictionary<string, string>> Filas, List<string> Encabezados) ParsearDelimitado(string texto, AppConfig config)
{
    var lineas = new List<string>();

    foreach (var l in texto.Split('\n'))
    {
        var limpia = l.Trim('\r');
        if (!string.IsNullOrWhiteSpace(limpia))
            lineas.Add(limpia);
    }

    if (lineas.Count == 0)
        throw new Exception("Archivo vacío");

    List<string> encabezados = new List<string>();

    if (!config.NoHeader)
    {
        var partes = Separar(lineas[0], config.Delimiter);
        foreach (var p in partes)
            encabezados.Add(p.Trim());

        lineas.RemoveAt(0);
    }
    else
    {
        var partes = Separar(lineas[0], config.Delimiter);
        for (int i = 0; i < partes.Length; i++)
            encabezados.Add(i.ToString());
    }

    var filas = new List<Dictionary<string, string>>();

    foreach (var linea in lineas)
    {
        var valores = Separar(linea, config.Delimiter);
        var fila = new Dictionary<string, string>();

        for (int i = 0; i < encabezados.Count; i++)
        {
            if (i < valores.Length)
                fila[encabezados[i]] = valores[i].Trim();
            else
                fila[encabezados[i]] = "";
        }

        filas.Add(fila);
    }

    return (filas, encabezados);
}


string[] Separar(string linea, string delimitador)
{
    return linea.Split(new string[] { delimitador }, StringSplitOptions.None);
}

//Ordenar multiples campos
(List<Dictionary<string, string>> Filas, List<string> Encabezados) OrdenarFilas(
    (List<Dictionary<string, string>> Filas, List<string> Encabezados) datos,
    AppConfig config)
{
    var filas = datos.Filas;

    foreach (var campo in config.SortFields)
    {
        if (!datos.Encabezados.Contains(campo.Name))
            throw new Exception($"El campo '{campo.Name}' no existe");
    }

    for (int i = 0; i < filas.Count - 1; i++)
    {
        for (int j = 0; j < filas.Count - i - 1; j++)
        {
            if (Comparar(filas[j], filas[j + 1], config.SortFields) > 0)
            {
                var temp = filas[j];
                filas[j] = filas[j + 1];
                filas[j + 1] = temp;
            }
        }
    }

    return datos;
}

int Comparar(Dictionary<string, string> a, Dictionary<string, string> b, List<SortField> campos)
{
    foreach (var campo in campos)
    {
        string valA = a[campo.Name];
        string valB = b[campo.Name];

        int resultado;

        if (campo.Numeric)
        {
            double numA = double.TryParse(valA, out var na) ? na : 0;
            double numB = double.TryParse(valB, out var nb) ? nb : 0;
            resultado = numA.CompareTo(numB);
        }
        else
        {
            resultado = string.Compare(valA, valB, StringComparison.Ordinal);
        }

        if (resultado != 0)
        {
            return campo.Descending ? -resultado : resultado;
        }
    }

    return 0;
}

//Serializacion de datos
string Serializar(
    (List<Dictionary<string, string>> Filas, List<string> Encabezados) datos,
    AppConfig config)
{
    var lineas = new List<string>();

    if (!config.NoHeader)
    {
        lineas.Add(string.Join(config.Delimiter, datos.Encabezados));
    }

    foreach (var fila in datos.Filas)
    {
        var valores = new List<string>();

        foreach (var h in datos.Encabezados)
        {
            valores.Add(fila[h]);
        }

        lineas.Add(string.Join(config.Delimiter, valores));
    }

    return string.Join(Environment.NewLine, lineas);
}

//salida de datos
void EscribirSalida(string texto, AppConfig config)
{
    if (config.OutputFile != null)
        File.WriteAllText(config.OutputFile, texto);
    else
        Console.WriteLine(texto);
}


void MostrarAyuda()
{
    Console.WriteLine(@"
Indicaciones para usar sortx:

sortx [archivoEntrada [archivoSalida]] -b campo[:tipo[:orden]]

Ejemplo:
sortx empleados.csv -b apellido
sortx empleados.csv -b edad:num:desc

Opciones:
  -b, --by            Campo de ordenamiento
  -i, --input         Archivo de entrada
  -o, --output        Archivo de salida
  -d, --delimiter     Delimitador (ej: , | \t)
  -nh, --no-header    Indica que no hay encabezado
  -h, --help          Mostrar ayuda
");
}

//MODELOS para configurar y ordenar

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
