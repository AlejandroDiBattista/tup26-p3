using System;

public class Programa
{
    public static void Main(string[] args)
    {
        Comandos.Ejecutar(args);
    }

    public static void ModoInteractivo()
    {
        Console.Write("Ingrese una expresión matemática: ");
        string expresion = Console.ReadLine();

        try
        {
            Compilador comp = new Compilador(expresion);
            Nodo ast = comp.Parsear();

            while (true)
            {
                Console.Write("Ingrese valor para x (o 'fin' para salir): ");
                string entradaX = Console.ReadLine();

                if (string.IsNullOrEmpty(entradaX) || entradaX.ToLower() == "fin")
                {
                    break;
                }

                int x;
                if (int.TryParse(entradaX, out x))
                {
                    try
                    {
                        int resultado = ast.Evaluar(x);
                        Console.WriteLine("Resultado: " + resultado);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Error: Valor de x inválido.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}