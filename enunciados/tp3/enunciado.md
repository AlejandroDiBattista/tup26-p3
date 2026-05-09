# Trabajo Práctico — Agenda TUI con Terminal.Gui

**Entrega:** a definir por la cátedra

---

## Descripción

Desarrollar una aplicación de terminal llamada **Agenda** que permita administrar contactos desde una interfaz TUI (*Text User Interface*) usando **Terminal.Gui v2**.

La aplicación debe ser un único archivo `agenda.cs`, implementado como *file-based program* de C#. Debe persistir sus datos en JSON o SQLite según la extensión del archivo de trabajo.

---

## Sintaxis

```bash
dotnet run agenda.cs -- [archivo.json|archivo.db]
```

---

## Archivo de trabajo

- Si se ejecuta sin parámetros, la aplicación debe trabajar sobre `agenda.json`, ubicado en la misma carpeta que `agenda.cs`.
- Si se pasa un argumento, ese argumento indica el archivo de trabajo.
- Si el archivo termina en `.json`, la agenda debe persistir usando JSON.
- Si el archivo termina en `.db`, la agenda debe persistir usando SQLite.
- Si el archivo de trabajo no existe, la agenda debe empezar vacía y crearlo recién al guardar cambios.

```bash
dotnet run agenda.cs
dotnet run agenda.cs -- clientes.json
dotnet run agenda.cs -- /tmp/agenda-prueba.json
dotnet run agenda.cs -- agenda.db
```

---

## Modelo de datos

Cada contacto debe tener:

| Campo       | Tipo     | Descripción                               |
| ----------- | -------- | ------------------------------------------|
| `id`        | `int`    | Identificador numérico único del contacto.|
| `nombre`    | `string` | Nombre del contacto.                      |
| `apellido`  | `string` | Apellido del contacto.                    |
| `domicilio` | `string` | Domicilio del contacto.                   |
| `telefonos` | `string` | Teléfonos separados por coma.             |

El archivo JSON debe guardar una lista de contactos:

```json
[
  {
    "id": 1,
    "nombre": "Ada",
    "apellido": "Lovelace",
    "domicilio": "St. James Square 12",
    "telefonos": "+54 381 111-1111, +54 381 111-2222"
  }
]
```

En SQLite se debe usar una tabla `Contactos` con una columna `Id` como clave primaria numérica.

---

## Interfaz requerida

La aplicación debe usar Terminal.Gui y mostrar:

- Una barra de menú superior.
- Una ventana principal con marco visible.
- Un campo de búsqueda.
- Una lista de contactos.
- Un encabezado de columnas.

La lista debe mostrar columnas alineadas para:

| Columna    | Contenido                                |
| ---------- | -----------------------------------------|
| Contacto   | Apellido y nombre visibles del contacto. |
| Domicilio  | Domicilio del contacto.                  |
| Teléfonos  | Teléfonos separados por coma.            |

Los contactos deben ordenarse por `apellido` y luego por `nombre`.

---

## Referencia visual

La interfaz esperada debe parecerse a estas capturas:

![Agenda con diálogo de edición de contacto](imagenes/agenda-editar.svg)

![Agenda con menú Contacto desplegado](imagenes/agenda-menu.svg)

---

## Menú

La barra de menú debe tener al menos:

| Menú                 | Acción                           | Tecla      |
| -------------------- | -------------------------------- | ---------- |
| Agenda -> Guardar    | Guarda los cambios pendientes.   | `Ctrl+S`   |
| Agenda -> Salir      | Cierra la aplicación.            | `Ctrl+Q`   |
| Contacto -> Agregar  | Agrega un contacto nuevo.        | `F2`       |
| Contacto -> Editar   | Edita el contacto seleccionado.  | `Enter`    |
| Contacto -> Borrar   | Borra el contacto seleccionado.  | `Delete`   |

Los marcos de los menús desplegables deben verse correctamente.

---

## Búsqueda

El campo de búsqueda debe filtrar la lista en tiempo real.

La búsqueda debe coincidir contra:

- Nombre.
- Apellido.
- Domicilio.
- Teléfonos.

La comparación debe ignorar mayúsculas y minúsculas.

---

## Alta, edición y baja

### Agregar contacto

Al agregar un contacto se debe abrir un diálogo con:

- Nombre.
- Apellido.
- Domicilio.
- Hasta cuatro teléfonos.

Los teléfonos se ingresan en campos separados y se guardan como un texto separado por coma.

### Editar contacto

Al presionar `Enter` sobre la lista o elegir la opción del menú, se debe abrir el mismo diálogo con los datos del contacto seleccionado.

Al aceptar, se deben actualizar los datos del contacto.

### Borrar contacto

Al presionar `Delete` sobre la lista o elegir la opción del menú, se debe pedir confirmación antes de borrar.

Si la terminal envía `Backspace` para la tecla física de borrado, también se acepta como borrado del contacto seleccionado.

---

## Guardado y cambios pendientes

La aplicación debe trabajar con cambios pendientes en memoria:

- Agregar, editar o borrar un contacto no debe guardar inmediatamente.
- La opción `Agenda -> Guardar` o `Ctrl+S` debe persistir los cambios pendientes.
- Si hay cambios pendientes, el título de la ventana debe mostrar un indicador visual, por ejemplo `*`.
- Al salir con cambios pendientes, se debe preguntar si se desea guardar, salir sin guardar o cancelar.

---

## Validaciones

- No se debe permitir guardar un contacto sin nombre y sin apellido.
- Si un contacto no tiene domicilio, la lista debe mostrar `(sin domicilio)`.
- Si un contacto no tiene teléfonos, la lista debe mostrar `(sin teléfonos)`.
- Si un contacto no tiene nombre visible, la lista debe mostrar `(sin nombre)`.
- Si el archivo de trabajo no existe, la agenda debe empezar vacía.
- Si el archivo JSON está vacío o no se puede leer como lista de contactos, la agenda debe empezar vacía.

---

## Diseño requerido

El programa debe organizarse con funciones locales para las operaciones principales de la interfaz:

```text
1. Resolver archivo de trabajo
2. Crear unidad de trabajo según extensión del archivo
3. Construir interfaz Terminal.Gui
4. Refrescar lista y aplicar búsqueda
5. Agregar contacto
6. Editar contacto seleccionado
7. Borrar contacto seleccionado
8. Guardar cambios pendientes
```

Se sugiere separar la persistencia con estas abstracciones:

```csharp
public interface IEntity {
    int Id { get; set; }
}

public interface IRepository<T> where T : class, IEntity {
    IReadOnlyList<T> ReadAll();
    T? ReadOne(int id);
    void Create(T item);
    void Update(T item);
    void Remove(int id);
}

public interface IUnitOfWork<T> : IRepository<T> where T : class, IEntity {
    bool HasPendingChanges { get; }
    void SaveChanges();
}

public interface IStore<T> where T : class, IEntity {
    IReadOnlyList<T> Load();
    void SaveChanges(IReadOnlyCollection<StoreChange<T>> changes);
}
```

La unidad de trabajo debe manejar el tracking de cambios en memoria. Los stores deben limitarse a cargar entidades y persistir operaciones de inserción, actualización y borrado.

---

## Archivo de prueba

Crear un archivo `agenda.json` en la misma carpeta que `agenda.cs`:

```json
[
  {
    "id": 1,
    "nombre": "Ada",
    "apellido": "Lovelace",
    "domicilio": "St. James Square 12",
    "telefonos": "+54 381 111-1111, +54 381 111-2222"
  },
  {
    "id": 2,
    "nombre": "Alan",
    "apellido": "Turing",
    "domicilio": "Wilmslow Road 42",
    "telefonos": "+54 381 222-2222"
  },
  {
    "id": 3,
    "nombre": "Grace",
    "apellido": "Hopper",
    "domicilio": "Arlington Ave 9",
    "telefonos": "+54 381 333-3333, +54 381 333-4444"
  }
]
```

---

## Casos de prueba mínimos

| Caso                                             | Resultado esperado                                      |
| ------------------------------------------------ | ------------------------------------------------------- |
| `dotnet run agenda.cs` sin `agenda.json`         | Empieza con la agenda vacía.                            |
| `dotnet run agenda.cs -- prueba.json`            | Usa `prueba.json` como archivo de trabajo.              |
| `dotnet run agenda.cs -- prueba.db`              | Usa SQLite y crea la tabla `Contactos` si no existe.    |
| Escribir en búsqueda                             | La lista se filtra en tiempo real.                      |
| Seleccionar contacto y presionar `Enter`         | Abre el diálogo de edición.                             |
| Seleccionar contacto y presionar `Delete`        | Pide confirmación y borra si se acepta.                 |
| Agregar contacto con `F2`                        | Lo agrega, refresca la lista y marca cambios pendientes.|
| Intentar aceptar contacto sin nombre ni apellido | Muestra error y no guarda.                              |
| Presionar `Ctrl+S`                               | Persiste los cambios pendientes.                        |
| Salir con cambios pendientes                     | Pregunta si se desea guardar, descartar o cancelar.     |
| Salir y volver a abrir después de guardar        | Los cambios persisten.                                  |

---

## Entrega

- Archivo `agenda.cs` completo.
- Archivo `agenda.json` con datos de prueba o archivo `agenda.db` generado.

> [!NOTE]
> La entrega se ejecuta con `dotnet run agenda.cs -- [archivo.json|archivo.db]`. Si no se indica archivo, debe trabajar sobre `agenda.json`.
