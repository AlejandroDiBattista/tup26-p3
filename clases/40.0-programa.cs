using System;

class Program
{
    static void QuickSort(int[] a, int izq, int der)
    {
        int i = izq, j = der;
        int pivote = a[(izq + der) / 2];

        while (i <= j)
        {
            while (a[i] < pivote) i++;
            while (a[j] > pivote) j--;

            if (i <= j)
            {
                int temp = a[i];
                a[i] = a[j];
                a[j] = temp;
                i++;
                j--;
            }
        }

        if (izq < j) QuickSort(a, izq, j);
        if (i < der) QuickSort(a, i, der);
    }

    static void Main()
    {
        int[] numeros = { 5, 3, 8, 4, 2, 7, 1, 6 };

        QuickSort(numeros, 0, numeros.Length - 1);

        Console.WriteLine(string.Join(", ", numeros));
    }
}