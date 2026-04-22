# Presentaciones Marp - Curso de C#

## Descripción

Este directorio contiene 12 presentaciones Marp basadas en los temas principales del curso de C#. Cada presentación ha sido convertida a HTML para visualización en navegador.

## Presentaciones Incluidas

### 1. **02.010 - Variables y Memoria en C#**
   - Variables y tipos
   - Tipado estático y tipado fuerte
   - Stack vs Heap
   - Tipos por valor vs por referencia

### 2. **02.020 - Tipos de Datos y Operadores**
   - Tipos primitivos (int, double, bool, char)
   - Literales y sufijos
   - Operadores aritméticos, lógicos y bit a bit
   - Conversión de tipos

### 3. **02.030 - Enumeraciones**
   - Declaración y uso de enums
   - Tipos subyacentes
   - Pattern matching con enums
   - Flags y serialización

### 4. **02.040 - Strings en C#**
   - Construcción de strings (concatenación, interpolación, verbatim)
   - Inmutabilidad y StringBuilder
   - Comparación y métodos comunes
   - Unicode y emojis

### 5. **02.050 - Tipos Compuestos**
   - Arrays y arrays multidimensionales
   - Tuplas
   - List<T>, Dictionary<K,V>, HashSet<T>
   - Records

### 6. **02.060 - Clases y Objetos**
   - Declaración de clases
   - Propiedades y constructores
   - Herencia y métodos virtuales
   - Clases abstractas y static
   - IDisposable y destructores

### 7. **02.070 - Null y Tipos Anulables**
   - Nullable value types (T?)
   - Nullable reference types
   - Operadores ?., ??, ??=, !
   - Pattern matching con null

### 8. **02.080 - Interfaces y Contratos**
   - Declaración de interfaces
   - Implementación múltiple
   - Métodos por defecto
   - Interfaces genéricas

### 9. **02.090 - Funciones, Delegados y Eventos**
   - Funciones locales y recursión
   - Delegados y multicast
   - Func<>, Action<>, Predicate<>
   - Expresiones lambda
   - Eventos y EventHandler

### 10. **02.100 - Control de Flujo y Excepciones**
   - if, else if, else
   - Operador ternario
   - switch instruction y switch expression
   - Bucles (for, while, foreach)
   - Try/catch/finally y excepciones personalizadas

### 11. **02.110 - Switch Expression y Pattern Matching**
   - Constant, type, property patterns
   - Relational, logical, tuple patterns
   - Positional y list patterns
   - Guardias with when
   - Casos de uso reales

### 12. **02.120 - LINQ**
   - Where, Select, OrderBy
   - GroupBy, Join, SelectMany
   - Agregación (Count, Sum, Average)
   - Lazy evaluation vs ToList()
   - Query syntax

## Cómo Usar

### Ver las presentaciones:
1. Abre cualquier archivo `.html` en tu navegador
2. Usa las flechas del teclado para navegar entre slides
3. Presiona `F` para pantalla completa
4. Presiona `?` para ver los atajos disponibles

### Editar las presentaciones:
1. Edita los archivos `.md` en tu editor preferido
2. Reconvierte con marp usando:
   ```bash
   npx @marp-team/marp-cli --theme default --html archivo.md --output archivo.html
   ```

## Características de las Presentaciones

- **Tema**: Default (light)
- **Paginación**: Habilitada
- **Código**: Con resaltado de sintaxis
- **Responsive**: Compatible con navegadores modernos

## Contenido de Cada Presentación

Cada presentación incluye:
- Diapositiva de título
- Conceptos clave explicados
- Ejemplos de código en C#
- Tablas de referencia
- Buenas prácticas
- Resumen final

## Notas

- Las presentaciones están diseñadas para aprendizaje autodidacta
- Incluyen código ejecutable en C# moderno (C# 8+)
- Se pueden usar como material de referencia durante el estudio

---

**Generado**: Abril 2026
**Formato**: Marp (Markdown Presentation Ecosystem)
**Versión Marp CLI**: 4.3.1
