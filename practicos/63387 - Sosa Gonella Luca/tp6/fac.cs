using System;

class Program
{
    static void Main()
    {
        Console.Write("Ingrese un número entero no negativo: ");
        string? entrada = Console.ReadLine();

        if (!int.TryParse(entrada, out int numero) || numero < 0)
        {
            Console.WriteLine("Debe ingresar un número entero no negativo.");
            return;
        }

        long factorial = 1;

        for (int i = 2; i <= numero; i++)
        {
            factorial *= i;
        }

        Console.WriteLine($"El factorial de {numero} es {factorial}");
    }
}
