using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

try
{
    //CORRECCIÓN: Se borró el Console.WriteLine que ensuciaba la salida.
    AppConfig config = ParseArgs(args);

    string texto = ReadInput(config);

    var datos = ParseDelimited(texto, config);

    List<Dictionary<string, string>> filasOrdenadas = SortRows(datos.Rows, datos.Headers, config);

    string salida = Serialize(filasOrdenadas, datos.Headers, config);

    WriteOutput(salida, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error: " + ex.Message);

    Environment.Exit(1);
}


// 1. ParseArgs

AppConfig ParseArgs(string[] argumentos)
{ 
    string? input = null;

    string? output = null;

    string separador = ",";

    bool sinHeader = false;

    List<SortField> reglas = new List<SortField>();

    List<string> posicionales = new List<string>();

    for (int i = 0; i < argumentos.Length; i++)
    {

        string actual = argumentos[i].ToLower();

        switch (actual)
        {
            case "-h":
            case "--help":
                
                MostrarAyuda();
                Environment.Exit(0);
                break;

            case "-i":
            case "--input":

                if (i + 1 >= argumentos.Length)
                    throw new Exception("Falta el archivo de entrada.");

                input = argumentos[++i];
                break;

            case "-o":
            case "--output":

                if (i + 1 >= argumentos.Length)
                    throw new Exception("Falta el archivo de salida.");

                output = argumentos[++i];
                break;

            case "-d":
            case "--delimiter":

                if (i + 1 >= argumentos.Length)
                    throw new Exception("Falta el delimitador.");

                separador = argumentos[++i];

                if (separador == "\\t")
                    separador = "\t";

                break;

            case "-nh":
            case "--no-header":

                sinHeader = true;
                break;

            case "-b":
            case "--by":

                if (i + 1 >= argumentos.Length)
                    throw new Exception("Falta el valor para -b / --by.");

                reglas.Add(ParseSortField(argumentos[++i]));
                break;

            default:

                if (!argumentos[i].StartsWith("-"))
                {
                    posicionales.Add(argumentos[i]);
                }
                else
                {
                    throw new Exception("Opción no válida: " + argumentos[i]);
                }
                break;
        }
    }

    if (input == null && posicionales.Count > 0)
        input = posicionales[0];

    if (output == null && posicionales.Count > 1)
        output = posicionales[1];

    if (posicionales.Count > 2)
        throw new Exception("Hay demasiados argumentos posicionales.");

    if (reglas.Count == 0)
        throw new Exception("Debes indicar al menos un campo de orden con -b.");

    return new AppConfig(input, output, separador, sinHeader, reglas);

    void MostrarAyuda()
    {
        Console.WriteLine("Uso:");
        Console.WriteLine("sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
        Console.WriteLine("      [-i|--input input] [-o|--output output]");
        Console.WriteLine("      [-d|--delimiter delimitador]");
        Console.WriteLine("      [-nh|--no-header] [-h|--help]");
        Console.WriteLine();
        Console.WriteLine("Ejemplos:");
        Console.WriteLine("sortx empleados.csv -b apellido");
        Console.WriteLine("sortx empleados.csv -b salario:num:desc");
        Console.WriteLine("sortx datos.tsv -d \"\\t\" -nh -b 1:alpha:asc");
    }
}

SortField ParseSortField(string texto)
{
    string[] partes = texto.Split(':');

    if (partes.Length == 0 || string.IsNullOrWhiteSpace(partes[0]))
        throw new Exception("Campo inválido en --by.");

    string nombre = partes[0];

    bool esNumerico = false;
    bool esDescendente = false;

    if (partes.Length > 1)
    {
        string tipo = partes[1].ToLower();

        if (tipo == "num")
            esNumerico = true;
        else if (tipo == "alpha")
            esNumerico = false;
        else
            throw new Exception("Tipo inválido en --by: " + partes[1]);
    }

    if (partes.Length > 2)
    {
        string orden = partes[2].ToLower();

        if (orden == "desc")
            esDescendente = true;
        else if (orden == "asc")
            esDescendente = false;
        else
            throw new Exception("Orden inválido en --by: " + partes[2]);
    }

    if (partes.Length > 3)
        throw new Exception("Formato inválido en --by: " + texto);

    return new SortField(nombre, esNumerico, esDescendente);
}

// 2. ReadInput

string ReadInput(AppConfig config)
{
    if (config.InputFile != null)
    {
        if (!File.Exists(config.InputFile))
            throw new Exception("No existe el archivo de entrada: " + config.InputFile);

        return File.ReadAllText(config.InputFile);
    }

    return Console.In.ReadToEnd();
}

// 3. ParseDelimited

(List<string> Headers, List<Dictionary<string, string>> Rows) ParseDelimited(string texto, AppConfig config)
{
    List<string> lineas = texto
        .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
        .ToList();

    if (lineas.Count == 0)
        return (new List<string>(), new List<Dictionary<string, string>>());

    List<string> headers = new List<string>();

    List<Dictionary<string, string>> filas = new List<Dictionary<string, string>>();

    if (config.NoHeader == false)
    {
        headers = lineas[0].Split(config.Delimiter).ToList();

        for (int i = 1; i < lineas.Count; i++)
        {
            string[] valores = lineas[i].Split(config.Delimiter);

            if (valores.Length != headers.Count)
                throw new Exception("La línea tiene distinta cantidad de columnas: " + lineas[i]);

            Dictionary<string, string> fila = new Dictionary<string, string>();

            for (int j = 0; j < headers.Count; j++)
            {
                fila[headers[j]] = valores[j];
            }

            filas.Add(fila);
        }
    }
    else
    {
        string[] primeraFila = lineas[0].Split(config.Delimiter);

        for (int i = 0; i < primeraFila.Length; i++)
        {
            headers.Add(i.ToString());
        }

        for (int i = 0; i < lineas.Count; i++)
        {
            string[] valores = lineas[i].Split(config.Delimiter);

            if (valores.Length != headers.Count)
                throw new Exception("La línea tiene distinta cantidad de columnas: " + lineas[i]);

            Dictionary<string, string> fila = new Dictionary<string, string>();

            for (int j = 0; j < headers.Count; j++)
            {
                fila[headers[j]] = valores[j];
            }

            filas.Add(fila);
        }
    }

    return (headers, filas);
}

// 4. SortRows

List<Dictionary<string, string>> SortRows(
    List<Dictionary<string, string>> rows,
    List<string> headers,
    AppConfig config)
{
    for (int i = 0; i < config.SortFields.Count; i++)
    {
        if (!headers.Contains(config.SortFields[i].Name))
            throw new Exception("La columna no existe: " + config.SortFields[i].Name);
    }

    List<Dictionary<string, string>> resultado = new List<Dictionary<string, string>>(rows);

    resultado.Sort((a, b) =>
    {

        for (int i = 0; i < config.SortFields.Count; i++)
        {
            SortField regla = config.SortFields[i];
            int comparacion = 0;

            if (regla.Numeric)
            {
                // CORRECCIÓN: Se intenta convertir. Si falla (ej: celda vacía), asumimos que es 0 en vez de explotar.
                decimal valorA = decimal.TryParse(a[regla.Name], out decimal resA) ? resA : 0;
                decimal valorB = decimal.TryParse(b[regla.Name], out decimal resB) ? resB : 0;

                comparacion = valorA.CompareTo(valorB);
            }

            else
            {
                comparacion = string.Compare(a[regla.Name], b[regla.Name], true);
            }

            if (regla.Descending)
                comparacion = comparacion * -1;

            if (comparacion != 0)
                return comparacion;
        }

        return 0;
    });

    return resultado;
}

// 5. Serialize

string Serialize(List<Dictionary<string, string>> rows, List<string> headers, AppConfig config)
{
    List<string> lineas = new List<string>();

    if (config.NoHeader == false)
    {
        lineas.Add(string.Join(config.Delimiter, headers));
    }

    for (int i = 0; i < rows.Count; i++)
    {
        List<string> valores = new List<string>();

        for (int j = 0; j < headers.Count; j++)
        {
            valores.Add(rows[i][headers[j]]);
        }

        lineas.Add(string.Join(config.Delimiter, valores));
    }

    return string.Join(Environment.NewLine, lineas);
}

// 6. WriteOutput

void WriteOutput(string texto, AppConfig config)
{
    if (config.OutputFile != null)
    {
        File.WriteAllText(config.OutputFile, texto);
    }
    else
    {
        Console.Write(texto);
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