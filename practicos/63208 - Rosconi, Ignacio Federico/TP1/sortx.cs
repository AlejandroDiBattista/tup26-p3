
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

Console.WriteLine($"sortx {string.Join(" ", args)}");

using System;

class SortX
{
    static void Main(string[] args)
    {
        Console.WriteLine("Ingrese la cantidad de números a ordenar:");
        int cantidad = int.Parse(Console.ReadLine());

        int[] numeros = new int[cantidad];

        for (int i = 0; i < cantidad; i++)
        {
            Console.WriteLine("Ingrese el número " + (i + 1) + ":");
            numeros[i] = int.Parse(Console.ReadLine());
        }

        // Ordenamiento burbuja
        for (int i = 0; i < cantidad - 1; i++)
        {
            for (int j = 0; j < cantidad - i - 1; j++)
            {
                if (numeros[j] > numeros[j + 1])
                {
                    int auxiliar = numeros[j];
                    numeros[j] = numeros[j + 1];
                    numeros[j + 1] = auxiliar;
                }
            }
        }

        Console.WriteLine("Números ordenados:");

        for (int i = 0; i < cantidad; i++)
        {
            Console.Write(numeros[i] + " ");
        }

        Console.WriteLine();
    }
}
