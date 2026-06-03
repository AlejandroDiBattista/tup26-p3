# Trabajo Práctico 3 — `AgendaT` - 
## Aplicación de Agenda TUI con persistencia en SQLite e import/export JSON

**Entrega:** Hasta el viernes 22 de MAYO de 2026 a las 21:00hs

---

## Descripción

Desarrollar una aplicación de terminal llamada **`agenda`** que permita **gestionar una agenda de contactos** desde una **interfaz de usuario en terminal (TUI)**, con persistencia en **SQLite** e intercambio de datos en **JSON**.

La aplicación debe ofrecer un CRUD completo de contactos (alta, baja, modificación, consulta) con búsqueda activa, además de importación y exportación a archivos JSON.

El objetivo del trabajo es construir una aplicación interactiva separando claramente las responsabilidades de **interfaz visual**, **lógica de coordinación**, **persistencia en base de datos** e **interoperabilidad con archivos externos**.

---

## Formato de entrega

El trabajo debe entregarse como **un único archivo `.cs`** usando el modo *file-based* de .NET 10 (sin `.csproj`). Las dependencias se declaran al principio del archivo:

> En agenda.cs esta el esqueleto con las dependencias que debe ser tomado como referencia:

Y se ejecuta con:

```bash
dotnet run agenda.cs
dotnet run agenda.cs -- agenda.db
```

A pesar de estar en un solo archivo, **el código debe estar organizado en clases con responsabilidades separadas** (ver sección "Diseño requerido").

---

## Sintaxis

```bash
agenda [archivo.db]
```

### Argumentos posicionales

| Argumento    | Descripción                                                                                              |
| ------------ | -------------------------------------------------------------------------------------------------------- |
| `archivo.db` | Ruta opcional al archivo SQLite. Por defecto `agenda.db`. Si no existe, se crea con el schema necesario. |

---

## Modelo de contacto

Cada contacto debe registrar al menos:

- `Id`       (entero, autogenerado por la base)
- `Nombre`   (texto, obligatorio)
- `Telefonos`(texto)
- `Email`    (texto)
- `Notas`    (texto multilínea)
- `Favorito` (booleano)

---

## Funcionalidad requerida

### Estructura visual

La ventana principal debe contener:

- **Barra de menú superior** con menús desplegables.
- **Campo de búsqueda** activa.
- **Panel de lista** de contactos, con indicador visual para favoritos.
- **Panel de detalle** del contacto seleccionado.
- **Barra de estado** con el mensaje de la última operación.

### Menús

| Menú         | Opciones                                            |
| ------------ | --------------------------------------------------- |
| `Archivo`    | Importar JSON, Exportar JSON, Salir.                |
| `Contactos`  | Nuevo, Editar, Eliminar.                            |
| `Ver`        | Solo favoritos (toggle).                            |
| `Ayuda`      | Acerca de.                                          |

### Operaciones y atajos

| Acción                    | Atajo              |
| ------------------------- | ------------------ |
| Nuevo contacto            | `F2` / `Ctrl+N`    |
| Editar contacto           | `F3` / `Enter`     |
| Eliminar contacto         | `Del` / `Ctrl+D`   |
| Importar desde JSON       | `Ctrl+I`           |
| Exportar a JSON           | `Ctrl+E`           |
| Foco en búsqueda          | `F4`               |
| Salir                     | `Ctrl+Q`           |

### Búsqueda activa

El campo de búsqueda debe filtrar la lista **en tiempo real** mientras el usuario tipea, comparando contra `Nombre`, `Telefonos` y `Email`. El filtro debe combinarse con el toggle de **solo favoritos**.

### Validaciones

- El `Nombre` no puede estar vacío.
- Si se ingresa `Email`, debe contener `@`.
- Antes de **eliminar** un contacto, pedir confirmación.
- Antes de **importar**, mostrar la cantidad de contactos a agregar y pedir confirmación.
- Los telefonos pueden tener hasta 5 numeros separados por comas, pero deben ser ingresados en campos individuales en el diálogo de edición.

### Persistencia

- Toda alta, modificación o eliminación debe quedar **persistida inmediatamente** en la base SQLite.
- Al **importar** un JSON, los contactos se agregan como registros nuevos con `Id` asignado por la base; no se conservan los ids del archivo.
- Al **exportar**, se vuelcan todos los contactos a un archivo JSON legible (con tildes y `ñ` correctas).

---

## Comportamiento esperado

Al iniciar el programa:

1. Abrir (o crear) la base SQLite indicada por argumento, o `agenda.db` si no se indicó ninguna.
2. Cargar los contactos existentes desde la base.
3. Mostrar la interfaz visual.
4. Mantenerse activo respondiendo a teclado y menús hasta que el usuario decida salir.

### Errores

Ante condiciones inválidas, el programa debe informar el error con un mensaje claro mediante un `MessageBox`. Casos esperables:

- Archivo de base inaccesible o corrupto.
- Archivo JSON inexistente al importar.
- JSON con formato inválido.
- Nombre vacío al guardar un contacto.
- Email sin `@` al guardar un contacto.

---

## Ejemplos de uso

```bash
# Iniciar la agenda con la base por defecto (agenda.db)
dotnet run agenda.cs

# Iniciar con un archivo de base específico
dotnet run agenda.cs -- contactos-2026.db
```

Una vez dentro de la TUI:

- `F2` abre el diálogo de nuevo contacto.
- Tipear en el campo de búsqueda filtra la lista.
- `Enter` sobre un contacto abre el diálogo de edición.
- `Ctrl+E` exporta a JSON y pide la ruta de salida.
- `Ctrl+Q` cierra la aplicación.

---

## Diseño requerido

A pesar de estar todo en un único archivo, el código debe separar claramente las siguientes responsabilidades, **cada una en su propia clase**:

```text
1. Procesar argumentos    → top-level code: leer args y arrancar la app
2. Ventana principal      → AgendaWindow: layout, menús, eventos, coordinación
3. Diálogo de edición     → ContactDialog: campos, validación, devolver contacto
4. Persistencia           → SqliteAgendaStore: CRUD con Dapper.Contrib
5. Interoperabilidad JSON → JsonAgendaIO: leer y escribir archivos JSON
6. Modelo de datos        → Contacto: la clase con [Table] y [Key]
```

### Modelo

Se espera una clase de contacto con la siguiente forma general:

```csharp
[Table("Contactos")]
public sealed class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

    public Contacto Clone();
}
```

### Stack tecnológico requerido

- **Terminal.Gui** v2 para la TUI.
- **Microsoft.Data.Sqlite** para la conexión a SQLite.
- **Dapper** + **Dapper.Contrib** para el CRUD.
- **System.Text.Json** para la importación y exportación.

### Reglas de arquitectura

- El diálogo de edición **no** toca la base de datos: sólo devuelve datos. La ventana principal decide si persistir.
- Cada operación CRUD debe actualizar **primero** la base y **después** la lista en memoria.
- La lista visible (`filteredContacts`) es derivada de la lista completa (`contacts`) más los filtros activos; nunca al revés.
- El menú no debe contener lógica: cada `MenuItem` delega en un método de la ventana.

### Organización dentro del archivo

El orden sugerido dentro del archivo `agenda.cs` es:

```text
1. directivas #:package y #:property
2. usings
3. top-level code (procesar args, crear store, arrancar la app)
4. clase AgendaWindow
5. clase ContactDialog
6. clase SqliteAgendaStore
7. clase JsonAgendaIO
8. clase Contacto
```

---

## Casos de prueba mínimos

| Caso                                                               | Resultado esperado                                                          |
| -------------------------------------------------------------------| ----------------------------------------------------------------------|
| `dotnet run agenda.cs`                                             | Crea `agenda.db` si no existe y abre la TUI.                          |
| `dotnet run agenda.cs -- otra.db`                                  | Usa `otra.db` como archivo de base.                                   |
| Crear un contacto con nombre `"Ana"`                               | Aparece en la lista y persiste tras reiniciar la app.                 |
| Crear un contacto con nombre vacío                                 | Error de validación, el diálogo no se cierra.                         |
| Crear un contacto con email `"ana"`                                | Error de validación: el email debe contener `@`.                      |
| Eliminar un contacto                                               | Pide confirmación; al aceptar, desaparece.                            |
| Buscar `"ana"` en una agenda con `"Ana"`, `"Juan"` y `"Susana"`    | La lista muestra `"Ana"` y `"Susana"`.                                |
| Marcar `"Solo favoritos"` con dos favoritos en una lista de cinco  | La lista muestra solo los dos favoritos.                              |
| Exportar a `salida.json` y luego importar el mismo archivo         | Los contactos se duplican (ids nuevos), no chocan con los existentes. |
| Importar un archivo JSON inexistente                               | Mensaje de error claro, la app sigue funcionando.                     |
| Cerrar con `Ctrl+Q`                                                | La aplicación finaliza limpiamente y restaura la terminal.            |

---

## Entrega

- Un único archivo `agenda.cs` en la carpeta `/tp3`.
