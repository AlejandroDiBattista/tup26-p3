# Genéricos en C#: Generalización progresiva de la clase `Conjunto`

Esta serie de clases muestra cómo una misma idea —un **conjunto** de elementos— evoluciona desde una implementación concreta y limitada hasta una solución genérica, reutilizable e integrada con el ecosistema de C#.

---

## Paso 1 — `Conjunto` con array propio (implementación manual)

**Archivo:** `13.1-genericos-conjunto-array.cs`

En lugar de delegar en `List<int>`, se implementa el almacenamiento con un `int[]` propio. Esto exige manejar la capacidad y el conteo manualmente.

```csharp
class Conjunto {
    int[] elementos;
    int count;

    public Conjunto(int capacidad = 10) {
        elementos = new int[capacidad];
        count = 0;
    }

    public void Eliminar(int valor) {
        // Reemplaza el elemento eliminado con el último del array
        for (int i = 0; i < count; i++) {
            if (elementos[i] == valor) {
                elementos[i] = elementos[count - 1];
                count--;
                return;
            }
        }
    }
}
```

También se agregan operaciones de conjunto usando **métodos estáticos** y **operadores sobrecargados**:

```csharp
public static Conjunto Union(Conjunto a, Conjunto b) { ... }
public static Conjunto Interseccion(Conjunto a, Conjunto b) { ... }

public static Conjunto operator |(Conjunto a, Conjunto b) => Union(a, b);
public static Conjunto operator &(Conjunto a, Conjunto b) => Interseccion(a, b);
```

Esto permite escribir `var d = b & c;` en lugar de llamar al método directamente.

**Problema:** Sigue siendo solo para `int`, y ahora hay más código manual para mantener.

---

## Paso 2 — `Conjunto` con `List<int>` (composición)

**Archivo:** `13.2-genericos-conjunto-list.cs`

Se vuelve a usar `List<int>` pero con las operaciones de unión e intersección ya incorporadas. Este paso consolida la API antes de generalizarla.

```csharp
// Composición: usamos una clase para implementar la funcionalidad de otra.
class Conjunto {
    List<int> elementos;

    public List<int> Elementos => elementos.ToList(); // Devuelve copia para proteger el estado interno
}
```

> **Composición** significa que `Conjunto` *tiene* una `List<int>`, en vez de *ser* una `List<int>` (herencia). Así controlamos exactamente qué operaciones se exponen.

**Problema:** Sigue sin poder usarse con otros tipos.

---

## Paso 3 — `Conjunto<T>` genérico con `IConjunto<T>`

**Archivo:** `13.3-genericos-conjunto-list-generica.cs`

Este es el salto conceptual más importante: se introduce el **tipo genérico `T`**.

```csharp
// Interface genérica: contrato que debe cumplir cualquier implementación de conjunto
interface IConjunto<T> where T : IEquatable<T> {
    void Agregar(T valor);
    void Eliminar(T valor);
    bool Contiene(T valor);
    List<T> Elementos { get; }
    int Count { get; }
}

// Clase genérica: T se especifica al crear la instancia
public class Conjunto<T> : IConjunto<T> where T : IEquatable<T> {
    List<T> elementos;

    public bool Contiene(T valor) {
        foreach (var e in elementos) {
            if (e.Equals(valor)) return true; // Usa IEquatable<T>
        }
        return false;
    }
}
```

La restricción `where T : IEquatable<T>` garantiza que los elementos saben compararse entre sí, lo que es necesario para detectar duplicados.

Ahora se puede usar la misma clase con cualquier tipo:

```csharp
var numeros  = new Conjunto<int>();
var palabras = new Conjunto<string>();
```

También se muestra que distintas implementaciones pueden interoperar gracias a la interface:

```csharp
// b es Conjunto<string>, c es ConjuntoString — ambos implementan IConjunto<string>
var d = Union(b, c);

static IConjunto<T> Union<T>(IConjunto<T> a, IConjunto<T> b) where T : IEquatable<T> { ... }
```

---

## Paso 4 — Clases de dominio con `IEquatable<T>`

**Archivo:** `13.4-genericos-conjunto-list-generica-equatable.cs`

¿Qué pasa cuando queremos un `Conjunto<Alumno>`? La clase `Alumno` debe decirle al sistema *cuándo dos alumnos son iguales*. Eso se hace implementando `IEquatable<Alumno>`:

```csharp
class Alumno(string nombre, int legajo) : IEquatable<Alumno> {
    public string Nombre => nombre;
    public int Legajo => legajo;

    // Dos alumnos son el mismo si tienen el mismo legajo, sin importar el nombre
    public bool Equals(Alumno? otro) {
        if (otro is null) return false;
        return this.Legajo == otro.Legajo;
    }
}
```

```csharp
var clase = new Conjunto<Alumno>();
clase.Agregar(new Alumno("Ana",   20));
clase.Agregar(new Alumno("Anita", 20)); // No se agrega: mismo legajo que Ana
clase.Agregar(new Alumno("Bob",   22));
// Resultado: { Ana (20 años), Bob (22 años) }
```

Gracias a `IEquatable<T>`, `elementos.Contains(valor)` funciona correctamente sin necesidad de loops manuales.

---

## Paso 5 — Interface con `IReadOnlyList<T>`

**Archivo:** `13.5-genericos-conjunto-list-generica-equatable-enumerable.cs`

Se refina la interface para que `Elementos` devuelva `IReadOnlyList<T>` en lugar de `List<T>`. Esto expone los datos sin permitir que el llamador modifique la colección interna:

```csharp
interface IConjunto<T> where T : IEquatable<T> {
    void Agregar(T valor);
    void Eliminar(T valor);
    bool Contiene(T valor);
    int Count { get; }
    IReadOnlyList<T> Elementos { get; }  // Solo lectura
}
```

---

## Paso 6 — `IEnumerable<T>`: soporte para `foreach` y LINQ

**Archivo:** `13.6-genericos-conjunto-list-generica-equatable-Enumerator.cs`

Para poder escribir `foreach (var alumno in clase)` directamente, o usar métodos LINQ como `.Where()`, la clase debe implementar `IEnumerable<T>`:

```csharp
interface IConjunto<T> : IEnumerable<T> where T : IEquatable<T> { ... }

class Conjunto<T> : IConjunto<T> where T : IEquatable<T> {

    // yield return entrega los elementos uno a uno al iterador
    public IEnumerator<T> GetEnumerator() {
        foreach (var elemento in elementos) {
            yield return elemento;
        }
    }

    // Requerido por compatibilidad con la interface no genérica IEnumerable
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

Con esto el conjunto se puede usar en cualquier contexto que espere una secuencia:

```csharp
foreach (var alumno in clase) { ... }

clase.Where(a => a.Legajo > 20).ToList().ForEach(a => Console.WriteLine(a));
```

---

## Paso 7 — `IComparable<T>`: soporte para ordenamiento

**Archivo:** `13.7-genericos-conjunto-list-generica-equatable-Enumerator-comparator.cs`

El último paso agrega `IComparable<T>` a la clase de dominio, lo que permite usar `OrderBy` de LINQ:

```csharp
class Alumno(string nombre, int legajo) : IEquatable<Alumno>, IComparable<Alumno> {

    // Define el orden natural: por legajo, de menor a mayor
    public int CompareTo(Alumno? otro) {
        if (otro is null) return 1;
        return this.Legajo.CompareTo(otro.Legajo);
    }
}
```

```csharp
// Filtra alumnos con legajo > 20 y los ordena por legajo
foreach (var alumno in clase.Where(a => a.Legajo > 20).OrderBy(a => a)) {
    Console.WriteLine($" - {alumno}");
}
```

---

## Resumen de la evolución

| Paso | Archivo | Novedad principal |
|------|---------|-------------------|
| 0 | `13.0` | `Conjunto` con `List<int>`, versión mínima |
| 1 | `13.1` | Almacenamiento propio con `int[]`, operadores `\|` y `&` |
| 2 | `13.2` | Composición con `List<int>`, API consolidada |
| 3 | `13.3` | `Conjunto<T>` + `IConjunto<T>` + `where T : IEquatable<T>` |
| 4 | `13.4` | Clase de dominio `Alumno` implementa `IEquatable<T>` |
| 5 | `13.5` | `IReadOnlyList<T>` para proteger el estado interno |
| 6 | `13.6` | `IEnumerable<T>` + `yield return` → `foreach` y LINQ |
| 7 | `13.7` | `IComparable<T>` → `OrderBy` en LINQ |

---

## Conceptos clave

- **Genéricos (`<T>`):** permiten escribir una clase o método una sola vez y usarla con cualquier tipo.
- **Restricciones (`where`):** limitan qué tipos puede ser `T`, garantizando que tenga ciertas capacidades.
- **`IEquatable<T>`:** el tipo sabe comparar dos instancias por igualdad (necesario para detectar duplicados).
- **`IComparable<T>`:** el tipo sabe ordenarse (necesario para `OrderBy` y similares).
- **`IEnumerable<T>` + `yield return`:** permite iterar con `foreach` y usar todo el ecosistema LINQ.
- **Composición:** la clase *tiene* una colección interna en lugar de *heredar* de ella, lo que da control total sobre la API expuesta.
