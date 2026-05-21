#!/usr/bin/env -S dotnet run
#:package System.CommandLine@2.0.0-beta4.22272.1
using System.CommandLine;

// La libreria System.CommandLine permite crear aplicaciones de consola con una estructura de comandos y argumentos,
// facilitando la creación de interfaces de línea de comandos complejas y bien organizadas. 

//  
// Description:
//   Ejemplo de aplicación de consola con System.CommandLine

// Usage:
//   23.4-consola-command-line [command] [options]
//
// Options:
//   --version       Show version information
//   -?, -h, --help  Show help and usage information
//
// Commands:
//   saludar <nombre>           Saluda a una persona
//   sumar <numero1> <numero2>  Suma dos números
/// 
/// 


// En este ejemplo se crea una aplicación de consola con dos comandos: "saludar" y "sumar".

// Definimos un comando raíz que actúa como punto de entrada, y luego agregamos los comandos específicos con sus argumentos y manejadores correspondientes.
var raiz = new RootCommand("Ejemplo de aplicación de consola con System.CommandLine");

// Creamos un comando "saludar" que toma un argumento "nombre" y muestra un saludo personalizado.
var saludar = new Command("saludar", "Saluda a una persona");

// Los argumentos son los valores que el usuario proporciona después del comando para personalizar su comportamiento. En este caso, "nombre" es un argumento de tipo string que representa el nombre de la persona a saludar.
var nombre  = new Argument<string>("nombre", "El nombre de la persona a saludar");
saludar.AddArgument(nombre);

// Las opciones son parámetros opcionales que modifican el comportamiento del comando. En este caso, "--formal" es una opción booleana que indica si se debe usar un saludo formal o informal.
var opcion = new Option<bool>("--formal", "Usar un saludo formal");
saludar.AddOption(opcion);

// El manejador es la función que se ejecuta cuando el comando es invocado. En este caso, se define un manejador para el comando "saludar" que toma el nombre y la opción de formalidad para mostrar un saludo adecuado.
saludar.SetHandler((string nombre, bool formal) => {
    if (formal)
        Console.WriteLine($"Buenos días, {nombre}");
    else
        Console.WriteLine($"Hola {nombre}!");
}, nombre, opcion);


// Creamos un comando "sumar" que toma dos argumentos "numero1" y "numero2" y muestra la suma de ambos.
var sumar = new Command("sumar", "Suma dos números");

// Con 2 argumentos de tipo int, el manejador se encargará de recibirlos como enteros y realizar la suma.
var num1  = new Argument<int>("numero1", "El primer número a sumar");
var num2  = new Argument<int>("numero2", "El segundo número a sumar");
sumar.AddArgument(num1);
sumar.AddArgument(num2);

// Y definimos el manejador para el comando "sumar" que toma los dos números, los suma y muestra el resultado.
sumar.SetHandler((int a, int b) => Console.WriteLine($"{a} + {b} = {a + b}") , num1, num2);

// Finalmente, agregamos ambos comandos al comando raíz para que estén disponibles cuando se ejecute la aplicación. El método InvokeAsync se encarga de procesar los argumentos proporcionados por el usuario y ejecutar el comando correspondiente.    
raiz.AddCommand(saludar);
raiz.AddCommand(sumar);

return await raiz.InvokeAsync(args);
