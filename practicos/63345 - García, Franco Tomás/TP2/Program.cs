using System;

class Program
{
    static void Main(string[] argumentos)
    {
        if (argumentos.Length > 0)
        {
            if (argumentos.Length > 0 && (argumentos[0] == "--help" || argumentos[0] == "-h"))
            {
                Console.WriteLine("Operaciones disponibles:");
                Console.WriteLine("Suma (+)");
                Console.WriteLine("Resta (-)");
                Console.WriteLine("Multiplicación (*)");
                Console.WriteLine("División entera (/)");
                Console.WriteLine("Módulo (%)");
                Console.WriteLine("Paréntesis ()");
                Console.WriteLine();
                Console.WriteLine("Modo interactivo:");
                Console.WriteLine("Ingresar las expresiones de forma manual");
                Console.WriteLine("Escriba \"salir\" para terminar");
                return;
            }

            try
            {
                Console.WriteLine(Calculadora.Resolver(argumentos[0]));
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("Error: división por cero");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        else
        {
            while (true)
            {
                Console.Write("> ");
                string? entradaUsuario = Console.ReadLine();

                if (entradaUsuario == null) continue;

                entradaUsuario = entradaUsuario.Trim().ToLower();

                if (entradaUsuario == "salir" || entradaUsuario == "fin")
                    break;

                try
                {
                    Console.WriteLine(Calculadora.Resolver(entradaUsuario));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }
    }
}
