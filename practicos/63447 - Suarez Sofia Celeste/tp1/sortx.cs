using System;
using System.Collections.Generic;
using System.IO;
//primero creamos el try/catch, que es lo que va a order todo lo que el programa va a hacer, y si ocurre un error lo guarda en la variable error y lo muestra. 
try
{
    //creamos la variable config que va a guardar los pedidos del usuario, se necesita para que luego las funciones saquen de este "paquete de instrucciones" lo que necesitan, y que luego decimos que si el usuario se equivoca en los comandos el programa se pare
    var config = CargarConfiguracion(args);
    if (config == null) return;

    //definimos la informacion sin procesar del archivo para que el programa pueda trabajar como string ya que en este case se espera que el contenido del archivo empleados.csv sea una cadena de texto grande.
    string textoBruto = LeerEntrada(config);

    //separamos la informaciond el archivo en la cabecera, que siempre va a ser la misma, de las filas, que son lo que si se va a ordenar.
    var (cabeceras, filas) = ProcesarTexto(textoBruto, config);

    //el objetivo principal del programa, toma las filas y las acomoda segun lo que el usuario pidio, osea lo que esta guardado en la variable confing, utiliza las cabeceras para saber que columna debe ordenar.
    OrdenarFilas(filas, config, cabeceras);

    //es el archivo ya ordenado se guarda en la variable resultado y se lo entrega a la funcion EscribirSalida para que lo muestro o lo guarde en otro archivo.
    string resultado = ConvertirATexto(cabeceras, filas, config);
    EscribirSalida(resultado, config);
}
catch (Exception error)
{
    Console.WriteLine($"error: {error.Message}");
}

//appconfig? puede devolver una configuracion valida o un null, se usa porque si el usuario pide ayuda o hay un error la función no tiene datos que enviar y puede devuelver un null.
//se leen los pedidos del usuario.
AppConfig? CargarConfiguracion(string[] argumentos)
{
    string entrada = "", salida = "", separador = ",";
    bool sinCabecera = false;

    // sortfield son los pedidos y list<> es donde se guardan, cada vez que el usuario pone -b se crea un nuevo sortfield con el nombre de la columna y si es num o desc y se guarda en esta lista.
    var listaCampos = new List<SortField>();

    for (int i = 0; i < argumentos.Length; i++)
    {
        if (argumentos[i] == "-i") entrada = argumentos[++i];
        else if (argumentos[i] == "-o") salida = argumentos[++i];
        else if (argumentos[i] == "-d") separador = argumentos[++i];
        else if (argumentos[i] == "-nh") sinCabecera = true; 
        else if (argumentos[i] == "-b")
        {
            string[] partes = argumentos[++i].Split(':');
            string nombreCol = partes[0];
            bool esNumero = false;
            bool esDesc = false;

            // se revisa si el usuario puso :num:desc, :desc:num, un :num solo o un:desc solo.
            for (int j = 1; j < partes.Length; j++)
            {
                if (partes[j] == "num") esNumero = true;
                if (partes[j] == "desc") esDesc = true;
            }

            // Agregamos este "pedido" a nuestra lista de campos
            listaCampos.Add(new SortField(nombreCol, esNumero, esDesc));
        }
    }

    //si el programa ve que listacampos esta vacio, osea que el usuario no pusolos -b, entonces salta un error.
    if (listaCampos.Count == 0) throw new Exception("Falta la columna para ordenar (-b)");

    //toda la informacion se guarda aqui.
    return new AppConfig(entrada, salida, separador, sinCabecera, listaCampos);
}

//entrada de datos, por donde se reciben los datos, por un archivo o por la terminal?, se inicializa como string porque que espera que esta funcion nos de una cadena de texto grande, (Configuracion config) es donde la funcion se fija si el usuario especifico un archivo de entrada o no
string LeerEntrada(AppConfig config)
{

    //si el comando de archivo no esta vacio, osea si el usuario escribio -i.
    //entonces el programa busca el archivo en la computadora, lo lee y lo devuelve para que se guarde en la variable textoBruto.
    if (!string.IsNullOrEmpty(config.InputFile)) 
        return File.ReadAllText(config.InputFile);
    
    //si el usuario no escribio -i el programa espera que el usuario envie la informacion por la terminal, lo que el usuario envie se guarda en la variable textoBruto.
    return Console.In.ReadToEnd();
}

//en esta parte se toma el texto y los pedidos del usuario, aqui se necesita principalmente los separadores para poder dividir el texto en partes, y las cabeceras para saber que columna es cada una. 
(string[]?, List<string[]>) ProcesarTexto(string texto, AppConfig config)
{
    // Cambiamos el Split para que limpie bien los archivos, se usa \r\n, \r y \n para que el programa funcione igual de bien en Windows, Mac o Linux, y el stringsplitoptions.removeemptyentries es para que si hay lineas vacias no les de importancia y no de error.
    //trabaja con el texto.
    string[] lineas = texto.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    //trabaja con las cabeceras, el .Split(config.Delimiter) separa las cabeceras con el separador que el usuario puso para que luego el programa sepa en que columna trabajas.
    string[] cabeceras = lineas[0].Split(config.Delimiter);
    
    var listaFilas = new List<string[]>();
    for (int i = 1; i < lineas.Length; i++)
    {
        listaFilas.Add(lineas[i].Split(config.Delimiter));
    }
    return (cabeceras, listaFilas);
}

//aqui se ordenan los datos, filas es la lista de empleados y son la funcion sort que se encarga de ordenar, el sort recibe una función que le dice cómo comparar dos filas, y esa función revisa cada campo que el usuario pidió para ordenar, encuentra el índice de ese campo en las cabeceras, compara los valores de ese campo en las dos filas, y si encuentra una diferencia devuelve cuál va primero, si son iguales sigue al siguiente campo hasta que encuentre una diferencia o se acaben los campos.
void OrdenarFilas(List<string[]> filas, AppConfig config, string[]? cabeceras)
{
    // El Sort ahora revisa toda la lista de campos que guardamos
    filas.Sort((filaA, filaB) => {
        
        //el foreach pasa al siguiente campo pedido por el usuario si es que hubo un "empate" en la comparacion del primer campo pedido.
        foreach (var campo in config.SortFields)
        {
            //se inicializa el indice,las columnas, en -1 para que si no se encuentra la columna que el usuario pidió, el programa sepa que no existe y salte un error.
            //el ciclo for recorre todas las cabeceras, las limpia con trim para problrmas por los espacion y compara con el nombre del campo que el usuario pidió, si encuentra una coincidencia guarda el indice de esa columna y sale del ciclo, si al final el indice sigue siendo -1, significa que no se encontró la columna y el programa salta un error.
            int indice = -1;
            for (int i = 0; i < cabeceras.Length; i++)
            {
                if (cabeceras[i].Trim() == campo.Name) { indice = i; break; }
            }

            if (indice == -1) throw new Exception("No existe la columna " + campo.Name);

            //una vez que tenemos el indice de la columna, sacamos los valores de esa columna en las dos filas que estamos comparando, se limpian los espacion con trim por las dudas.
            string valA = filaA[indice].Trim();
            string valB = filaB[indice].Trim();

            // Si el campo es numérico, convertimos a números y comparamos, porque si se guarda como texto puede haber problemas al comparar numeros, si no, comparamos como texto. El resultado se guarda en comp, comp las compara y devuelve -1 si valA va antes que valB, 1 si valB va antes que valA, o 0 si son iguales y el ciclo pasa al -b que el usuario pidio.
            int comp;
            if (campo.Numeric)
            {
                double nA = double.Parse(valA);
                double nB = double.Parse(valB);
                comp = nA.CompareTo(nB);
            }
            else
            {
                comp = string.Compare(valA, valB);
            }

            if (comp != 0) 
            //si el usuario pidió que ese campo sea descendente, entonces invertimos el resultado de la comparación para que se ordene al revés, si no, dejamos el resultado como está para que se ordene de forma ascendente.
                return campo.Descending ? -comp : comp;
            
        }
        return 0;
    });
}

//una vez ordenados los datos, se deben juntar otra vez, con el .join se unen las cabeceras con el separador que el usuario pidió, y luego se hace lo mismo con cada fila, y se van agregando a la variable final, al final se devuelve esta variable que es el texto ya ordenado com environment.newline para que los datos esten ordenados por filas y no aparezcan todos juntos, y ya esta listo para mostrar o guardar.
string ConvertirATexto(string[]? cabeceras, List<string[]> filas, AppConfig config)
{
    string final = string.Join(config.Delimiter, cabeceras) + Environment.NewLine;
    foreach (var f in filas)
    {
        final += string.Join(config.Delimiter, f) + Environment.NewLine;
    }
    return final;
}

//aqui se muestran los datos ya ordenados,texto, dependiendo de como el usuario pidio que se los devolviera el progrema, si el usuario pudo un archivo de salida, los datos ordenados se muestran alli, sino se muestran en consola.
void EscribirSalida(string texto, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile)) 
        File.WriteAllText(config.OutputFile, texto);
    else 
        Console.WriteLine(texto);
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields
);
