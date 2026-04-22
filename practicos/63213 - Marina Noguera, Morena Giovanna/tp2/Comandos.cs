namespace TP2.Calculadora;

public static class Comandos
{
    public static void EjecutarModoInteractivo()
    {
        Console.Write("Ingrese la expresión: ");
        string? entrada = Console.ReadLine();
        if (string.IsNullOrEmpty(entrada)) return;

        try {
            var arbol = Compilador.Parse(entrada);
            while (true) {
                Console.Write("Valor de x (o 'salir'): ");
                string? input = Console.ReadLine()?.ToLower().Trim();
                if (input == "salir") break;
                if (int.TryParse(input, out int x))
                    Console.WriteLine($"Resultado: {arbol.Evaluar(x)}");
                else
                    Console.WriteLine("Entrada no válida.");
            }
        } catch (Exception e) {
            Console.WriteLine($"Error: {e.Message}");
        }
    }
}