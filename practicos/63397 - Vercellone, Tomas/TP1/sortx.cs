try
{
    AppConfig config = ParseArgs(args);

    string input = ReadInput(config.InputFile);

    var filas = ParseDelimited(input, config.Delimiter);

    var filasOrdenadas = SortRows(filas, config);

    string output = Serialize(filasOrdenadas, config.Delimiter);

    WriteOutput(output, config.OutputFile);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    List<SortField> sortFields = new();

    int i = 0;
    while (i < args.Length)
    {
        string arg = args[i];

        if (arg == "-b" || arg == "--by")
        {
            if (i + 1 >= args.Length)
                throw new Exception("Falta valor para -b");
            i++;
            sortFields.Add(ParseSortField(args[i]));
        }
        else if (arg == "-i" || arg == "--input")
        {
            if (i + 1 >= args.Length)
                throw new Exception("Falta archivo para -i");
            i++;
            inputFile = args[i];
        }
        else if (arg == "-o" || arg == "--output")
        {
            if (i + 1 >= args.Length)
                throw new Exception("Falta archivo para -o");
            i++;
            outputFile = args[i];
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            if (i + 1 >= args.Length)
                throw new Exception("Falta delimitador");
            i++;
            delimiter = args[i];
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            noHeader = true;
        }
        else if (arg == "-h" || arg == "--help")
        {
            Console.WriteLine("Uso: sortx [archivo] -b campo[:num][:desc] [-o salida] [-d delim] [-nh]");
            Environment.Exit(0);
        }
        else
        {
            inputFile = arg;
        }

        i++;
    }

    if (sortFields.Count == 0)
        throw new Exception("Debe especificar al menos un campo de ordenamiento (-b)");

    if (inputFile == null && !Console.IsInputRedirected)
        throw new Exception("Debe especificar un archivo de entrada o usar stdin");

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

SortField ParseSortField(string text)
{
    string[] partes = text.Split(':');

    string name = partes[0];
    bool numeric = partes.Length > 1 && partes[1] == "num";
    bool descending = partes.Length > 2 && partes[2] == "desc";

    return new SortField(name, numeric, descending);
}

string ReadInput(string? filePath)
{
    if (filePath == null)
        return Console.In.ReadToEnd();

    if (!File.Exists(filePath))
        throw new Exception("El archivo no existe");

    return File.ReadAllText(filePath);
}

List<string[]> ParseDelimited(string input, string delimiter)
{
    List<string[]> filas = new();

    string[] lineas = input.Split('\n');

    foreach (var linea in lineas)
    {
        if (string.IsNullOrWhiteSpace(linea)) continue;
        filas.Add(linea.TrimEnd('\r').Split(delimiter));
    }

    return filas;
}

List<string[]> SortRows(List<string[]> filas, AppConfig config)
{
    if (filas.Count == 0 || config.SortFields.Count == 0)
        return filas;

    int start = 0;
    string[]? header = null;

    if (!config.NoHeader)
    {
        header = filas[0];
        start = 1;
    }

    var datos = filas.Skip(start).ToList();

    if (datos.Count == 0)
        return filas;

    SortField first = config.SortFields[0];
    int firstIndex = GetColumnIndex(first, header);

    IOrderedEnumerable<string[]> sorted = first.Numeric
        ? (first.Descending
            ? datos.OrderByDescending(f => SafeParse(f, firstIndex))
            : datos.OrderBy(f => SafeParse(f, firstIndex)))
        : (first.Descending
            ? datos.OrderByDescending(f => GetSafeValue(f, firstIndex))
            : datos.OrderBy(f => GetSafeValue(f, firstIndex)));

    for (int i = 1; i < config.SortFields.Count; i++)
    {
        SortField field = config.SortFields[i];
        int index = GetColumnIndex(field, header);

        sorted = field.Numeric
            ? (field.Descending
                ? sorted.ThenByDescending(f => SafeParse(f, index))
                : sorted.ThenBy(f => SafeParse(f, index)))
            : (field.Descending
                ? sorted.ThenByDescending(f => GetSafeValue(f, index))
                : sorted.ThenBy(f => GetSafeValue(f, index)));
    }

    var resultado = sorted.ToList();

    if (header != null)
        resultado.Insert(0, header);

    return resultado;
}

double SafeParse(string[] fila, int index)
{
    if (index >= fila.Length) return 0;
    if (double.TryParse(fila[index], out double val)) return val;
    return 0;
}

string GetSafeValue(string[] fila, int index)
{
    if (index >= fila.Length) return "";
    return fila[index];
}

int GetColumnIndex(SortField field, string[]? header)
{
    if (header == null)
    {
        if (!int.TryParse(field.Name, out int index))
            throw new Exception("Índice inválido");
        return index;
    }

    for (int i = 0; i < header.Length; i++)
    {
        if (header[i].Equals(field.Name, StringComparison.OrdinalIgnoreCase))
            return i;
    }

    throw new Exception($"La columna '{field.Name}' no existe en el archivo.");
}

string Serialize(List<string[]> filas, string delimiter)
{
    List<string> lineas = new();

    foreach (var fila in filas)
        lineas.Add(string.Join(delimiter, fila));

    return string.Join("\n", lineas);
}

void WriteOutput(string text, string? filePath)
{
    if (filePath == null)
    {
        Console.WriteLine(text);
    }
    else
    {
        File.WriteAllText(filePath, text);
    }
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
