Aquí tienes las implementaciones de la función factorial "ultra simple" en los lenguajes que solicitaste. La simplicidad se refiere a una implementación recursiva directa sin optimizaciones adicionales.

---

### C#

```csharp
using System;

public class Factorial
{
    public static int CalculateFactorial(int n)
    {
        if (n < 0)
        {
            throw new ArgumentException("Factorial is not defined for negative numbers.");
        }
        if (n == 0)
        {
            return 1;
        }
        return n * CalculateFactorial(n - 1);
    }

    // Ejemplo de uso
    // public static void Main(string[] args)
    // {
    //     int number = 5;
    //     int result = CalculateFactorial(number);
    //     Console.WriteLine($"El factorial de {number} es: {result}"); // Output: El factorial de 5 es: 120
    // }
}
```

---

### JavaScript

```javascript
function calculateFactorial(n) {
  if (n < 0) {
    throw new Error("Factorial is not defined for negative numbers.");
  }
  if (n === 0) {
    return 1;
  }
  return n * calculateFactorial(n - 1);
}

// Ejemplo de uso
// const number = 5;
// const result = calculateFactorial(number);
// console.log(`El factorial de ${number} es: ${result}`); // Output: El factorial de 5 es: 120
```

---

### Python

```python
def calculate_factorial(n):
  if n < 0:
    raise ValueError("Factorial is not defined for negative numbers.")
  if n == 0:
    return 1
  return n * calculate_factorial(n - 1)

# Ejemplo de uso
# number = 5
# result = calculate_factorial(number)
# print(f"El factorial de {number} es: {result}") # Output: El factorial de 5 es: 120
```

---

### Ruby

```ruby
def calculate_factorial(n)
  if n < 0
    raise ArgumentError, "Factorial is not defined for negative numbers."
  end
  if n == 0
    return 1
  end
  n * calculate_factorial(n - 1)
end

# Ejemplo de uso
# number = 5
# result = calculate_factorial(number)
# puts "El factorial de #{number} es: #{result}" # Output: El factorial de 5 es: 120
```

---

### Go

```go
package main

import "fmt"

func CalculateFactorial(n int) int {
        if n < 0 {
                panic("Factorial is not defined for negative numbers.")
        }
        if n == 0 {
                return 1
        }
        return n * CalculateFactorial(n-1)
}

// Ejemplo de uso
// func main() {
//      number := 5
//      result := CalculateFactorial(number)
//      fmt.Printf("El factorial de %d es: %d\n", number, result) // Output: El factorial de 5 es: 120
// }
```

---

### Dart

```dart
int calculateFactorial(int n) {
  if (n < 0) {
    throw ArgumentError("Factorial is not defined for negative numbers.");
  }
  if (n == 0) {
    return 1;
  }
  return n * calculateFactorial(n - 1);
}

// Ejemplo de uso
// void main() {
//   int number = 5;
//   int result = calculateFactorial(number);
//   print("El factorial de $number es: $result"); // Output: El factorial de 5 es: 120
// }
```

---

### Swift

```swift
func calculateFactorial(n: Int) -> Int {
    if n < 0 {
        fatalError("Factorial is not defined for negative numbers.")
    }
    if n == 0 {
        return 1
    }
    return n * calculateFactorial(n: n - 1)
}

// Ejemplo de uso
// let number = 5
// let result = calculateFactorial(n: number)
// print("El factorial de \(number) es: \(result)") // Output: El factorial de 5 es: 120
```

---

**Notas sobre la "ultra simplicidad":**

*   **Recursión:** Todas estas implementaciones utilizan la recursión, que es una forma elegante y directa de definir el factorial matemáticamente ($n! = n \times (n-1)!$).
*   **Casos Base:** El caso base para el factorial es $0! = 1$. También se incluye un manejo para números negativos, ya que el factorial no está definido para ellos.
*   **Potencial de Desbordamiento:** Estas implementaciones son simples, pero **no manejan desbordamientos de enteros**. Para números grandes, el resultado del factorial crecerá muy rápidamente y superará los límites de los tipos de datos enteros estándar. En un entorno de producción, deberías considerar usar tipos de datos que soporten números muy grandes (como `BigInteger` en C# o Python, o bibliotecas específicas en otros lenguajes) o implementar un enfoque iterativo con validación.
*   **Limitaciones de la Recursión:** Para números muy grandes, la recursión profunda puede llevar a un desbordamiento de la pila (Stack Overflow Error). Las implementaciones iterativas son generalmente más eficientes y seguras para valores grandes.

Estas son las versiones más directas y sencillas basadas en la definición recursiva.

✦ 4,77s
tup26-p3/clases ▶ 