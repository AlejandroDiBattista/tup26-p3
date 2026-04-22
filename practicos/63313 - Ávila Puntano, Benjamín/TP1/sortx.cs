
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

using System.Drawing;
using System.Net.Http.Headers;
using Microsoft.VisualBasic.FileIO;

Console.WriteLine($"sortx {string.Join(" ", args)}");

try{
// este appconfig guarda los datos temporalmente, a diferencia del record que los toma al final
AppConfig parseargs(string[] args)
{
    string? inputFile = null; 
    string? outputFile = null; 
    string delimiter = ","; 
    bool noHeader = false; 
    bool showHelp = false;
    List<SortField> sortFields = new List<SortField>(); 
    int positional = 0; 

    SortField ParseSortField(string spec)
    {
        var parts = spec.Split(':');
        string name = parts[0];
        bool numeric = parts.Length > 1 && parts[1].Equals("num", StringComparison.OrdinalIgnoreCase);
        bool descending = parts.Length > 2 && parts[2].Equals("desc", StringComparison.OrdinalIgnoreCase);
        return new SortField(name, numeric, descending);}
    

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];

        if (arg == "--help" || arg == "-h")
        {
            showHelp = true;
            continue;
        }

        if (arg == "--no-header" || arg == "-nh")
        {
            noHeader = true;
            continue;
        }

        if (arg == "--by" || arg == "-b")
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"necesita '{arg}' un valor.");

            sortFields.Add(ParseSortField(args[++i]));
            continue;
        }

        if (arg == "--input" || arg == "-i")
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"necesita '{arg}' un valor.");

            inputFile = args[++i];
            continue;
        }

        if (arg == "--output" || arg == "-o")
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"necesita '{arg}' un valor.");

            outputFile = args[++i];
            continue;
        }

        if (arg == "--delimiter" || arg == "-d")
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"necesita '{arg}' un valor.");

            string raw = args[++i];
            delimiter = raw == @"\t" ? "\t" : raw;
            continue;
        }

        if (!arg.StartsWith("-"))
        {
            if (positional == 0) { inputFile = arg; positional++; }
            else if (positional == 1) { outputFile = arg; positional++; }
            else throw new ArgumentException($"arg posicional inexistente: '{arg}'.");
            continue;
        }

        throw new ArgumentException($"opción desconocida: '{arg}'.");
    }

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, showHelp, sortFields);

}
string readinput(AppConfig cfg)
{
    //  se verifica que el archivo cfg no sea null (los datos parseados)
    if(cfg.InputFile != null) 
      return File.ReadAllText(cfg.InputFile); 
    return Console.In.ReadToEnd(); 
}
// lista de fila y encabezado en base al texto de archivo cfg
(List<Dictionary<string,string>> rows, string[]? headers) ParseDelimited(string text, AppConfig cfg)
{
    // el templines va a tener todas las lineas incluyendo las vacias tambien, despues con el split separa en lineas y por ultimo se eliminan las lineas vacias.
    var tempLines = text
        .Replace("\r\n", "\n")
        .Replace("\r", "\n")
        .Split('\n');
    string[] lines = Array.FindAll(tempLines, l => l.Length > 0);

    if (lines.Length == 0)
        return (new List<Dictionary<string, string>>(), null);

    string[]? headers;
    int dataStart;

    if (!cfg.NoHeader)
    {
        headers = lines[0].Split(cfg.Delimiter);
        dataStart = 1;
    }
    else
    {
        headers = null;
        dataStart = 0;
    }
    
    var rows = new List<Dictionary<string, string>>(); //guardo los datos de listas en esta variable rows
    for (int lineIdx = dataStart; lineIdx < lines.Length; lineIdx++) // recorre las lineas del archivo en base al data start y si es q hay encabezado o no
    {
        var values = lines[lineIdx].Split(cfg.Delimiter); // se guardan los datos spliteados en values
        var row = new Dictionary<string, string>(); // se crea una carpeta vacia para guardar los datos de cada fila.

        if (!cfg.NoHeader && headers is not null)
        {
            for (int col = 0; col < headers.Length; col++) // bucle q recorre las columnas de las columnas 0 al 3
            {
                row[headers[col]] = col < values.Length ? values[col] : string.Empty; // se guarda en  la var row el valor de cada columna
            }
        }
        else
        {
            for (int col = 0; col < values.Length; col++)
            {
                row[col.ToString()] = values[col];
            }
        }
        rows.Add(row);
    }
    
    if (cfg.SortFields.Count > 0 && rows.Count > 0) //toma en cuenta la cantidad de elementos de sort fields y la cantidad de elementos de row 
    {
        var firstrow = rows[0]; // guarda el valor de la primera fila de rows en la variable firstrow
        foreach (var sf in cfg.SortFields) //bucle en donde la variable sf es temporal y toma los datos de cfg sort fields
        {
            if (!firstrow.ContainsKey(sf.Name)) // si no existe la condicion de que la primera fila contenga la clave del campo de ordenamiento
            {
                string available = "";
                foreach (var key in firstrow.Keys) //bucle mostrando las columnas de row y se guardan en key temporalmente 
                {
                    if (available.Length > 0) 
                        available += ", ";
                    available += key; 
                    // se verifica si la var available tiene algun valor, si es que tiene se le agrega una , y despues el nombre de la columna que esta en key
                }
                throw new ArgumentException($"campo de ordenamiento desconocido: '{sf.Name}'. disponible: {available}"); // error si es que la primera fila no tiene clave sf
            }
        }
    }
    
    List<Dictionary<string, string>> sortrows(List<Dictionary<string, string>> rows, List<SortField> sortFields)
    {
        if (sortFields.Count == 0 || rows.Count == 0) 
            return rows; // si no hay criterio de ordernamiento o no hay filas se devuelve las filas sin ordenar
        
        rows.Sort((a, b) => // ordena la lista a y b 
        {
            foreach (var sf in sortFields) // recorremos los criterios de ordenamiento para comparar las filas a y b y se guardan en sf temporalmente
            {
                int cmp = 0;  // aqui se guardara e l resultado de la comparacion.

                if (sf.Numeric) 
                {
                    // creacion de 2 variables a y b para guardar los valores numericos de las filas a y b , se inician en 0 por si no se pueden parsear a numero
                    double va = 0; 
                    double vb = 0; 

                    //en caso de ser string se parsean a numero, si no se pueden parsear quedan en 0, se guardan en va y vb.
                    double.TryParse(a[sf.Name], out va); 
                    double.TryParse(b[sf.Name], out vb); 

                    cmp = va.CompareTo(vb); // se guarda en cmp el resultado de la comparacion entre las dos variables
                }
                else
                {
                    // verificacion de que los valores de a y b no sean null, si son null se ponen como ""  de lo contrario quedan con el valor orig
                    string va = a[sf.Name] ?? ""; 
                    string vb = b[sf.Name] ?? "";
                    cmp = string.Compare(va, vb, StringComparison.Ordinal); // comparacion guardada en cmp
                }

                if(cmp != 0) // si la comparacion es igual a 0 se debe decidir cual variable va primero, si cmp es distinto de 0 se devuelve el resultado de la comparacion teniendo en cuenta el ordenamiento ascendente o descendente
                {
                    if (sf.Descending)  // mayor a menor
                        return -cmp;   // devuelve la fila que tenga el valor mayor
                    else
                        return cmp; // lo contrario la menor.
                }
            }
                return 0;
        });
        return rows;
    }

    if (cfg.SortFields.Count > 0 && rows.Count > 0)
        rows = sortrows(rows, cfg.SortFields);

    return (rows, headers);
}

string serialize(List<Dictionary<string, string>> rows, string[]? header, AppConfig cfg)
{
    var lines = new List<string>();  // lista string para guardar las lineas a escribir en el archivo de salida

    
    if (!cfg.NoHeader && header != null)
    {
        lines.Add(string.Join(cfg.Delimiter, header)); //s i hay encabezado se agrega a las lineas el encabezado con el delimitador

        foreach (var row in rows) // bucle que recorre las filas
        {
            string line = ""; // variable temporal para guardar la linea a escribir en el archivo de salida

            for (int i = 0; i < header.Length; i++) // for q recorre  las columnas del encabezado
            {
                if (i > 0)
                    line += cfg.Delimiter; // si la columna es mayor a 0 se agrega el delimitador 

                if (row.ContainsKey(header[i])) // si la linea contiene la clave 
                    line += row[header[i]];
                else
                    line += ""; // si no contiene la clave se agrega un valor vacio
            }

            lines.Add(line); // se agrega la linea a la lista de lineas
        }
    }
    else
    {
        foreach (var row in rows) //bucle que recorre todas las filas si no hay un headr
        {
            string line = ""; // variable temporal para guardar la linea a escribir en el archivo de salid

            for (int i = 0; i < row.Count; i++) // bucle que recorre del 0 a 3
            {
                if (i > 0)
                    line += cfg.Delimiter; // si la columna es mayor a 0 se agrega el delimitador

                string key = i.ToString(); // convierte el numero de columna a string  y lo guarda en key

                if (row.ContainsKey(key)) // si la linea contiene la clave de la columna se agrega el valor a la linea
                    line += row[key];
            }

            lines.Add(line);
        }
    }

    return string.Join("\n", lines); // se devuelve un string con todas las lineas unidas con salto de linea 
}

    void writeoutput(string text, AppConfig cfg) // 
    {
        if (cfg.OutputFile != null)  // se verifica que el archivo de salida no sea null
            File.WriteAllText(cfg.OutputFile, text); // si el archivo de salida no es null se escribe el texto en el archivo de salida
        else
            Console.Write(text); // si el archivo de salida es null se escribe el texto en la consola
     }

    void ShowHelp()
    {
    
      Console.WriteLine("""
        sortx — Ordena archivos de texto delimitados (CSV, TSV, PSV, etc.)

        USO:
          sortx [input [output]] [-b campo[:tipo[:orden]]]... [opciones]
          dotnet run sortx.cs -- [input [output]] [-b campo[:tipo[:orden]]]... [opciones]

        OPCIONES:
          -b, --by campo[:tipo[:orden]]   Campo de ordenamiento (repetible).
                                            tipo  : alpha (default) | num
                                            orden : asc  (default)  | desc
          -i, --input  <archivo>          Archivo de entrada  (default: stdin).
          -o, --output <archivo>          Archivo de salida   (default: stdout).
          -d, --delimiter <delim>         Delimitador         (default: ,).
                                            Usar \t para tabulación.
          -nh, --no-header                Sin encabezado; campos por índice (0, 1, 2...).
          -ay,  --ayuda                     Muestra esta ayuda y termina.

        EJEMPLOS:
          sortx empleados.csv -b apellido
          sortx empleados.csv -b salario:num:desc
          sortx empleados.csv -b departamento -b salario:num:desc
          sortx empleados.csv resultado.csv -b apellido
          sortx -i empleados.csv -o resultado.csv -b apellido
          sortx datos.tsv -d "\t" -nh -b 1:alpha:asc
          cat empleados.csv | sortx -b apellido > ordenado.csv
        """);
    }

    var config = parseargs(args);

    if (config.ShowHelp)
    {
        ShowHelp();
        return; // exit 0
    }

    var rawText = readinput(config);
    var (rows, headers) = ParseDelimited(rawText, config);
    var output = serialize(rows, headers, config);
    writeoutput(output, config);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Error de argumentos: {ex.Message}");
    Console.Error.WriteLine("Use --help para ver la ayuda.");
    Environment.Exit(1);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"Error: Archivo no encontrado — {ex.FileName}");
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error inesperado: {ex.Message}");
    Environment.Exit(1);
}
 //commit 1
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    bool ShowHelp,
    List<SortField> SortFields
);          
//Testeo  T2 con el comando "dotnet run empleados.csv -b apellido" y devuelve los datos: //ordena por apellido
//nombre,apellido,edad,salario,departamento
//Carlos,García,35,85000,Ingeniería
//Laura,González,38,95000,Gerencia
//María,López,31,88000,Ingeniería
//Ana,Martínez,28,72000,Diseño
//Luis,Rodríguez,42,120000,Gerencia
//Pedro,Sánchez,25,65000,Diseño

//Testeo T3 con el comando "dotnet run empleados.csv -b salario:num:asc" y devuelve los datos: ordena por salario ascendente
//nombre,apellido,edad,salario,departamento
//Pedro,Sánchez,25,65000,Diseño
//Ana,Martínez,28,72000,Diseño
//Carlos,García,35,85000,Ingeniería
//María,López,31,88000,Ingeniería
//Laura,González,38,95000,Gerencia
//Luis,Rodríguez,42,120000,Gerencia

//Testeo T4 con el comando "dotnet run empleados.csv -b departamento -b salario:num:desc" y devuelve los datos: ordena por depto y salario asc
//nombre,apellido,edad,salario,departamento
//Ana,Martínez,28,72000,Diseño
//Pedro,Sánchez,25,65000,Diseño
//Luis,Rodríguez,42,120000,Gerencia
//Laura,González,38,95000,Gerencia
//María,López,31,88000,Ingeniería
//Carlos,García,35,85000,Ingeniería

//Testeo T6 COn el comando "dotnet run -- -b salario:num:desc" permite escribir en la consola puse los sig datos:
//nombre,salario
//Laura,1500
//Luis,3000
//Maria,1000
//Pablo,5000

//Y devuelve:
//nombre,salario
//Pablo,5000
//Luis,3000
//Laura,1500
//Maria,1000