
using System.Globalization;

try
{
    var config = ParseArgs(args);
    var texto = LeerEntrada(config);
    var (filas, encabezados, lineaEncabezado) = ParsearDelimitado(texto, config);
    var ordenadas = OrdenarFilas(filas, config, encabezados);
    var salida = Serializar(ordenadas, config, encabezados, lineaEncabezado);
    EscribirSalida(salida, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}


AppConfig ParseArgs(string[] args)
{
    string? entrada = null;
    string? salida = null;
    string delimitador = ",";
    bool sinEncabezado = false;
    var campos = new List<SortField>();

    for (int i = 0; i < args.Length; i++)
    {
        var a = args[i];

        if (a == "-h" || a == "--help")
        {
            Console.WriteLine("sortx [input [output]] [-b campo[:tipo[:orden]]]...");
            Environment.Exit(0);
        }
        else if (a == "-i" || a == "--input")
        {
            entrada = args[++i];
        }
        else if (a == "-o" || a == "--output")
        {
            salida = args[++i];
        }
        else if (a == "-d" || a == "--delimiter")
        {
            var d = args[++i];
            delimitador = d == "\\t" ? "\t" : d;
        }
        else if (a == "-nh" || a == "--no-header")
        {
            sinEncabezado = true;
        }
        else if (a == "-b" || a == "--by")
        {
            var spec = args[++i];
            var partes = spec.Split(':');

            var nombre = partes[0];
            bool numerico = partes.Length > 1 && partes[1] == "num";
            bool desc = partes.Length > 2 && partes[2] == "desc";

            campos.Add(new SortField(nombre, numerico, desc));
        }
        else
        {
            if (entrada == null) entrada = a;
            else if (salida == null) salida = a;
        }
    }

    return new AppConfig(entrada, salida, delimitador, sinEncabezado, campos);
}

string LeerEntrada(AppConfig config)
{
    if (config.ArchivoEntrada != null)
        return File.ReadAllText(config.ArchivoEntrada);

    return Console.In.ReadToEnd();
}

(List<Dictionary<string, string>>, List<string>, string?)
ParsearDelimitado(string texto, AppConfig config)
{
    var lineas = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    var filas = new List<Dictionary<string, string>>();
    var encabezados = new List<string>();
    string? lineaEncabezado = null;

    int inicio = 0;

    if (!config.SinEncabezado)
    {
        lineaEncabezado = lineas[0];
        encabezados = lineas[0].Split(config.Delimitador).ToList();
        inicio = 1;
    }
    else
    {
        var cols = lineas[0].Split(config.Delimitador).Length;
        for (int i = 0; i < cols; i++)
            encabezados.Add(i.ToString());
    }

    for (int i = inicio; i < lineas.Length; i++)
    {
        var valores = lineas[i].Split(config.Delimitador);
        var dic = new Dictionary<string, string>();

        for (int j = 0; j < encabezados.Count; j++)
        {
            var val = j < valores.Length ? valores[j] : "";
            dic[encabezados[j]] = val;
        }

        filas.Add(dic);
    }

    return (filas, encabezados, lineaEncabezado);
}

List<Dictionary<string, string>> OrdenarFilas(
    List<Dictionary<string, string>> filas,
    AppConfig config,
    List<string> encabezados)
{
    IOrderedEnumerable<Dictionary<string, string>>? ordenado = null;

    foreach (var campo in config.CamposOrden)
    {
        if (!encabezados.Contains(campo.Nombre))
            throw new Exception($"Campo inexistente: {campo.Nombre}");

        Func<Dictionary<string, string>, object> selector = fila =>
        {
            var val = fila[campo.Nombre];

            if (campo.Numerico)
            {
                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
                    return n;
                return 0.0;
            }

            return val;
        };

        if (ordenado == null)
        {
            ordenado = campo.Descendente
                ? filas.OrderByDescending(selector)
                : filas.OrderBy(selector);
        }
        else
        {
            ordenado = campo.Descendente
                ? ordenado.ThenByDescending(selector)
                : ordenado.ThenBy(selector);
        }
    }

    return ordenado?.ToList() ?? filas;
}

string Serializar(
    List<Dictionary<string, string>> filas,
    AppConfig config,
    List<string> encabezados,
    string? lineaEncabezado)
{
    var lineas = new List<string>();

    if (!config.SinEncabezado && lineaEncabezado != null)
        lineas.Add(lineaEncabezado);

    foreach (var fila in filas)
    {
        var valores = encabezados.Select(h => fila.ContainsKey(h) ? fila[h] : "");
        lineas.Add(string.Join(config.Delimitador, valores));
    }

    return string.Join(Environment.NewLine, lineas);
}

void EscribirSalida(string salida, AppConfig config)
{
    if (config.ArchivoSalida != null)
        File.WriteAllText(config.ArchivoSalida, salida);
    else
        Console.WriteLine(salida);
}


record SortField(string Nombre, bool Numerico, bool Descendente);

record AppConfig(
    string? ArchivoEntrada,
    string? ArchivoSalida,
    string Delimitador,
    bool SinEncabezado,
    List<SortField> CamposOrden
);