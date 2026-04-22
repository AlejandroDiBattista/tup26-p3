# Trabajo Práctico 2 — Compilador de Expresiones Aritméticas con Variable: `calculadora`

**Entrega:** 22 de ABRIL de 2026 a las 23:59hs

---

## Descripción

Desarrollar una aplicación de consola llamada **`calculadora`** que permita **parsear y evaluar expresiones aritméticas enteras** con soporte para la variable **`x`**.

La herramienta debe poder trabajar de dos maneras:

- **Modo directo**, recibiendo una expresión y un valor para `x` por línea de comandos.
- **Modo interactivo**, permitiendo ingresar una expresión una vez y luego evaluarla varias veces con distintos valores de `x`.

El objetivo del trabajo es modelar un **árbol de sintaxis abstracta (AST)** y construir un **parser de descenso recursivo** que respete precedencia de operadores, paréntesis y operadores unarios.

---

## Sintaxis

```bash
calculadora [expresion valor] [--help] [--probar]
```

---

## Opciones

| Opción larga | Corta | Descripción                  |
| ------------ | ----- | ---------------------------- |
| `--help`     | `-h`  | Muestra la ayuda y termina.  |
| `--test`     | `-t`  | Ejecuta pruebas automáticas. |

### Argumentos posicionales

| Argumento   | Descripción                                                                           |
| ----------- | ------------------------------------------------------------------------------------- |
| `expresion` | Fórmula a evaluar. Incluir números enteros, operadores, paréntesis y la variable `x`. |
| `valor`     | Valor entero con el que se reemplaza la variable `x` al evaluar la expresión.         |

---

## Expresiones soportadas

La calculadora debe aceptar expresiones formadas por:

- **Números enteros**: `0`, `15`, `123`
- **Variable**: `x` o `X`
- **Operadores binarios**:
  - `+` suma
  - `-` resta
  - `*` multiplicación
  - `/` división entera
- **Operadores unarios**:
  - `+` positivo
  - `-` negación
- **Paréntesis**: `(` y `)`

### Precedencia

El parser debe respetar el siguiente orden de precedencia:

1. Paréntesis
2. Operadores unarios `+` y `-`
3. Multiplicación y división
4. Suma y resta

---

## Comportamiento esperado

### Modo directo

Si se invoca con dos argumentos posicionales:

```bash
calculadora "(x - 1) * 2" 10
```

el programa debe:

1. Parsear la expresión.
2. Reemplazar `x` por el valor indicado.
3. Evaluar el AST resultante.
4. Mostrar el resultado por `stdout`.

### Modo interactivo

Si se ejecuta sin argumentos, el programa debe:

1. Pedir una expresión matemática con la variable `x`.
2. Compilar esa expresión una sola vez.
3. Pedir sucesivamente valores para `x`.
4. Mostrar el resultado de cada evaluación.
5. Finalizar cuando el usuario ingrese `fin` o una entrada vacía.

### Ayuda

Si se invoca con `--help`, `-h` debe mostrar una ayuda breve y terminar con código de salida `0`.

### Pruebas

Si se invoca con `--test`, `-p` o `-t`, debe ejecutar un conjunto de pruebas automáticas y reportar si pasaron correctamente.

### Errores

Ante expresiones inválidas, el programa debe informar el error con un mensaje claro. Algunos casos esperables:

- Token inesperado
- Paréntesis sin cerrar
- Entrada vacía
- División por cero
- Valor de `x` inválido

---

## Ejemplos de uso

```bash
# Evaluación directa
calculadora "1 + 2 * 3" 0
# Salida: 7

# Uso de la variable x
calculadora "1 + 2 * x" 10
# Salida: 21

# Paréntesis y precedencia
calculadora "(x - 1) * (x - 8 / 4) + 3" 5
# Salida: 15

# Modo interactivo
calculadora

# Ayuda
calculadora --help

# Ejecutar pruebas
calculadora --probar
```

---

## Diseño requerido

El programa debe separar claramente las siguientes responsabilidades:

```text
1. Procesar comandos      → interpretar args y decidir el modo de ejecución
2. Parsear expresión      → convertir texto en un AST
3. Representar nodos      → modelar números, variable, unarios y binarios
4. Evaluar AST            → calcular el resultado para un valor de x
5. Ejecutar pruebas       → verificar precedencia y evaluación
```

### Modelo de nodos

Se espera una jerarquía de tipos similar a la siguiente:

```csharp
abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo;
class VariableNodo : Nodo;
class NegativoNodo : Nodo;

abstract class NodoBinario : Nodo;
class SumaNodo : NodoBinario;
class RestaNodo : NodoBinario;
class MultiplicacionNodo : NodoBinario;
class DivisionNodo : NodoBinario;
```

### Parser

El parser debe implementarse mediante **descenso recursivo**, con una estructura equivalente a:

```text
Expresion := Termino { ('+' | '-') Termino }
Termino   := Factor  { ('*' | '/') Factor }
Factor    := '+' Factor
          | '-' Factor
          | '(' Expresion ')'
          | numero
          | x
```

### Organización sugerida

La solución puede dividirse en archivos similares a:

- `Programa.cs` para el punto de entrada y el modo interactivo
- `Compilador.cs` para el parser
- `Nodos.cs` para la jerarquía del AST
- `Comandos.cs` para el procesamiento de argumentos
- `Pruebas.cs` para las pruebas automáticas

---

## Casos de prueba mínimos

| Comando                                        | Resultado esperado                    |
| ---------------------------------------------- | ------------------------------------- |
| `calculadora "1 + 2 * 3" 0`                    | `7`                                   |
| `calculadora "1 + 2 * x" 10`                   | `21`                                  |
| `calculadora "(x - 1) * (x - 8 / 4) + 3" 10`   | `75`                                  |
| `calculadora "-(3 + 2)" 0`                     | `-5`                                  |
| `calculadora "10 / 2" 0`                       | `5`                                   |
| `calculadora --help`                           | Muestra ayuda y termina con código 0  |
| `calculadora --test`                           | Ejecuta pruebas automáticas           |
| `calculadora "(1 + 2" 0`                       | Error de parsing                      |

---

## Entrega

- Proyecto completo en la carpeta `enunciados/tp2`.

> [!NOTE]
> A pesar de que en el enunciado se muestra `calculadora --help`
> durante el desarroll se recomienda ejecutar el proyecto directamente con `dotnet run`
> desde la carpeta del proyecto, pasando los argumentos después de `tp2`:
> `dotnet run -- --help` o `dotnet run -- "(x - 1) * 2" 10`. Esto facilita la depuración y el desarrollo iterativo.
