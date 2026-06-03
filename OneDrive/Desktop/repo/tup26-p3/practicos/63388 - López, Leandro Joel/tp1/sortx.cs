
record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);

try
{
    var configuracion = ParseArgs(args);
    var texto = ReadInput(configuracion);
    var (filas, encabezados) = ParseDelimited(texto, configuracion);
    ValidateSortFields(filas, encabezados, configuracion);
    var ordenadas = SortRows(filas, configuracion);
    var salida = Serialize(ordenadas, encabezados, configuracion);
    WriteOutput(salida, configuracion);
}
catch (ApplicationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] argumentos)
{
    string? archivoEntrada = null;
    string? archivoSalida = null;
    var delimitador = ",";
    var sinEncabezado = false;
    var camposOrden = new List<SortField>();

    for (var i = 0; i < argumentos.Length; i++)
    {
        var arg = argumentos[i];

        if (arg == "-h" || arg == "--help")
        {
            ShowHelp();
            Environment.Exit(0);
        }

        if (arg == "-i" || arg == "--input")
        {
            i = ExpectArgument(argumentos, i, arg);
            archivoEntrada = argumentos[i];
            continue;
        }

        if (arg == "-o" || arg == "--output")
        {
            i = ExpectArgument(argumentos, i, arg);
            archivoSalida = argumentos[i];
            continue;
        }

        if (arg == "-d" || arg == "--delimiter")
        {
            i = ExpectArgument(argumentos, i, arg);
            delimitador = ParseDelimiter(argumentos[i]);
            continue;
        }

        if (arg == "-nh" || arg == "--no-header")
        {
            sinEncabezado = true;
            continue;
        }

        if (arg == "-b" || arg == "--by")
        {
            i = ExpectArgument(argumentos, i, arg);
            camposOrden.Add(ParseSortField(argumentos[i]));
            continue;
        }

        if (arg.StartsWith("-"))
        {
            throw new ApplicationException($"Opción desconocida: {arg}");
        }

        if (archivoEntrada == null)
        {
            archivoEntrada = arg;
            continue;
        }

        if (archivoSalida == null)
        {
            archivoSalida = arg;
            continue;
        }

        throw new ApplicationException($"Argumento inesperado: {arg}");
    }

    var config = new AppConfig(archivoEntrada, archivoSalida, delimitador, sinEncabezado, camposOrden);
    ValidateSortFieldSpecifications(config);
    return config;

    static void ValidateSortFieldSpecifications(AppConfig config)
    {
        if (!config.NoHeader)
        {
            return;
        }

        for (var i = 0; i < config.SortFields.Count; i++)
        {
            if (!int.TryParse(config.SortFields[i].Name, out _))
            {
                throw new ApplicationException($"Con --no-header, el campo debe ser un índice numérico: {config.SortFields[i].Name}");
            }
        }
    }

    static int ExpectArgument(string[] argumentos, int indice, string opcion)
    {
        if (indice + 1 >= argumentos.Length)
        {
            throw new ApplicationException($"Falta valor para la opción {opcion}");
        }

        return indice + 1;
    }

    static string ParseDelimiter(string valor)
    {
        if (valor == "\\t" || valor == "\t")
        {
            return "\t";
        }

        if (valor.Length == 0)
        {
            throw new ApplicationException("El delimitador no puede estar vacío.");
        }

        return valor;
    }

    static SortField ParseSortField(string especificacion)
    {
        var partes = especificacion.Split(':');
        if (partes.Length == 0 || string.IsNullOrWhiteSpace(partes[0]))
        {
            throw new ApplicationException($"Especificación de campo inválida: {especificacion}");
        }

        var nombre = partes[0];
        var tipo = partes.Length > 1 && !string.IsNullOrWhiteSpace(partes[1]) ? partes[1].ToLowerInvariant() : "alpha";
        var orden = partes.Length > 2 && !string.IsNullOrWhiteSpace(partes[2]) ? partes[2].ToLowerInvariant() : "asc";

        var numerico = tipo switch
        {
            "num" => true,
            "alpha" => false,
            _ => throw new ApplicationException($"Tipo de comparación inválido: {tipo}")
        };

        var descendente = orden switch
        {
            "asc" => false,
            "desc" => true,
            _ => throw new ApplicationException($"Orden inválido: {orden}")
        };

        return new SortField(nombre, numerico, descendente);
    }
}

string ReadInput(AppConfig config)
{
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        return File.ReadAllText(config.InputFile);
    }

    return Console.In.ReadToEnd();
}

(List<Dictionary<string, string>> Filas, string[]? Encabezados) ParseDelimited(string texto, AppConfig config)
{
    var lineas = SplitLines(texto);
    if (lineas.Count == 0)
    {
        return (new List<Dictionary<string, string>>(), config.NoHeader ? null : new string[0]);
    }

    string[]? encabezados = null;
    var indiceInicial = 0;

    if (!config.NoHeader)
    {
        encabezados = SplitLine(lineas[0], config.Delimiter);
        indiceInicial = 1;
    }

    var filas = new List<Dictionary<string, string>>();
    for (var i = indiceInicial; i < lineas.Count; i++)
    {
        var valores = SplitLine(lineas[i], config.Delimiter);
        var fila = new Dictionary<string, string>(StringComparer.Ordinal);

        if (encabezados != null)
        {
            for (var j = 0; j < encabezados.Length; j++)
            {
                fila[encabezados[j]] = j < valores.Length ? valores[j] : string.Empty;
            }

            for (var j = encabezados.Length; j < valores.Length; j++)
            {
                fila[j.ToString()] = valores[j];
            }
        }
        else
        {
            for (var j = 0; j < valores.Length; j++)
            {
                fila[j.ToString()] = valores[j];
            }
        }

        filas.Add(fila);
    }

    return (filas, encabezados);

    static List<string> SplitLines(string texto)
    {
        var resultado = new List<string>();
        if (texto.Length == 0)
        {
            return resultado;
        }

        var actual = string.Empty;
        for (var i = 0; i < texto.Length; i++)
        {
            if (texto[i] == '\r')
            {
                if (i + 1 < texto.Length && texto[i + 1] == '\n')
                {
                    i++;
                }

                resultado.Add(actual);
                actual = string.Empty;
                continue;
            }

            if (texto[i] == '\n')
            {
                resultado.Add(actual);
                actual = string.Empty;
                continue;
            }

            actual += texto[i];
        }

        resultado.Add(actual);
        return resultado;
    }

    static string[] SplitLine(string linea, string delimitador)
    {
        if (linea.Length == 0)
        {
            return Array.Empty<string>();
        }

        return linea.Split(new[] { delimitador }, StringSplitOptions.None);
    }
}

void ValidateSortFields(List<Dictionary<string, string>> filas, string[]? encabezados, AppConfig config)
{
    if (config.SortFields.Count == 0)
    {
        return;
    }

    if (encabezados == null)
    {
        var maxIndice = -1;
        for (var i = 0; i < filas.Count; i++)
        {
            foreach (var clave in filas[i].Keys)
            {
                if (int.TryParse(clave, out var indice) && indice > maxIndice)
                {
                    maxIndice = indice;
                }
            }
        }

        for (var i = 0; i < config.SortFields.Count; i++)
        {
            if (!int.TryParse(config.SortFields[i].Name, out var indice) || indice > maxIndice)
            {
                throw new ApplicationException($"Columna inexistente: {config.SortFields[i].Name}");
            }
        }

        return;
    }

    for (var i = 0; i < config.SortFields.Count; i++)
    {
        var nombre = config.SortFields[i].Name;
        if (!Array.Exists(encabezados, e => e == nombre))
        {
            throw new ApplicationException($"Columna inexistente: {nombre}");
        }
    }
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> filas, AppConfig config)
{
    if (config.SortFields.Count == 0)
    {
        return filas;
    }

    filas.Sort((primera, segunda) => CompareRows(primera, segunda, config.SortFields));
    return filas;

    static int CompareRows(Dictionary<string, string> a, Dictionary<string, string> b, List<SortField> criterios)
    {
        for (var i = 0; i < criterios.Count; i++)
        {
            var criterio = criterios[i];
            var valorA = a.TryGetValue(criterio.Name, out var vA) ? vA : string.Empty;
            var valorB = b.TryGetValue(criterio.Name, out var vB) ? vB : string.Empty;
            int comparacion;

            if (criterio.Numeric)
            {
                var numeroA = ParseDouble(valorA);
                var numeroB = ParseDouble(valorB);
                comparacion = numeroA.CompareTo(numeroB);
            }
            else
            {
                comparacion = StringComparer.OrdinalIgnoreCase.Compare(valorA, valorB);
            }

            if (comparacion != 0)
            {
                return criterio.Descending ? -comparacion : comparacion;
            }
        }

        return 0;
    }

    static double ParseDouble(string texto)
    {
        if (double.TryParse(texto, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var valor))
        {
            return valor;
        }

        return 0.0;
    }
}

string Serialize(List<Dictionary<string, string>> filas, string[]? encabezados, AppConfig config)
{
    var lineas = new List<string>();

    if (encabezados != null)
    {
        lineas.Add(JoinLine(encabezados, config.Delimiter));
    }

    for (var i = 0; i < filas.Count; i++)
    {
        var fila = filas[i];
        var valores = new List<string>();

        if (encabezados != null)
        {
            for (var j = 0; j < encabezados.Length; j++)
            {
                valores.Add(fila.TryGetValue(encabezados[j], out var valor) ? valor : string.Empty);
            }

            var extras = new List<KeyValuePair<int, string>>();
            foreach (var clave in fila.Keys)
            {
                if (Array.Exists(encabezados, e => e == clave))
                {
                    continue;
                }

                if (int.TryParse(clave, out var indice))
                {
                    extras.Add(new KeyValuePair<int, string>(indice, fila[clave]));
                }
            }

            extras.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (var j = 0; j < extras.Count; j++)
            {
                valores.Add(extras[j].Value);
            }
        }
        else
        {
            var maxIndice = -1;
            foreach (var clave in fila.Keys)
            {
                if (int.TryParse(clave, out var indice) && indice > maxIndice)
                {
                    maxIndice = indice;
                }
            }

            for (var j = 0; j <= maxIndice; j++)
            {
                valores.Add(fila.TryGetValue(j.ToString(), out var valor) ? valor : string.Empty);
            }
        }

        lineas.Add(JoinLine(valores.ToArray(), config.Delimiter));
    }

    return string.Join(Environment.NewLine, lineas);

    static string JoinLine(string[] valores, string delimitador)
    {
        var texto = string.Empty;
        for (var i = 0; i < valores.Length; i++)
        {
            if (i > 0)
            {
                texto += delimitador;
            }

            texto += valores[i];
        }

        return texto;
    }
}

void WriteOutput(string texto, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile))
    {
        File.WriteAllText(config.OutputFile, texto);
        return;
    }

    Console.Write(texto);
}

void ShowHelp()
{
    Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]...] [-i|--input input] [-o|--output output] [-d|--delimiter delimitador] [-nh|--no-header] [-h|--help]");
    Console.WriteLine();
    Console.WriteLine("  -b, --by campo[:tipo[:orden]]   Campo por el que ordenar. Se puede repetir para ordenamiento múltiple.");
    Console.WriteLine("  -i, --input input               Archivo de entrada.");
    Console.WriteLine("  -o, --output output             Archivo de salida.");
    Console.WriteLine("  -d, --delimiter delimitador     Carácter delimitador. Default: ,.");
    Console.WriteLine("  -nh, --no-header                Indica que el archivo no tiene fila de encabezado.");
    Console.WriteLine("  -h, --help                      Muestra esta ayuda y termina.");
    Console.WriteLine();
    Console.WriteLine("Especificación de campo: campo[:tipo[:orden]]");
    Console.WriteLine("  campo   Nombre de la columna o índice numérico si no hay encabezado.");
    Console.WriteLine("  tipo    alpha (default) o num.");
    Console.WriteLine("  orden   asc (default) o desc.");
    Console.WriteLine();
    Console.WriteLine("Ejemplos:");
    Console.WriteLine("  sortx empleados.csv -b apellido");
    Console.WriteLine("  sortx empleados.csv resultado.csv -b salario:num:desc");
    Console.WriteLine("  sortx datos.tsv -d \\t -nh -b 1:alpha:asc");
}

