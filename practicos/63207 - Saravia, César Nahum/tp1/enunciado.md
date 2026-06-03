# Trabajo Práctico — Herramienta CLI: `sortx`

**Entrega:**  8 de ABRIL de 2025 a las 23:59hs

---

## Descripción

Desarrollar una herramienta de línea de comandos llamada **`sortx`** que lea un archivo de texto delimitado (CSV, TSV, PSV u otro), ordene sus filas según los criterios indicados, y escriba el resultado.

La herramienta debe ser un único archivo `sortx.cs`, implementado como *file-based program* de C# sin clases auxiliares, usando `record` para la configuración y funciones locales para cada paso del proceso.

---

## Sintaxis

```
sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
      [-i|--input input] [-o|--output output]
      [-d|--delimiter delimitador]
      [-nh|--no-header] [-h|--help]
```

---

## Opciones

| Opción larga    | Corta  | Descripción |
|-----------------|--------|-------------|
| `--by`          | `-b`   | Campo por el que ordenar. Se puede repetir para ordenamiento múltiple. |
| `--input`       | `-i`   | Archivo de entrada. |
| `--output`      | `-o`   | Archivo de salida. |
| `--delimiter`   | `-d`   | Carácter delimitador. Default: `,`. Usar `\t` para tabulación. |
| `--no-header`   | `-nh`  | Indica que el archivo no tiene fila de encabezado. En ese caso los campos se identifican por su índice numérico (0, 1, 2...). |
| `--help`        | `-h`   | Muestra la ayuda y termina. |

### Especificación de campo: `campo[:tipo[:orden]]`

Cada valor de `--by` tiene el formato `campo[:tipo[:orden]]`, donde:

- **`campo`** — nombre de la columna (si hay encabezado) o índice numérico desde 0 (si no hay encabezado).
- **`tipo`** — criterio de comparación:
  - `alpha` — comparación alfabética (default).
  - `num` — comparación numérica.
- **`orden`** — dirección:
  - `asc` — ascendente (default).
  - `desc` — descendente.

**Ejemplos de especificación:**

| Expresión            | Significado |
|----------------------|-------------|
| `apellido`           | Por `apellido`, alfabético, ascendente |
| `salario:num`        | Por `salario`, numérico, ascendente |
| `salario:num:desc`   | Por `salario`, numérico, descendente |
| `2:num:asc`          | Por columna índice 2, numérico, ascendente (sin encabezado) |

---

## Archivos de entrada y salida

- El archivo de entrada puede especificarse como **primer argumento posicional** o con `-i|--input`.
- El archivo de salida puede especificarse como **segundo argumento posicional** o con `-o|--output`.
- Si el archivo de entrada **no se especifica**, la herramienta debe leer desde **stdin**.
- Si el archivo de salida **no se especifica**, la herramienta debe escribir en **stdout**.
- Esto permite encadenar la herramienta con otros comandos:

```bash
cat datos.csv | sortx -b apellido > ordenado.csv
```

---

## Comportamiento esperado

### Ordenamiento múltiple

Cuando se especifican varios `--by`, se ordenan por el primer campo; en caso de empate, por el segundo, y así sucesivamente.

```bash
sortx empleados.csv -b departamento -b salario:num:desc
```

Ordena por `departamento` alfabéticamente; dentro de cada departamento, por `salario` de mayor a menor.

### Sin encabezado

Con `--no-header`, no se toma la primera fila como encabezado y los campos se referencian por índice:

```bash
sortx datos.csv -nh -b 2:num:desc
```

La primera fila es un dato más y se ordena junto con el resto. La salida tampoco incluye encabezado.

### Con encabezado (comportamiento default)

La primera fila se preserva en la salida como encabezado, independientemente del ordenamiento.

### Delimitador

```bash
sortx datos.tsv -d "\t" -b nombre
sortx datos.psv -d "|"  -b nombre
```

---

## Ejemplos de uso

```bash
# Ordenar por apellido (alfabético, ascendente)
sortx empleados.csv -b apellido

# Ordenar por salario descendente y guardar en archivo
sortx empleados.csv resultado.csv -b salario:num:desc

# Múltiples criterios
sortx empleados.csv -b departamento -b salario:num:desc -o resultado.csv

# Con opciones explícitas
sortx -i empleados.csv -o resultado.csv -b apellido

# TSV sin encabezado, ordenar por columna 1 (segunda columna)
sortx datos.tsv -d "\t" -nh -b 1:alpha:asc

# Usando redirección
cat empleados.csv | sortx -b apellido > ordenado.csv

# Ayuda
sortx --help
```

---

## Diseño requerido

El programa debe seguir el siguiente pipeline, implementando cada paso como una función local independiente:

```
1. ParseArgs      → leer la configuración desde los argumentos
2. ReadInput      → leer el texto desde el archivo o stdin
3. ParseDelimited → convertir el texto en una lista de filas (lista de diccionarios)
4. SortRows       → ordenar las filas según los criterios configurados
5. Serialize      → convertir las filas ordenadas de vuelta a texto
6. WriteOutput    → escribir en el archivo de salida o stdout
```

El punto de entrada (`try/catch` principal) debe limitarse a invocar estas funciones en orden, sin lógica adicional.

### Modelo de configuración

Se debe definir un `record` con al menos los siguientes datos:

```csharp
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields
);
```

---

## Archivo de prueba

Para verificar el funcionamiento, utilizar el siguiente `empleados.csv`:

```csv
nombre,apellido,edad,salario,departamento
Carlos,García,35,85000,Ingeniería
Ana,Martínez,28,72000,Diseño
Luis,Rodríguez,42,120000,Gerencia
María,López,31,88000,Ingeniería
Pedro,Sánchez,25,65000,Diseño
Laura,González,38,95000,Gerencia
```

### Casos de prueba mínimos

| Comando | Resultado esperado |
|---|---|
| `sortx empleados.csv -b apellido` | Filas ordenadas por apellido A→Z |
| `sortx empleados.csv -b salario:num:desc` | De mayor a menor salario |
| `sortx empleados.csv -b departamento -b salario:num:desc` | Por depto, dentro por salario desc |
| `sortx empleados.csv -b apellido:alpha:asc -o salida.csv` | Genera `salida.csv` |
| `cat empleados.csv \| sortx -b apellido` | Mismo resultado, leyendo desde stdin |
| `sortx empleados.csv -b columnaInexistente` | Error en stderr, código de salida ≠ 0 |
| `sortx --help` | Muestra ayuda y termina con código 0 |

---

## Entrega

- Archivo `sortx.cs` completo.

> [!NOTE]
> Si bien el comando `sortx` se menciona para ilustrar el uso, la entrega es un archivo `sortx.cs` que se compila y ejecuta con `dotnet run sortx.cs -- [args]`. 

