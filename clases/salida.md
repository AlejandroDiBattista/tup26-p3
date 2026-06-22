## Vos

factorial

## Asistente

¿Querés el factorial de un número en particular, o una implementación en C#?

Por ejemplo, en C#:

```csharp
static long Factorial(int n)
{
    if (n < 0) throw new ArgumentException("n debe ser >= 0");
    long resultado = 1;

    for (int i = 2; i <= n; i++)
        resultado *= i;

    return resultado;
}
```

Si querés, te lo adapto para `BigInteger` o recursivo.

