#nullable disable
System.Console.OutputEncoding = System.Text.Encoding.UTF8;
string inputFile = null;
string outputFile = null;
string delimiter = ",";
bool noHeader = false;
bool helpRequested = false;

System.Collections.Generic.List<string> posicionales = new System.Collections.Generic.List<string>();
System.Collections.Generic.List<string> sortFields = new System.Collections.Generic.List<string>();

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-h" || args[i] == "--help")
    {
        helpRequested = true;
    }
    else if (args[i] == "-nh" || args[i] == "--no-header")
    {
        noHeader = true;
    }
    else if (args[i] == "-d" || args[i] == "--delimiter")
    {
        if (i + 1 < args.Length)
        {
            delimiter = args[i + 1];

            if (delimiter == "\\t")
            {
                delimiter = "\t";
            }

            i++;
        }
        else
        {
            System.Console.Error.WriteLine("Error: Debes indicar un delimitador después de -d o --delimiter.");
            System.Environment.Exit(1);
        }
    }
    else if (args[i] == "-i" || args[i] == "--input")
    {
        if (i + 1 < args.Length)
        {
            inputFile = args[i + 1];
            i++;
        }
        else
        {
            System.Console.Error.WriteLine("Error: Debes indicar un archivo después de -i o --input.");
            System.Environment.Exit(1);
        }
    }
    else if (args[i] == "-o" || args[i] == "--output")
    {
        if (i + 1 < args.Length)
        {
            outputFile = args[i + 1];
            i++;
        }
        else
        {
            System.Console.Error.WriteLine("Error: Debes indicar un archivo después de -o o --output.");
            System.Environment.Exit(1);
        }
    }
    else if (args[i] == "-b" || args[i] == "--by")
    {
        if (i + 1 < args.Length)
        {
            sortFields.Add(args[i + 1]);
            i++;
        }
        else
        {
            System.Console.Error.WriteLine("Error: Debes indicar un campo después de -b o --by.");
            System.Environment.Exit(1);
        }
    }
    else
    {
        posicionales.Add(args[i]);
    }
}

if (inputFile == null && posicionales.Count > 0)
{
    inputFile = posicionales[0];
}

if (outputFile == null && posicionales.Count > 1)
{
    outputFile = posicionales[1];
}

if (helpRequested)
{
    MostrarAyuda();
    return;
}

string texto = "";

if (inputFile != null)
{
    texto = System.IO.File.ReadAllText(inputFile, System.Text.Encoding.UTF8);
}
else
{
    texto = System.Console.In.ReadToEnd();
}

string[] lineas = texto.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

if (lineas.Length == 0)
{
    EscribirSalida(outputFile, "");
    return;
}

string header = "";
int inicioDatos = 0;

if (!noHeader)
{
    header = lineas[0];
    inicioDatos = 1;
}

System.Collections.Generic.List<string[]> filas = new System.Collections.Generic.List<string[]>();

for (int i = inicioDatos; i < lineas.Length; i++)
{
    string[] partes = lineas[i].Split(delimiter);
    filas.Add(partes);
}

string[] nombresColumnas = null;

if (!noHeader)
{
    nombresColumnas = header.Split(delimiter);
}

if (sortFields.Count == 0)
{
    System.Console.Error.WriteLine("Error: Debes indicar al menos un campo con -b o --by.");
    System.Environment.Exit(1);
}

for (int i = sortFields.Count - 1; i >= 0; i--)
{
    string criterio = sortFields[i];
    string[] partesCriterio = criterio.Split(':');

    string campo = partesCriterio[0];
    string tipo = "alpha";
    string orden = "asc";

    if (partesCriterio.Length > 1)
    {
        tipo = partesCriterio[1];
    }

    if (partesCriterio.Length > 2)
    {
        orden = partesCriterio[2];
    }

    int indiceCampo = -1;

    if (noHeader)
    {
        bool pudo = int.TryParse(campo, out indiceCampo);
        if (!pudo || indiceCampo < 0)
        {
            System.Console.Error.WriteLine("Error: Con --no-header el campo debe ser un índice numérico mayor o igual a 0.");
            System.Environment.Exit(1);
        }
    }
    else
    {
        for (int j = 0; j < nombresColumnas.Length; j++)
        {
            if (nombresColumnas[j] == campo)
            {
                indiceCampo = j;
                break;
            }
        }

        if (indiceCampo == -1)
        {
            System.Console.Error.WriteLine("Error: La columna '" + campo + "' no existe.");
            System.Environment.Exit(1);
        }
    }

    if (tipo != "alpha" && tipo != "num")
    {
        System.Console.Error.WriteLine("Error: El tipo debe ser 'alpha' o 'num'.");
        System.Environment.Exit(1);
    }

    if (orden != "asc" && orden != "desc")
    {
        System.Console.Error.WriteLine("Error: El orden debe ser 'asc' o 'desc'.");
        System.Environment.Exit(1);
    }

    bool numerico = (tipo == "num");
    bool descendente = (orden == "desc");

    filas.Sort(delegate (string[] a, string[] b)
    {
        string valorA = "";
        string valorB = "";

        if (indiceCampo < a.Length)
        {
            valorA = a[indiceCampo];
        }

        if (indiceCampo < b.Length)
        {
            valorB = b[indiceCampo];
        }

        int resultado = 0;

        if (numerico)
        {
            double numA = 0;
            double numB = 0;

            double.TryParse(valorA, out numA);
            double.TryParse(valorB, out numB);

            resultado = numA.CompareTo(numB);
        }
        else
        {
            resultado = string.Compare(valorA, valorB, System.StringComparison.OrdinalIgnoreCase);
        }

        if (descendente)
        {
            resultado = -resultado;
        }

        return resultado;
    });
}

System.Collections.Generic.List<string> lineasSalida = new System.Collections.Generic.List<string>();

// Header solo si corresponde
if (!noHeader)
{
    lineasSalida.Add(header);
}

// Filas
for (int i = 0; i < filas.Count; i++)
{
    lineasSalida.Add(string.Join(delimiter, filas[i]));
}

// Unir SIN línea extra al final
string resultado = string.Join("\n", lineasSalida);

EscribirSalida(outputFile, resultado);

void MostrarAyuda()
{



    System.Console.WriteLine("Uso:");
    System.Console.WriteLine("sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
    System.Console.WriteLine("      [-i|--input input] [-o|--output output]");
    System.Console.WriteLine("      [-d|--delimiter delimitador]");
    System.Console.WriteLine("      [-nh|--no-header] [-h|--help]");
    System.Console.WriteLine("");
    System.Console.WriteLine("Ejemplos:");
    System.Console.WriteLine("sortx datos.csv -b edad:num:desc -b nombre:alpha:asc");
    System.Console.WriteLine("Opciones:");
    System.Console.WriteLine("-b  | --by         Campo por el que ordenar. Formato: campo[:alpha|num[:asc|desc]]");
    System.Console.WriteLine("-i  | --input      Archivo de entrada");
    System.Console.WriteLine("-o  | --output     Archivo de salida");
  System.Console.WriteLine("-d  | --delimiter  Delimitador. Por defecto: ,");
    System.Console.WriteLine("-nh | --no-header  Indica que no hay encabezado");
  System.Console.WriteLine("-h  | --help       Muestra esta ayuda");
}

void EscribirSalida(string archivoSalida, string contenido)
{
    if (archivoSalida != null)
    {
              System.IO.File.WriteAllText(archivoSalida, contenido, System.Text.Encoding.UTF8);
    }
    else
    {
        System.Console.Write(contenido);
    }
}