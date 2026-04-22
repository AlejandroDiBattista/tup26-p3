namespace TP2.Calculadora;

class Program
{
    static void Main(string[] args)
    {
        // 1. dotnet run -- test
        if (args.Length > 0 && args[0].ToLower() == "test")
        {
            Pruebas.Ejecutar(); 
        }
        // 2. dotnet run -- "expresion" valorX
        else if (args.Length == 2)
        {
            try
            {
                var arbol = Compilador.Parse(args[0]);
                Console.WriteLine(arbol.Evaluar(int.Parse(args[1])));
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        // 3. dotnet run (Modo interactivo)
        else
        {
            Comandos.EjecutarModoInteractivo();
        }
    }
}