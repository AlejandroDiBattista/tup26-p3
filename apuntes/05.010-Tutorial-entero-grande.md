# Tutorial: cómo implementar cálculos con números grandes

En la práctica, .NET ya incluye `System.Numerics.BigInteger` para trabajar con enteros de tamaño arbitrario. Sin embargo, implementar una versión propia es un muy buen ejercicio para entender cómo se representan los números y cómo funcionan las operaciones aritméticas básicas.

## Idea general

Los tipos enteros del lenguaje tienen un tamaño limitado. Por ejemplo, en C# el tipo `int` ocupa 32 bits con signo, por lo que solo puede representar valores entre `-2_147_483_648` y `2_147_483_647`.

Si una cuenta supera ese rango, ocurre un desbordamiento (*overflow*) y el resultado deja de ser correcto.

Para representar enteros más grandes, podemos crear nuestro propio tipo de dato. La idea más simple es separar el número en dos partes:

- **el signo**: positivo o negativo;
- **la magnitud**: una colección de dígitos o bloques que guardan el valor.

En este tutorial vamos a usar una representación sencilla: **un dígito decimal por posición**. No es la opción más eficiente, pero sí la más fácil de entender.

## Representación del número

Vamos a suponer, para simplificar, que cada número tiene hasta 100 dígitos.

```cs
const int MAX_DIGITS = 100;

record EnteroGrande(bool EsNegativo, int[] Digitos);
```

El arreglo `Digitos` va a guardar los dígitos en orden **little-endian**, es decir:

- `Digitos[0]` contiene el dígito menos significativo;
- `Digitos[1]` contiene el siguiente;
- y así sucesivamente.

Por ejemplo, el número `12345` se representaría así:

```text
[5, 4, 3, 2, 1, 0, 0, 0, ...]
```

Esta forma de guardar el número hace que sumar, restar y multiplicar sea más simple, porque empezamos directamente por el dígito de menor peso.

> Para mantener el tutorial simple, vamos a asumir que los resultados “entran” en el espacio disponible, salvo en la multiplicación, donde reservaremos un arreglo más grande.

## Comparar dos magnitudes

Antes de restar o dividir, necesitamos poder comparar dos números.

Como los dígitos están guardados de menor a mayor importancia, para comparar hay que recorrer el arreglo **desde el final hacia el principio**.

```cs
int CompararMagnitudes(int[] a, int[] b) {
    for (int i = MAX_DIGITS - 1; i >= 0; i--) {
        if (a[i] > b[i]) return 1;
        if (a[i] < b[i]) return -1;
    }

    return 0;
}
```

El resultado sigue la convención habitual:

- `1` si `a` es mayor que `b`;
- `-1` si `a` es menor que `b`;
- `0` si ambos son iguales.

## Cómo sumar dos números grandes

La suma se implementa igual que en la escuela: se suman los dígitos de derecha a izquierda y se arrastra el acarreo.

```text
    345 + 678 = 1023

      3 4 5
    + 6 7 8
    -------
    1 0 2 3
```

Paso a paso:

- `5 + 8 = 13`: escribimos `3` y llevamos `1`.
- `4 + 7 + 1 = 12`: escribimos `2` y llevamos `1`.
- `3 + 6 + 1 = 10`: escribimos `0` y llevamos `1`.
- El acarreo final `1` queda como nuevo dígito más significativo.

```cs
int[] SumarMagnitudes(int[] a, int[] b) {
    int[] resultado = new int[MAX_DIGITS];
    int acarreo = 0;

    for (int i = 0; i < MAX_DIGITS; i++) {
        int suma = a[i] + b[i] + acarreo;
        resultado[i] = suma % 10;
        acarreo = suma / 10;
    }

    return resultado;
}
```

## Cómo restar dos números grandes

La resta sigue la misma idea, pero en lugar de llevar un acarreo hay que **pedir prestado** cuando una resta parcial da negativa.

Esta versión asume que `a >= b`. Si no se cumple, primero hay que comparar ambos números y decidir el signo del resultado.

```text
  512 - 345 = 167

  5 1 2
- 3 4 5
-------
  1 6 7
```

Paso a paso:

- `2 - 5 = -3`: escribimos `7` y pedimos prestado `1`.
- `1 - 4 - 1 = -4`: escribimos `6` y pedimos prestado `1`.
- `5 - 3 - 1 = 1`: escribimos `1` y terminamos.

```cs
int[] RestarMagnitudes(int[] a, int[] b) {
    int[] resultado = new int[MAX_DIGITS];
    int prestamo = 0;

    for (int i = 0; i < MAX_DIGITS; i++) {
        int resta = a[i] - b[i] - prestamo;

        if (resta < 0) {
            resultado[i] = resta + 10;
            prestamo = 1;
        } else {
            resultado[i] = resta;
            prestamo = 0;
        }
    }

    return resultado;
}
```

## Cómo multiplicar dos números grandes

La multiplicación también se hace igual que en la escuela: multiplicamos cada dígito del multiplicador por cada dígito del multiplicando y acumulamos los resultados parciales en la posición correspondiente.

```text
   123
 x 456
 -----
   738   (123 × 6)
  615    (123 × 5, corrido una posición)
 492     (123 × 4, corrido dos posiciones)
 -----
 56088
```

En este caso, si multiplicamos dos números de 100 dígitos, el resultado puede tener hasta 200 dígitos. Por eso vamos a reservar un arreglo más grande.

```cs
int[] MultiplicarMagnitudes(int[] a, int[] b) {
    int[] resultado = new int[MAX_DIGITS * 2];

    for (int i = 0; i < MAX_DIGITS; i++) {
        int acarreo = 0;

        for (int j = 0; j < MAX_DIGITS; j++) {
            int posicion = i + j;
            int producto = a[j] * b[i] + resultado[posicion] + acarreo;

            resultado[posicion] = producto % 10;
            acarreo = producto / 10;
        }

        resultado[i + MAX_DIGITS] += acarreo;
    }

    return resultado;
}
```

## Cómo dividir dos números grandes

La división es la operación más delicada. La idea es replicar la división larga:

1. tomamos los dígitos del dividendo desde el más significativo hacia el menos significativo;
2. vamos construyendo un resto parcial;
3. calculamos cuántas veces entra el divisor en ese resto;
4. guardamos ese dígito en el cociente;
5. seguimos con el siguiente dígito.

```text
    26789 ÷ 123 = 217, resto 98

    123 × 217 + 98 = 26789

       2 | 123  -> 0   (resto 2)
      26 | 123  -> 0   (resto 26)
     267 | 123  -> 2   (resto 21)
     218 | 123  -> 1   (resto 95)
     959 | 123  -> 7   (resto 98)
```

Primero necesitamos un par de funciones auxiliares:

```cs
bool EsCero(int[] numero) {
    for (int i = 0; i < numero.Length; i++) {
        if (numero[i] != 0) {
            return false;
        }
    }

    return true;
}

void DesplazarUnaPosicion(int[] numero) {
    for (int i = MAX_DIGITS - 1; i > 0; i--) {
        numero[i] = numero[i - 1];
    }

    numero[0] = 0;
}
```

Y ahora sí, una versión simple de la división:

```cs
int[] DividirMagnitudes(int[] dividendo, int[] divisor) {
    if (EsCero(divisor)) {
        throw new DivideByZeroException();
    }

    int[] cociente = new int[MAX_DIGITS];
    int[] resto = new int[MAX_DIGITS];

    for (int i = MAX_DIGITS - 1; i >= 0; i--) {
        DesplazarUnaPosicion(resto);
        resto[0] = dividendo[i];

        int digitoCociente = 0;

        while (CompararMagnitudes(resto, divisor) >= 0) {
            resto = RestarMagnitudes(resto, divisor);
            digitoCociente++;
        }

        cociente[i] = digitoCociente;
    }

    return cociente;
}
```

> Esta implementación es correcta como modelo didáctico, pero no es eficiente: cada paso de la división usa restas repetidas. Para una versión más rápida conviene estimar cada dígito del cociente en lugar de restar una y otra vez.

## Cómo manejar números negativos

Una vez que sabemos operar con magnitudes positivas, agregar el signo es bastante más simple.

La idea es esta:

- si ambos números tienen el mismo signo, se suman las magnitudes y se conserva el signo;
- si tienen signos distintos, se restan las magnitudes y el resultado toma el signo del número de mayor magnitud.

```cs
EnteroGrande NormalizarCero(EnteroGrande numero) {
    return EsCero(numero.Digitos)
        ? new EnteroGrande(false, numero.Digitos)
        : numero;
}

EnteroGrande SumarConSigno(EnteroGrande a, EnteroGrande b) {
    if (a.EsNegativo == b.EsNegativo) {
        return NormalizarCero(
            new EnteroGrande(a.EsNegativo, SumarMagnitudes(a.Digitos, b.Digitos))
        );
    }

    int comparacion = CompararMagnitudes(a.Digitos, b.Digitos);

    if (comparacion == 0) {
        return new EnteroGrande(false, new int[MAX_DIGITS]);
    }

    if (comparacion > 0) {
        return NormalizarCero(
            new EnteroGrande(a.EsNegativo, RestarMagnitudes(a.Digitos, b.Digitos))
        );
    }

    return NormalizarCero(
        new EnteroGrande(b.EsNegativo, RestarMagnitudes(b.Digitos, a.Digitos))
    );
}

EnteroGrande CambiarSigno(EnteroGrande numero) {
    return EsCero(numero.Digitos)
        ? numero
        : new EnteroGrande(!numero.EsNegativo, numero.Digitos);
}

EnteroGrande RestarConSigno(EnteroGrande a, EnteroGrande b) {
    return SumarConSigno(a, CambiarSigno(b));
}
```

Para producto y división, la regla del signo es más simple:

- **igual signo** → resultado positivo;
- **distinto signo** → resultado negativo.

En otras palabras:

```cs
bool SignoResultado(bool signoA, bool signoB) {
    return signoA != signoB;
}
```

## Generalización: usar una base mayor que 10

En este tutorial usamos base 10 porque es la forma más intuitiva de ver qué está pasando. Pero no es la forma más eficiente.

Una mejora muy común es guardar cada “dígito” en una base más grande, por ejemplo base `1_000_000`. En ese caso, cada posición del arreglo guardaría un valor entre `0` y `999_999`.

Eso permite:

- usar menos posiciones para representar el mismo número;
- hacer menos iteraciones al sumar, restar o multiplicar;
- mejorar bastante el rendimiento.

## Resumen

Para implementar enteros grandes desde cero, alcanza con estas ideas:

1. representar la magnitud como una secuencia de dígitos;
2. guardar los dígitos del menos significativo al más significativo;
3. implementar suma, resta, multiplicación y división sobre la magnitud;
4. agregar el signo por separado.

La implementación de este tutorial está pensada para aprender. Una versión de producción necesitaría varias mejoras, por ejemplo:

- guardar solo los dígitos significativos;
- usar una base mayor que 10;
- optimizar la división;
- agregar conversión desde y hacia `string`.

Pero como punto de partida, este modelo ya muestra la idea esencial: **un número grande no es más que una estructura de datos sobre la que implementamos las reglas de la aritmética**.