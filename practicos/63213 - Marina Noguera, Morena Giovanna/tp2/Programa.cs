namespace TP2.Calculadora;

class Program
{
    static void Main(string[] args)
    {
        if (args[0].ToLower() == "test")
        {
            Pruebas.Ejecutar();
            return;
        }
        
        // Si el usuario no pasó argumentos
        if (args.Length == 0)
        {
            EjecutarModoInteractivo();
        }
        
        // Si el usuario pasó la expresión y el valor de x
        else if (args.Length == 2)
        {
            EjecutarModoDirecto(args[0], args[1]);
        }
        else
        {
            Console.WriteLine("Uso incorrecto.");
            Console.WriteLine("Modo interactivo: dotnet run");
            Console.WriteLine("Modo directo: dotnet run -- \"expresion\" valorX");
        }
    }

    static void EjecutarModoDirecto(string expresion, string valorX)
    {
        try
        {
            var nodo = Compilador.Parse(expresion);
            if (int.TryParse(valorX, out int x))
            {
                Console.WriteLine(nodo.Evaluar(x));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void EjecutarModoInteractivo()
    {
        Console.WriteLine("--- Calculadora Interactiva ---");
        Console.Write("Ingrese la expresión (ej: x + 5 * 2): ");
        string? entrada = Console.ReadLine();

        if (string.IsNullOrEmpty(entrada)) return;

        try
        {
            var arbol = Compilador.Parse(entrada);
            Console.WriteLine("Expresión aceptada. Escriba valores para 'x' o 'salir' para terminar.");

            while (true)
            {
                Console.Write("x = ");
                string? inputX = Console.ReadLine();

                if (inputX?.ToLower() == "salir") break;

                if (int.TryParse(inputX, out int x))
                {
                    Console.WriteLine($"Resultado: {arbol.Evaluar(x)}");
                }
                else
                {
                    Console.WriteLine("Por favor, ingrese un número entero.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en la expresión: {ex.Message}");
        }
    }
}