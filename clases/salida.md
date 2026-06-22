## Vos

factorial

## Asistente

El **factorial** de un número entero no negativo `n` se define como:

```text
n! = n × (n - 1) × (n - 2) × ... × 1
```

Casos especiales:

```text
0! = 1
1! = 1
```

Ejemplos:

```text
5! = 5 × 4 × 3 × 2 × 1 = 120
```

### C# - factorial iterativo

```csharp
using System;

class Program
{
    static long Factorial(int n)
    {
        if (n < 0)
            throw new ArgumentException("El factorial no está definido para números negativos.");

        long resultado = 1;

        for (int i = 2; i <= n; i++)
        {
            resultado *= i;
        }

        return resultado;
    }

    static void Main()
    {
        Console.WriteLine(Factorial(5)); // 120
    }
}
```

### C# - factorial recursivo

```csharp
static long Factorial(int n)
{
    if (n < 0)
        throw new ArgumentException("Número inválido");

    if (n == 0 || n == 1)
        return 1;

    return n * Factorial(n - 1);
}
```

Para valores grandes conviene usar `BigInteger` en vez de `long`.

## Vos

quicksort

## Asistente

**Quicksort** es un algoritmo de ordenamiento basado en *divide y vencerás*:

1. Elegir un **pivote**.
2. Particionar el arreglo:
   - elementos menores al pivote a la izquierda
   - elementos mayores al pivote a la derecha
3. Aplicar Quicksort recursivamente a cada lado.

Complejidad promedio:

```text
O(n log n)
```

Peor caso:

```text
O(n²)
```

---

## Implementación en C#

```csharp
using System;

class Program
{
    static void QuickSort(int[] arr, int izquierda, int derecha)
    {
        if (izquierda >= derecha)
            return;

        int indicePivote = Particionar(arr, izquierda, derecha);

        QuickSort(arr, izquierda, indicePivote - 1);
        QuickSort(arr, indicePivote + 1, derecha);
    }

    static int Particionar(int[] arr, int izquierda, int derecha)
    {
        int pivote = arr[derecha];
        int i = izquierda - 1;

        for (int j = izquierda; j < derecha; j++)
        {
            if (arr[j] <= pivote)
            {
                i++;
                Intercambiar(arr, i, j);
            }
        }

        Intercambiar(arr, i + 1, derecha);
        return i + 1;
    }

    static void Intercambiar(int[] arr, int a, int b)
    {
        int temp = arr[a];
        arr[a] = arr[b];
        arr[b] = temp;
    }

    static void Main()
    {
        int[] numeros = { 8, 3, 1, 7, 0, 10, 2 };

        QuickSort(numeros, 0, numeros.Length - 1);

        Console.WriteLine(string.Join(", ", numeros));
    }
}
```

Salida:

```text
0, 1, 2, 3, 7, 8, 10
```

---

## Versión genérica en C#

```csharp
using System;
using System.Collections.Generic;

public static class QuickSorter
{
    public static void QuickSort<T>(T[] arr, IComparer<T>? comparer = null)
    {
        comparer ??= Comparer<T>.Default;
        QuickSort(arr, 0, arr.Length - 1, comparer);
    }

    private static void QuickSort<T>(
        T[] arr,
        int izquierda,
        int derecha,
        IComparer<T> comparer)
    {
        if (izquierda >= derecha)
            return;

        int p = Particionar(arr, izquierda, derecha, comparer);

        QuickSort(arr, izquierda, p - 1, comparer);
        QuickSort(arr, p + 1, derecha, comparer);
    }

    private static int Particionar<T>(
        T[] arr,
        int izquierda,
        int derecha,
        IComparer<T> comparer)
    {
        T pivote = arr[derecha];
        int i = izquierda - 1;

        for (int j = izquierda; j < derecha; j++)
        {
            if (comparer.Compare(arr[j], pivote) <= 0)
            {
                i++;
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        (arr[i + 1], arr[derecha]) = (arr[derecha], arr[i + 1]);
        return i + 1;
    }
}
```

Uso:

```csharp
int[] datos = { 5, 9, 1, 3, 7 };

QuickSorter.QuickSort(datos);

Console.WriteLine(string.Join(", ", datos));
```

Resultado:

```text
1, 3, 5, 7, 9
```

---

Notas técnicas:

- No es estable por defecto.
- Ordena *in-place*.
- El peor caso ocurre si el pivote divide muy mal el arreglo, por ejemplo con datos ya ordenados usando siempre el último elemento como pivote.

