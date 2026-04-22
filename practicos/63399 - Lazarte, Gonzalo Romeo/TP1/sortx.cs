/*programa hecho por mí(Gonza)*/
using System;
using System.Collections.Generic;
/*aqui el try intenta ejecutar la funcion*/
try
{
    var config = ParseArgs(args);
    var texto = ReadInput(config);
    var filas = ParseDelimited(texto, config);
    var ordenadas = SortRows(filas, config);
    var salida = Serialize(ordenadas, config);
    /*Console.WriteLine("OUTPUT = " + config.OutputFile); esto era una ayuda para controlar que todo iba bien*/
    WriteOutput(salida, config);
   
}
/*el catch es lo que hará si falla el try (muestra mensaje del error)*/
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}

/*función que recibe los argumentos y analiza qué hay en cada arg*/
AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;

    var sortFields = new List<SortField>();

    /*array que compara cada valor*/
    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        /*verifica el argumento y si coincide con el de help muestra la data y termina el programa*/
        if (arg == "--help" || arg == "-h")
        {
            Console.WriteLine("Uso: sortx [input [output]] [-b campo[:tipo[:orden]]]...");
            Console.WriteLine("Opciones:");
            Console.WriteLine("-b, --by        Campo por el que ordenar");
            Console.WriteLine("-i, --input     Archivo de entrada");
            Console.WriteLine("-o, --output    Archivo de salida");
            Console.WriteLine("-d, --delimiter Delimitador");
            Console.WriteLine("-nh, --no-header Sin encabezado");
            Console.WriteLine("-h, --help      Mostrar ayuda");

            Environment.Exit(0);
        }

        if (arg == "-b" || arg == "--by")
        {
            var valor = args[i + 1];
            /*asigna a variables los valores que va obteniendo*/
            var partes = valor.Split(':');

            string nombre = partes[0];
            string tipo = partes.Length > 1 ? partes[1] : "alpha";
            string orden = partes.Length > 2 ? partes[2] : "asc";
            
            /*asigna T o F según la coincidencia*/
            bool numeric = tipo == "num";
            bool descending = orden == "desc";

            /*añade a la lista todos los nuevos valores obtenidos*/
            sortFields.Add(new SortField(nombre, numeric, descending));

            i++;
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            noHeader = true;
        }
        else if (!arg.StartsWith("-"))
        {
            if (arg.EndsWith(".cs"))
                continue;

            if (input == null)
            {
                input = arg;
            }
            else if (output == null)
            {
                output = arg;
            }
        }      
    }

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

/*funcion que lee los inputs ya sea de la consola o un archivo*/
string ReadInput(AppConfig config)
{
    if (config.InputFile != null)
    {
        /*Console.WriteLine("Leyendo desde archivo: " + config.InputFile); esto era una ayuda para controlar que todo iba bien*/
        return File.ReadAllText(config.InputFile);
    }
    else
    {
        /*Console.WriteLine("Leyendo desde stdin..."); esto era una ayuda para controlar que todo iba bien*/
        return Console.In.ReadToEnd();
    }
}

/*funcion que se encarga de ir acomodando en filas la info*/
List<Dictionary<string, string>> ParseDelimited(string texto, AppConfig config)
{
    var lineas = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    var headers = lineas[0].Split(config.Delimiter);

    var filas = new List<Dictionary<string, string>>();

    for (int i = 1; i < lineas.Length; i++)
    {
        var valores = lineas[i].Split(config.Delimiter);

        var fila = new Dictionary<string, string>();

        for (int j = 0; j < headers.Length; j++)
        {
            fila[headers[j]] = valores[j];
        }

        filas.Add(fila);
    }

    return filas;
}
/*funcion encargada de ordenar según criterios a cada campo y ordenarlos*/
List<Dictionary<string, string>> SortRows(
    List<Dictionary<string, string>> filas,
    AppConfig config)
{
    foreach (var campo in config.SortFields)
    {
        if (!filas[0].ContainsKey(campo.Name))
        {
            throw new Exception("Columna no existe: " + campo.Name);
        }
    }
    filas.Sort((a, b) =>
    {
        foreach (var campo in config.SortFields)
        {
            string valorA = a[campo.Name];
            string valorB = b[campo.Name];

            int resultado;

            if (campo.Numeric)
            {
                double numA = double.Parse(valorA);
                double numB = double.Parse(valorB);

                resultado = numA.CompareTo(numB);
            }
            else
            {
                resultado = string.Compare(valorA, valorB);
            }

            if (campo.Descending)
            {
                resultado *= -1;
            }

            if (resultado != 0)
            {
                return resultado;
            }
        }

        return 0;
    });

    return filas;
}

/*esta funcion es como la inversa de la otra, transforma todo a csv de nuevo*/
string Serialize(List<Dictionary<string, string>> filas, AppConfig config)
{
    var lineas = new List<string>();

    var headers = filas[0].Keys.ToList();

    if (!config.NoHeader)
    {
        lineas.Add(string.Join(config.Delimiter, headers));
    }

    foreach (var fila in filas)
    {
        var valores = new List<string>();

        foreach (var h in headers)
        {
            valores.Add(fila[h]);
        }

        lineas.Add(string.Join(config.Delimiter, valores));
    }

    return string.Join("\n", lineas);
}

/*funcion encargada de dejar el archivo nuevo de salida con todo ordenadito*/
void WriteOutput(string texto, AppConfig config)
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

/*defino ambos records con las variables que utilizaré*/
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);