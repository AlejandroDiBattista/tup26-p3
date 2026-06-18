# Desarrollo del TP5: AgendaWeb

## Objetivo y alcance

La aplicación administra una agenda de contactos desde una interfaz Blazor Web App. Permite listar, buscar, consultar, crear, editar y eliminar registros persistidos en la base SQLite provista por el enunciado.

El alcance se limita a una agenda local sin autenticación. La base inicial `contactos.db` conserva sus 20 registros de ejemplo.

## Requisitos interpretados

- Persistir los nueve datos del contacto y un identificador automático en SQLite.
- Acceder a la base exclusivamente mediante Entity Framework Core.
- Mostrar los contactos en un panel maestro ordenado por apellido y nombre.
- Filtrar por nombre, apellido, empresa, correo o teléfono.
- Mostrar todos los datos del contacto seleccionado en un panel de detalle.
- Crear y editar contactos con validación de los cuatro campos obligatorios.
- Pedir confirmación antes de eliminar un contacto.
- Separar modelo, acceso a datos, lógica de aplicación y componentes visuales.
- Mantener una interfaz clara, accesible y adaptable a pantallas pequeñas.

## Arquitectura y decisiones de diseño

La solución mantiene el modelo Blazor Web App con renderizado interactivo en el servidor. La página principal coordina el estado, mientras que el acceso a datos queda encapsulado en un servicio.

```text
Home.razor
├── ContactList.razor       listado, selección y búsqueda
├── ContactDetail.razor     consulta y acciones
└── ContactForm.razor       alta y edición validadas
        │
        ▼
ContactoService
        │
        ▼
IDbContextFactory<AgendaContext> ──► contactos.db
```

Se usa `IDbContextFactory<AgendaContext>` en lugar de inyectar un contexto con alcance completo del circuito. Un componente Blazor puede vivir mucho más que una petición HTTP; crear un contexto breve por operación evita mantener entidades rastreadas durante toda la sesión.

Las consultas de lectura usan `AsNoTracking` porque los resultados se muestran y se descartan. Para editar se trabaja con una copia del contacto seleccionado, de modo que cancelar el formulario no altere el detalle visible. La eliminación usa `ExecuteDeleteAsync`, que realiza una única sentencia SQL y permite saber si el registro todavía existía.

La interfaz de escritorio mantiene maestro y detalle en dos columnas. En pantallas de hasta 760 px muestra primero la lista y, después de una selección, cambia al detalle con una acción explícita para volver. Esta variante evita obligar a desplazarse una pantalla completa en teléfonos.

No se incorporó una biblioteca adicional de componentes: Bootstrap ya estaba incluido y los componentes propios son suficientes para este alcance.

## Implementación paso a paso

### 1. Persistencia

`AgendaContext` mapea `Contacto` a la tabla existente `Contactos`, configura la clave autogenerada y los campos obligatorios. `Program.cs` obtiene la cadena `ConnectionStrings:Agenda`, registra la fábrica de contextos y conserva el pipeline estándar de Blazor.

El proyecto fija el destino de los recursos de `wwwroot` en el archivo `.csproj`. Esto evita que MSBuild interprete la coma del nombre del directorio del alumno como un separador al calcular rutas de recursos estáticos.

### 2. Lógica de agenda

`ContactoService` expone operaciones asíncronas para:

- listar y filtrar contactos;
- obtener un registro por identificador;
- crear y recuperar el identificador generado;
- actualizar solo si el registro aún existe;
- eliminar e informar si se quitó una fila.

Antes de guardar se eliminan espacios accidentales al inicio y al final de los textos.

### 3. Listado y búsqueda

`ContactList` muestra cantidad de resultados, iniciales, nombre, empresa o teléfono. El buscador actualiza el listado al escribir y ofrece estados explícitos de carga y de búsqueda sin resultados.

La consulta se ordena siempre por apellido y nombre. SQLite resuelve el filtro mediante `LIKE` sobre los cinco campos más útiles para localizar una persona.

### 4. Detalle

`ContactDetail` presenta teléfono y correo como enlaces accionables, información laboral, fecha de nacimiento, dirección y notas. Los datos opcionales vacíos se muestran como “Sin especificar”.

### 5. Alta y edición

`ContactForm` usa `EditForm`, `DataAnnotationsValidator`, resumen de errores y mensajes junto a cada campo. El modelo declara límites de longitud y valida:

- nombre obligatorio;
- apellido obligatorio;
- teléfono obligatorio y con formato telefónico;
- correo obligatorio y con formato de correo.

Los controles incluyen etiquetas visibles, tipos de entrada y atributos de autocompletado apropiados. El botón de envío evita una segunda operación mientras el guardado está en curso.

### 6. Eliminación y errores

La eliminación requiere confirmar en un diálogo que identifica el contacto. Las operaciones muestran mensajes de éxito o error y controlan el caso en que otro proceso haya eliminado previamente el registro.

## Estructura de archivos

| Archivo | Responsabilidad |
|---|---|
| `Models/Contacto.cs` | Entidad y reglas de validación. |
| `Data/AgendaContext.cs` | Mapeo de Entity Framework Core. |
| `Services/ContactoService.cs` | Consultas y operaciones CRUD. |
| `Components/Pages/Home.razor` | Estado y coordinación de la pantalla. |
| `Components/Contacts/ContactList.razor` | Panel maestro y buscador. |
| `Components/Contacts/ContactDetail.razor` | Panel de consulta. |
| `Components/Contacts/ContactForm.razor` | Formulario reutilizable. |
| `wwwroot/app.css` y estilos `.razor.css` | Base visual y estilos aislados. |
| `Program.cs` | Registro de servicios y pipeline HTTP. |
| `appsettings.json` | Cadena de conexión. |

## Validaciones y manejo de errores

- Los campos obligatorios no aceptan cadenas vacías.
- El correo y el teléfono se validan antes de enviar.
- Cada texto tiene una longitud máxima para evitar entradas desproporcionadas.
- La fecha de nacimiento no permite seleccionar una fecha futura desde el control.
- Crear, actualizar y eliminar usan métodos asíncronos.
- Editar o eliminar un registro inexistente produce un mensaje comprensible.
- Los errores de persistencia conservan el formulario para poder reintentar.
- Eliminar requiere confirmación y no se ejecuta desde una navegación GET.

## Compilar y ejecutar

Requisitos: SDK de .NET 10.

Desde este directorio:

```bash
dotnet restore
dotnet build --no-restore
dotnet run
```

Abrir la dirección informada por la consola, normalmente `http://localhost:5276` o `https://localhost:7199`.

La aplicación busca `contactos.db` en el directorio del proyecto. Para probar con una copia puede sobrescribirse la conexión sin cambiar archivos:

```bash
ConnectionStrings__Agenda="Data Source=/ruta/a/copia.db" dotnet run
```

## Casos de prueba y resultados

| Caso | Resultado |
|---|---|
| Compilación .NET 10 | Correcta, 0 advertencias y 0 errores. |
| Base inicial | Se recuperaron los 20 contactos. |
| Búsqueda por empresa `Tech` | Se obtuvo únicamente Bruno Benítez. |
| Consulta de detalle | Se mostraron los nueve datos almacenados. |
| Envío vacío del alta | Se mostraron las cuatro validaciones obligatorias. |
| Crear contacto sobre copia de la base | Registro creado y textos normalizados. |
| Editar el contacto creado | Cargo actualizado y recuperado desde SQLite. |
| Eliminar el contacto creado | Registro eliminado; la cantidad volvió a 20. |
| Navegador de escritorio | Maestro/detalle, formulario y estados renderizados correctamente. |
| Navegador de 390 × 844 px | Lista y detalle navegables en pantallas separadas con acción para volver. |
| Consola del navegador | Sin errores durante los flujos verificados. |

Las operaciones mutables se ejecutaron contra copias temporales de `contactos.db`; la base entregada no fue alterada.

## Limitaciones y supuestos

- La agenda es local y no implementa usuarios, autenticación ni permisos.
- No se define un token de concurrencia; se controla la desaparición del registro, pero dos ediciones simultáneas aplican “último guardado gana”.
- El conjunto inicial es pequeño y no requiere paginación ni virtualización.
- Bootstrap y Bootstrap Icons se cargan desde CDN; sin conexión la aplicación sigue funcionando, pero pierde esos estilos e íconos externos.
- La búsqueda consulta al escribir. Para una base considerablemente mayor convendría agregar espera breve, paginación e índices específicos.
