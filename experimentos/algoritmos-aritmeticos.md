# Algoritmos aritméticos

## Suposiciones

Para estos algoritmos se asume que:

- Los números no tienen signo.
- Cada número se representa con un arreglo de dígitos.
- `n[0]` es la unidad, `n[1]` la decena, `n[2]` la centena, etc.
- `Normalizar(n)` elimina los ceros sobrantes al final del arreglo.
- `Desplazar(n, k)` agrega `k` ceros al principio del arreglo, es decir multiplica por `10^k`.
- `Comparar(a, b)` devuelve `-1` si `a < b`, `0` si `a = b` y `1` si `a > b`.
- En `Restar(a, b)` se supone que `a >= b`.
- En `Dividir(a, b)` se supone que `b > 0`.

## Suma

```text
Funcion Sumar(a, b)
    largo <- Maximo(Longitud(a), Longitud(b))
    resultado <- ArregloDeCeros(largo + 1)
    acarreo <- 0

    Para i <- 0 Hasta largo - 1 Hacer
        da <- 0
        db <- 0

        Si i < Longitud(a) Entonces
            da <- a[i]
        FinSi

        Si i < Longitud(b) Entonces
            db <- b[i]
        FinSi

        total <- da + db + acarreo
        resultado[i] <- total MOD 10
        acarreo <- total DIV 10
    FinPara

    Si acarreo > 0 Entonces
        resultado[largo] <- acarreo
    FinSi

    Retornar Normalizar(resultado)
FinFuncion
```

## Resta

```text
Funcion Restar(a, b)
    resultado <- ArregloDeCeros(Longitud(a))
    prestamo <- 0

    Para i <- 0 Hasta Longitud(a) - 1 Hacer
        da <- a[i] - prestamo
        db <- 0

        Si i < Longitud(b) Entonces
            db <- b[i]
        FinSi

        Si da < db Entonces
            da <- da + 10
            prestamo <- 1
        Sino
            prestamo <- 0
        FinSi

        resultado[i] <- da - db
    FinPara

    Retornar Normalizar(resultado)
FinFuncion
```

## Multiplicación

```text
Funcion Multiplicar(a, b)
    resultado <- [0]

    Para i <- 0 Hasta Longitud(b) - 1 Hacer
        termino <- Desplazar(a, i)

        Para j <- 1 Hasta b[i] Hacer
            resultado <- Sumar(resultado, termino)
        FinPara
    FinPara

    Retornar Normalizar(resultado)
FinFuncion
```

## División entera

```text
Funcion Dividir(a, b)
    Si Comparar(a, b) < 0 Entonces
        Retornar [0]
    FinSi

    resto <- [0]
    cocienteNormal <- ListaVacia()

    Para i <- Longitud(a) - 1 Hasta 0 Con Paso -1 Hacer
        resto <- Desplazar(resto, 1)
        resto <- Sumar(resto, [a[i]])

        digito <- 0

        Mientras Comparar(resto, b) >= 0 Hacer
            resto <- Restar(resto, b)
            digito <- digito + 1
        FinMientras

        Agregar(cocienteNormal, digito)
    FinPara

    cociente <- InvertirAArreglo(cocienteNormal)
    Retornar Normalizar(cociente)
FinFuncion
```
