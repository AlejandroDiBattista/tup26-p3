namespace TP2.Calculadora;

public class Programa
{
    public static void Main(string[] args)
    {
        Console.Title = "Calculadora de Expresiones - TP2";
        Console.WriteLine("--- Intérprete de Expresiones Aritméticas ---");

        if (args.Length == 0)
        {
            Console.WriteLine("Iniciando modo interactivo... (Escribe 'fin' para salir)");
        }
        else
        {
            // Aquí procesaremos los flags --help, --test o el modo directo
            Console.WriteLine($"Procesando {args.Length} argumentos de entrada...");
        }
    }
}