using System; 
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

try
{
    var config = ParseArgs(args);
    var texto = ReadInput(config);
    var filas = ParseDelimited(texto, config);
    var ordenadas = SortRows(filas, config);
    var salida = Serialize(ordenadas, config);
    WriteOutput(salida, config);
}

catch (Exception ex)
{
    Console.Error.WriteLine("Error: " + ex.Message);
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var camposOrden = new List<SortField>();
    var posicionales = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        switch (arg)
        {
            case "-i":
            case "--input":
                input = args[++i];
                break;

            case "-o":
            case "--output":
                output = args[++i];
                break;

            case "-d":
            case "--delimiter":
                var d = args[++i];
                delimiter = d == "\\t" ? "\t" : d;
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-h":
            case "--help":
                ShowHelp();
                Environment.Exit(0);
                break;

            case "-b":
            case "--by":
                var spec = args[++i];
                camposOrden.Add(ParseSortField(spec));
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new Exception("Opción desconocida: " + arg);

                posicionales.Add(arg);
                break;
        }
    }

    if (posicionales.Count > 0 && input == null)
        input = posicionales[0];

    if (posicionales.Count > 1 && output == null)
        output = posicionales[1];

    if (camposOrden.Count == 0)
        throw new Exception("Debe especificar al menos un campo de ordenamiento con -b");

    return new AppConfig(input, output, delimiter, noHeader, camposOrden);
}

void ShowHelp()
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  sortx [input [output]] -b campo[:tipo[:orden]]...");
    Console.WriteLine();
    Console.WriteLine("Opciones:");
    Console.WriteLine("  -b, --by           Campo de ordenamiento");
    Console.WriteLine("  -i, --input        Archivo de entrada");
    Console.WriteLine("  -o, --output       Archivo de salida");
    Console.WriteLine("  -d, --delimiter    Delimitador (default ,)");
    Console.WriteLine("  -nh, --no-header   Sin encabezado");
    Console.WriteLine("  -h, --help         Mostrar ayuda");
}

string ReadInput(AppConfig config)
{
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        if (!File.Exists(config.InputFile))
            throw new Exception("Archivo no encontrado: " + config.InputFile);

        return File.ReadAllText(config.InputFile);
    }

    if (!Console.IsInputRedirected)
        throw new Exception("No se especificó archivo de entrada ni hay datos en stdin");

    using var reader = Console.In;
    return reader.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string texto, AppConfig config)
{
    var filas = new List<Dictionary<string, string>>();

    var lineas = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    if (lineas.Length == 0)
        return filas;

    string[] encabezados;

    if (!config.NoHeader)
    {
        encabezados = lineas[0].Split(config.Delimiter);

        for (int i = 1; i < lineas.Length; i++)
        {
            var valores = lineas[i].Split(config.Delimiter);
            var fila = new Dictionary<string, string>();

            for (int j = 0; j < encabezados.Length; j++)
                fila[encabezados[j]] = valores[j];

            filas.Add(fila);
        }
    }
    else
    {
        var primera = lineas[0].Split(config.Delimiter);
        encabezados = Enumerable.Range(0, primera.Length).Select(i => i.ToString()).ToArray();

        foreach (var linea in lineas)
        {
            var valores = linea.Split(config.Delimiter);
            var fila = new Dictionary<string, string>();

            for (int j = 0; j < encabezados.Length; j++)
                fila[encabezados[j]] = valores[j];

            filas.Add(fila);
        }
    }

    return filas;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> filas, AppConfig config)
{
    if (filas.Count == 0)
        return filas;

    IOrderedEnumerable<Dictionary<string, string>>? ordenadas = null;

    foreach (var campo in config.SortFields)
    {
        Func<Dictionary<string, string>, object> clave = fila =>
        {
            if (!fila.ContainsKey(campo.Name))
                throw new Exception("Columna inexistente: " + campo.Name);

            var valor = fila[campo.Name];

            if (campo.Numeric)
            {
                if (!double.TryParse(valor, out var num))
                    throw new Exception("Valor no numérico: " + valor);

                return num;
            }

            return valor;
        };

        if (ordenadas == null)
        {
            ordenadas = campo.Descending ? filas.OrderByDescending(clave) : filas.OrderBy(clave);
        }
        else
        {
            ordenadas = campo.Descending ? ordenadas.ThenByDescending(clave) : ordenadas.ThenBy(clave);
        }
    }

    return ordenadas.ToList();
}

string Serialize(List<Dictionary<string, string>> filas, AppConfig config)
{
    if (filas.Count == 0)
        return "";

    var sb = new StringBuilder();
    var encabezados = filas[0].Keys.ToList();

    if (!config.NoHeader)
        sb.AppendLine(string.Join(config.Delimiter, encabezados));

    foreach (var fila in filas)
    {
        var valores = encabezados.Select(h => fila[h]);
        sb.AppendLine(string.Join(config.Delimiter, valores));
    }

    return sb.ToString();
}

void WriteOutput(string salida, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile))
        File.WriteAllText(config.OutputFile, salida);
    else
        Console.WriteLine(salida);
}

SortField ParseSortField(string spec)
{
    var partes = spec.Split(':');

    var nombre = partes[0];
    bool numerico = false;
    bool desc = false;

    if (partes.Length > 1)
    {
        if (partes[1] == "num")
            numerico = true;
        else if (partes[1] == "alpha")
            numerico = false;
        else
            throw new Exception("Tipo inválido: " + partes[1]);
    }

    if (partes.Length > 2)
    {
        if (partes[2] == "desc")
            desc = true;
        else if (partes[2] == "asc")
            desc = false;
        else
            throw new Exception("Orden inválido: " + partes[2]);
    }

    return new SortField(nombre, numerico, desc);
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);