### Análisis estructural: Quicksort vs Burbuja

| Aspecto | Quicksort | Burbuja |
|---|---|---|
| Tipo | Divide y vencerás | Comparación e intercambio |
| Estrategia | Elige un pivote y particiona | Compara pares vecinos |
| Recursivo | Sí | No necesariamente |
| Complejidad promedio | `O(n log n)` | `O(n²)` |
| Peor caso | `O(n²)` | `O(n²)` |
| Mejor caso | `O(n log n)` | `O(n)` si está optimizado |
| Memoria extra | `O(log n)` por recursión | `O(1)` |
| Estable | No normalmente | Sí |
| Uso real | Muy usado | Casi solo educativo |

---

## Burbuja

Recorre el arreglo varias veces comparando elementos vecinos.

```csharp
if (arr[i] > arr[i + 1])
{
    swap;
}
```

### Estructura

```text
for pasadas
    for comparaciones
        si están desordenados
            intercambiar
```

### Características

- Simple de entender.
- Muy ineficiente para listas grandes.
- Los elementos grandes “suben” al final.
- Tiene muchos intercambios innecesarios.

---

## Quicksort

Divide el arreglo según un pivote.

```text
menores que pivote | pivote | mayores que pivote
```

### Estructura

```text
quicksort(inicio, fin)
    elegir pivote
    particionar
    quicksort(izquierda)
    quicksort(derecha)
```

### Características

- Mucho más eficiente en promedio.
- Usa recursividad.
- Su rendimiento depende del pivote.
- Puede degradarse a `O(n²)` si el pivote es malo.

---

## Comparación conceptual

Burbuja trabaja de forma **lineal repetitiva**, revisando muchas veces el arreglo completo.

Quicksort trabaja de forma **jerárquica**, dividiendo el problema en partes más pequeñas.

---

## Conclusión

- Para aprender: **Burbuja** es más simple.
- Para rendimiento: **Quicksort** es muy superior.
- Para arreglos grandes: usar **Quicksort**.
- Burbuja no se recomienda en producción salvo casos muy pequeños o educativos.