# TP2 - Compilador de Expresiones Aritméticas (Calculadora)

Este proyecto es un compilador que utiliza un **Parser de Descenso Recursivo** para evaluar expresiones matemáticas con soporte para la variable `x`.

## 🛠️ Características
- Soporta operadores binarios: `+`, `-`, `*`, `/`.
- Soporta operadores unarios: `+`, `-`.
- Manejo de paréntesis y precedencia de operadores.
- Modo directo e interactivo.

## Cómo ejecutar
### Modo Directo
`dotnet run -- "expresión" valor`
Ejemplo: `dotnet run -- "(x + 2) * 3" 10`

### Modo Interactivo
`dotnet run`

### Pruebas Automáticas
`dotnet run -- --test`

## Estructura del Proyecto
- **Nodos.cs**: Representación del Árbol de Sintaxis Abstracta (AST).
- **Compilador.cs**: Lógica del Parser y Tokenizer.
- **Programa.cs**: Punto de entrada y gestión de modos.
- **Pruebas.cs**: Batería de tests de validación.