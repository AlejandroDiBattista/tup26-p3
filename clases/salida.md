La versión más simple de entender y escribir en C# es utilizando **LINQ**. Aunque no es la más eficiente en memoria, es la más clara visualmente.

### Opción 1: La más simple (Usando LINQ)
Esta versión es casi como leer la definición matemática del algoritmo.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    public static void Main()
    {
        List<int> numeros = new List<int> { 5, 3, 8, 1, 2, 7 };
        var ordenados = QuickSort(numeros);
        Console.WriteLine(string.Join(", ", ordenados));
    }

    public static IEnumerable<int> QuickSort(IEnumerable<int> lista)
    {
        if (!lista.Any()) return lista;

        int pivot = lista.First();
        var menores = lista.Skip(1).Where(x => x <= pivot);
        var mayores = lista.Skip(1).Where(x => x > pivot);

        return QuickSort(menores).Concat(new[] { pivot }).Concat(QuickSort(mayores));
    }
}
```

---

### Opción 2: La versión clásica (In-place)
Si buscas la versión tradicional que se enseña en la universidad (que modifica el array original y es más rápida), aquí tienes la forma más compacta:

```csharp
public static void QuickSort(int[] arr, int primero, int ultimo)
{
    int i = primero, j = ultimo;
    int pivote = arr[(primero + ultimo) / 2];

    while (i <= j)
    {
        while (arr[i] < pivote) i++;
        while (arr[j] > pivote) j--;

        if (i <= j)
        {
            // Intercambio (Swap) usando tuplas (C# 7.0+)
            (arr[i], arr[j]) = (arr[j], arr[i]);
            i++; j--;
        }
    }

    if (primero < j) QuickSort(arr, primero, j);
    if (i < ultimo) QuickSort(arr, i, ultimo);
}
```

### Diferencias clave:
1.  **Versión LINQ:** Muy fácil de leer. Crea nuevas listas en cada paso (usa más memoria). Ideal para entender el concepto: *"Tomo un pivote, pongo los menores a la izquierda, los mayores a la derecha y repito"*.
2.  **Versión Clásica:** Es mucho más rápida y eficiente porque mueve los números dentro del mismo array original sin crear copias.

**¿Cuál elegir?**
*   Si es para un **examen o entrevista técnica**, usa la **Opción 2**.
*   Si es para **entender el concepto** rápidamente, usa la **Opción 1**.