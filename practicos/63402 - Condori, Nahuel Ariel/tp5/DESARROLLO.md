# Desarrollo del TP5: AgendaWeb

## Objetivo y alcance

La aplicación administra una agenda de contactos desde una interfaz web Blazor. Permite crear, consultar, editar, eliminar y buscar contactos, y persiste los cambios en la base SQLite `contactos.db` mediante Entity Framework Core.

El alcance incluye el modelo completo solicitado, una vista maestro/detalle responsiva, validación de entradas, confirmación antes de eliminar, manejo de errores y pruebas automatizadas de la capa de aplicación.

## Requisitos interpretados

- Mostrar los 20 contactos iniciales almacenados en SQLite.
- Seleccionar un contacto desde el panel maestro y mostrar todos sus datos en el panel de detalle.
- Filtrar por nombre, apellido, teléfono, correo o empresa.
- Crear contactos con nombre, apellido, teléfono y correo obligatorios.
- Modificar cualquier campo de un contacto existente.
- Eliminar un contacto únicamente después de una confirmación.
- Mantener separados modelo, persistencia, lógica y componentes de interfaz.
- Adaptar la interfaz a escritorio, tableta y teléfono.

## Arquitectura y decisiones

La solución usa una Blazor Web App con renderizado interactivo en el servidor. Este modo mantiene la lógica y el acceso a datos en .NET sin trasladar la base ni secretos al navegador.

`AgendaContext` se registra con `AddDbContextFactory`. En Blazor Server, un circuito puede vivir más que una petición HTTP; crear un contexto corto por operación evita conservar un `DbContext` rastreando entidades durante todo ese circuito.

`IContactoService` define el contrato de aplicación y `ContactoService` concentra consultas, orden, normalización, validación y persistencia. Los componentes no acceden directamente a EF Core.

La conexión se lee desde `ConnectionStrings:Agenda`. Se mantuvo el archivo SQLite provisto en vez de reemplazarlo por datos en memoria o migraciones, porque forma parte del punto de partida y contiene los registros de ejemplo.

El formulario se extrajo a `ContactForm.razor` para separar edición y validación de la coordinación maestro/detalle. Se descartó crear una página distinta por operación porque el enunciado pide explícitamente una experiencia maestro/detalle y un diálogo conserva el contexto de selección.

Los formularios usan anotaciones de datos tanto en Blazor como en el servicio. La segunda validación protege el contrato aunque el servicio sea llamado desde otro componente.

## Implementación paso a paso

1. Se completaron las anotaciones de `Contacto`: campos obligatorios, correo válido y longitudes máximas.
2. Se configuró `AgendaContext` y la fábrica de contextos con la cadena de conexión de `appsettings.json`.
3. Se implementó el servicio CRUD. Antes de guardar se recortan espacios; al actualizar se copia únicamente información editable sobre una entidad existente.
4. Se construyó la página maestro/detalle. La selección se conserva al recargar y se limpia si el contacto deja de estar en el filtro.
5. La búsqueda espera 250 ms y cancela la consulta anterior. Esto evita acceder a SQLite por cada pulsación rápida y previene resultados fuera de orden.
6. Se implementó `ContactForm` con `EditForm`, `DataAnnotationsValidator`, etiquetas asociadas, autofill y mensajes por campo.
7. Alta y edición comparten el formulario. El modelo se copia antes de editar para que cancelar no cambie la ficha visible.
8. La eliminación usa confirmación y bloquea acciones repetidas mientras se persiste.
9. Los diálogos HTML nativos administran capa superior, tecla Escape y foco. La pequeña función de `dialogs.js` llama `showModal` desde Blazor.
10. Se agregaron estados de carga, errores comprensibles, registro técnico con `ILogger` y estilos responsivos con foco visible y movimiento reducido.

## Estructura de archivos

- `Models/Contacto.cs`: entidad y reglas de validación.
- `Models/Data/agenda.cs`: contexto EF Core y colección `Contactos`.
- `Models/Services/IContactoService.cs`: contrato CRUD y de búsqueda.
- `Models/Services/contactoservice.cs`: lógica de aplicación y persistencia.
- `Components/Pages/Home.razor`: coordinación maestro/detalle y estados de interfaz.
- `Components/ContactForm.razor`: formulario reutilizable de alta y edición.
- `wwwroot/app.css`: diseño, estados, responsividad y accesibilidad visual.
- `wwwroot/dialogs.js`: apertura de diálogos nativos desde Blazor.
- `tp5.Tests/ContactoServiceTests.cs`: pruebas aisladas sobre SQLite temporal.
- `contactos.db`: base entregada con los contactos iniciales.

## Algoritmos y persistencia

La consulta parte de `AsNoTracking`, aplica el filtro sobre cinco campos cuando existe texto y ordena por apellido y nombre. `AsNoTracking` es adecuado porque los objetos de la lista solo se muestran; cada modificación abre otro contexto y recupera la entidad por identificador.

En el alta, SQLite genera `Id`. En la modificación, un identificador inexistente produce `KeyNotFoundException` en lugar de aparentar éxito. En la baja, no encontrar el registro se considera una operación idempotente y no genera un segundo error.

Cada prueba crea un archivo SQLite único en la carpeta temporal del sistema, genera el esquema con `EnsureCreated` y elimina el archivo al finalizar. La base `contactos.db` nunca se modifica durante las pruebas.

## Validaciones y errores

- Nombre, apellido, teléfono y correo no admiten vacío ni espacios solamente.
- El correo debe tener un formato válido.
- Todos los textos tienen límites acordes al tipo de dato.
- Los espacios exteriores se eliminan antes de guardar.
- El formulario impide un segundo envío durante la persistencia.
- La eliminación requiere confirmación y también evita dobles clics.
- Los errores de base se registran con detalle en el servidor y se muestran al usuario con un mensaje breve.
- Las búsquedas anteriores se cancelan cuando cambia el texto.

## Compilar, ejecutar y probar

Requiere el SDK de .NET 10. Desde esta carpeta:

```bash
dotnet restore tp5.slnx
dotnet build tp5.slnx --no-restore
dotnet run --project tp5.csproj
```

La configuración de desarrollo publica normalmente en `http://localhost:5276`. Para ejecutar las pruebas:

```bash
dotnet test tp5.Tests/tp5.Tests.csproj --no-restore
```

## Casos verificados

- Compilación completa: correcta, sin errores ni advertencias.
- CRUD automatizado: alta, consulta, modificación y baja correctas.
- Búsqueda: filtra por texto y ordena por apellido y nombre.
- Normalización: elimina espacios exteriores antes de persistir.
- Validación: rechaza un nombre compuesto solo por espacios.
- Modificación inexistente: informa el error esperado.
- Interfaz en escritorio y móvil: lista, detalle, formulario, búsqueda y confirmación operativos.
- Resultado automatizado: 4 pruebas superadas, 0 fallidas.

## Supuestos y limitaciones

- La aplicación es académica y de un único usuario; no implementa autenticación ni control de concurrencia multiusuario.
- La búsqueda usa las reglas de comparación de SQLite. Las coincidencias ASCII no distinguen mayúsculas, pero el tratamiento de algunos caracteres acentuados depende de la intercalación disponible.
- La estructura ya existe en `contactos.db`; no se agregaron migraciones porque el enunciado entrega esa base como parte del proyecto.
- Bootstrap y Bootstrap Icons se cargan desde CDN, por lo que su apariencia completa requiere acceso de red en el navegador.
