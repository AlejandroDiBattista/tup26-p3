# parser_app.py

`parser_app.py` implementa un analizador recursivo descendente para expresiones aritméticas, construye un AST, lo muestra como árbol y como JSON, y además evalúa el resultado.

## Qué hace

Soporta:

- números enteros y decimales
- operadores binarios: `+`, `-`, `*`, `/`
- operadores unarios: `+`, `-`
- paréntesis
- espacios en blanco

Ejemplos válidos:

- `2 + 3 * 4`
- `(1 + 2) * 5`
- `-3 + 10`
- `4 / 2.5`

## Estructura del archivo

### `ParserError`
Excepción personalizada para errores de léxico, parsing o evaluación.

### Nodos del AST
Se definen con `@dataclass`:

- `NumberNode`: representa un número
- `BinaryOpNode`: representa una operación binaria
- `UnaryOpNode`: representa una operación unaria

### `Lexer`
Convierte el texto de entrada en una lista de tokens.

Reconoce:

- `NUMBER`
- `+`
- `-`
- `*`
- `/`
- `(`
- `)`
- `EOF`

Métodos principales:

- `current_char()`: devuelve el carácter actual
- `advance()`: avanza una posición
- `skip_whitespace()`: ignora espacios
- `number()`: parsea números enteros o decimales
- `tokens()`: genera la lista completa de tokens

## Gramática implícita

El parser implementa esta precedencia:

```text
expr   -> term (("+" | "-") term)*
term   -> factor (("*" | "/") factor)*
factor -> "+" factor
       | "-" factor
       | NUMBER
       | "(" expr ")"
```

## `Parser`
Construye el AST a partir de los tokens generados por `Lexer`.

Métodos principales:

- `parse()`: punto de entrada del análisis
- `expr()`: maneja suma y resta
- `term()`: maneja multiplicación y división
- `factor()`: maneja números, paréntesis y operadores unarios
- `eat(token_type)`: consume el token esperado o lanza error

## `Evaluator`
Recorre el AST y calcula el valor numérico de la expresión.

Casos evaluados:

- `NumberNode` → devuelve el valor
- `UnaryOpNode` → aplica signo positivo o negativo
- `BinaryOpNode` → aplica `+`, `-`, `*`, `/`

Controla además:

- división por cero

## Representación del AST

### `node_to_dict(node)`
Convierte un nodo del AST a diccionario Python.

### `ast_to_json(node)`
Convierte el AST a JSON legible con indentación.

### `format_ast_tree(node)`
Genera una vista tipo árbol usando caracteres como `└──` y `├──`.

## Funciones auxiliares

### `build_ast(expression: str)`
Parsea una expresión y devuelve el AST.

### `evaluate(expression: str) -> float`
Parsea y evalúa una expresión directamente.

## Ejecución

Desde el directorio del archivo:

```bash
python parser_app.py
```

El programa entra en modo interactivo:

- pide una expresión
- construye el AST
- muestra el árbol
- muestra el JSON
- muestra el resultado

Para salir:

- `salir`
- `exit`
- `quit`

## Ejemplo de uso

Entrada:

```text
> 2 + 3 * 4
```

Salida esperada aproximada:

```text
AST como árbol:
└── BinaryOp(+)
    ├── Number(2.0)
    └── BinaryOp(*)
        ├── Number(3.0)
        └── Number(4.0)

AST como JSON:
{
  "type": "BinaryOp",
  "op": "+",
  "left": {
    "type": "Number",
    "value": 2.0
  },
  "right": {
    "type": "BinaryOp",
    "op": "*",
    "left": {
      "type": "Number",
      "value": 3.0
    },
    "right": {
      "type": "Number",
      "value": 4.0
    }
  }
}

Resultado: 14
```

## Errores posibles

Algunos errores que puede informar:

- `Carácter inesperado: x`
- `Número inválido`
- `Se esperaba ) y se encontró EOF`
- `Token inesperado: *`
- `División por cero`

## Resumen

`parser_app.py` combina tres etapas clásicas:

1. análisis léxico (`Lexer`)
2. análisis sintáctico (`Parser`)
3. evaluación del AST (`Evaluator`)

Además agrega dos formatos de visualización del árbol sintáctico: árbol de texto y JSON.
