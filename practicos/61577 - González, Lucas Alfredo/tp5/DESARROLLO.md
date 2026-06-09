# Desarrollo del TP5: AgendaWeb

## 1. Objetivo y alcance

El trabajo implementa una agenda web completa sobre el proyecto Blazor provisto.
La aplicación permite consultar, buscar, crear, editar y eliminar contactos
persistidos en `contactos.db` mediante Entity Framework Core y SQLite.

La solución conserva la base con los 20 registros iniciales y organiza la
interfaz como maestro/detalle:

- El panel izquierdo muestra y filtra la colección.
- El panel derecho presenta el detalle, el formulario o la confirmación de
  eliminación según la acción actual.

No se agregaron servicios externos ni dependencias distintas de las incluidas
en el proyecto inicial.

## 2. Requisitos interpretados

Los requisitos del enunciado se transformaron en las siguientes funciones
verificables:

1. Definir un `DbContext` que represente la tabla `Contactos`.
2. Conectarse al archivo SQLite entregado con el TP.
3. Mostrar los contactos ordenados alfabéticamente.
4. Seleccionar un contacto y visualizar todos sus datos.
5. Buscar por nombre, apellido, teléfono, correo, empresa o cargo.
6. Crear contactos con validación de campos obligatorios.
7. Editar registros sin modificar el original hasta guardar.
8. Confirmar explícitamente antes de eliminar.
9. Mantener una estructura clara entre datos, lógica e interfaz.
10. Adaptar la aplicación a escritorio, tablet y teléfono.

## 3. Arquitectura y responsabilidades

La solución utiliza una separación sencilla en cuatro capas.

### 3.1. Modelo

`Models/Contacto.cs` representa una fila de la tabla. Sus propiedades incluyen
anotaciones como `Required`, `StringLength`, `EmailAddress` y `Phone`.

Estas anotaciones se reutilizan en dos niveles:

- Documentan las restricciones esperadas por la aplicación.
- Alimentan la validación del formulario Blazor mediante
  `DataAnnotationsValidator`.

Los campos opcionales se representan como cadenas vacías porque la base inicial
los define como `NOT NULL`. La fecha de nacimiento utiliza `DateOnly?` porque
puede no informarse y no necesita hora.

### 3.2. Acceso a datos

`Data/AgendaDbContext.cs` expone `DbSet<Contacto>` y vincula explícitamente la
entidad con la tabla `Contactos`.

Se eligió `IDbContextFactory<AgendaDbContext>` en lugar de inyectar un contexto
scoped directamente. Un componente Blazor interactivo puede permanecer activo
durante muchas operaciones y `DbContext` no está diseñado para compartir ese
tiempo de vida ni para ejecutar acciones concurrentes. La fábrica crea un
contexto breve por operación y lo descarta inmediatamente.

La cadena SQLite se construye desde `ContentRootPath`. Así, `contactos.db`
siempre se resuelve dentro del TP, incluso al ejecutar:

```bash
dotnet run --project "/ruta/al/tp5/tp5.csproj"
```

### 3.3. Lógica de aplicación

`Services/AgendaService.cs` concentra las operaciones:

- `BuscarAsync`: consulta y filtra los campos principales.
- `ObtenerAsync`: obtiene un registro sin seguimiento.
- `CrearAsync`: normaliza y agrega una entidad.
- `ActualizarAsync`: recupera la entidad persistida y copia solo campos
  editables.
- `EliminarAsync`: ejecuta un borrado directo por identificador.

Las lecturas usan `AsNoTracking` porque sus resultados solo se presentan en la
interfaz. Esto reduce el estado mantenido por Entity Framework.

La búsqueda se traduce a SQL con `EF.Functions.Like`, evitando cargar los 20
registros para recién después filtrar. El orden final utiliza la cultura
`es-AR`; SQLite ordena por código Unicode y ubicaría apellidos con `Á` fuera de
su posición alfabética esperada.

Antes de guardar se eliminan espacios al principio y al final. Para actualizar
no se adjunta directamente el objeto recibido desde el formulario: se consulta
la fila existente y se copian sus campos. Esta decisión evita confiar en un
estado de seguimiento originado en la interfaz.

### 3.4. Interfaz

`Components/Pages/Home.razor` coordina el estado general:

- Colección filtrada.
- Contacto seleccionado.
- Copia en edición.
- Modo actual del panel derecho.
- Mensajes de éxito y error.
- Cancelación de búsquedas reemplazadas.

La búsqueda aplica un retraso de 250 ms. Si el usuario sigue escribiendo, se
cancela la consulta pendiente y solo se ejecuta la más reciente.

Los componentes visuales se dividieron por responsabilidad:

| Componente | Responsabilidad |
|---|---|
| `ListaContactos.razor` | Representar el maestro y notificar la selección. |
| `DetalleContacto.razor` | Mostrar todos los datos y acciones disponibles. |
| `ContactoFormulario.razor` | Reutilizar el mismo formulario para alta y edición. |
| `ConfirmarEliminacion.razor` | Evitar borrados accidentales. |

La edición utiliza una copia campo por campo. Presionar **Cancelar** descarta
esa copia y mantiene intactos tanto el detalle como la base.

## 4. Implementación paso a paso

### Paso 1: configurar persistencia

Se inspeccionó el esquema existente de `contactos.db`, se creó
`AgendaDbContext`, se agregaron restricciones al modelo y se registró la fábrica
de contextos en `Program.cs`.

También se declaró explícitamente el espacio de nombres de los componentes para
estabilizar la compilación Razor.

### Paso 2: implementar casos de uso

Se creó `AgendaService` para separar Entity Framework de la UI. Primero se
implementaron las consultas y luego las tres mutaciones CRUD. Todos los métodos
son asíncronos y aceptan cancelación cuando corresponde.

### Paso 3: construir maestro/detalle

Se reemplazó la pantalla inicial por la agenda. La lista se implementó con
botones reales para admitir foco de teclado, Enter y barra espaciadora. El
detalle utiliza una lista de descripción `dl`, apropiada para pares etiqueta y
valor.

Los correos y teléfonos son enlaces `mailto:` y `tel:` respectivamente.

### Paso 4: agregar mutaciones

Se incorporó un formulario semántico dividido en `fieldset`:

- Datos personales.
- Información profesional.
- Información adicional.

Los campos requeridos muestran mensajes en español. El botón de guardado se
bloquea durante la operación para impedir envíos duplicados. La eliminación
requiere una pantalla de confirmación independiente.

### Paso 5: diseño y accesibilidad

`wwwroot/app.css` define un sistema visual con variables para color, radios,
sombras y tipografía. Se priorizaron:

- Contraste de texto y bordes.
- Foco visible con una señal que no depende solo del color.
- Controles de al menos 48 píxeles en dispositivos táctiles.
- Etiquetas visibles sobre cada entrada.
- Estados comunicados con texto e iconos.
- Compatibilidad con `prefers-reduced-motion`.
- Paso a una columna por debajo de 56 rem.
- Acciones de ancho completo en teléfonos.

Los campos incluyen tipos, `inputmode` y valores `autocomplete` adecuados para
correo, teléfono, nombre, empresa y dirección.

### Paso 6: compatibilidad con la ruta reglamentaria

La versión 10.0.201 del SDK presenta un problema al calcular rutas de contenido
cuando el directorio absoluto contiene una coma, como ocurre en:

```text
61577 - González, Lucas Alfredo
```

El proyecto declara `TargetPath` para `wwwroot/**/*`. Con esta configuración
MSBuild no ejecuta el cálculo defectuoso y `dotnet build` funciona sin mover ni
renombrar la carpeta.

## 5. Validaciones y manejo de errores

- Los campos nombre, apellido, teléfono y correo son obligatorios.
- El correo debe tener formato válido.
- El teléfono se valida como número telefónico y se almacena como texto para
  conservar prefijos, espacios y guiones.
- Cada campo limita su longitud para impedir datos desproporcionados.
- Las operaciones muestran mensajes de éxito y errores comprensibles.
- Actualizar o eliminar una fila inexistente devuelve un resultado controlado.
- El formulario se deshabilita durante guardado o eliminación.
- Las búsquedas reemplazadas se cancelan sin mostrarse como error.

## 6. Compilación y ejecución

Desde la carpeta `tp5`:

```bash
dotnet restore
dotnet build --no-restore
dotnet run
```

La consola informa la URL local, normalmente:

```text
http://localhost:5276
```

La base `contactos.db` debe permanecer junto a `tp5.csproj`.

## 7. Pruebas realizadas

### Compilación

```bash
dotnet restore
dotnet build --no-restore
```

Resultado:

- Compilación correcta.
- Cero errores.
- Cero advertencias.

### Base inicial

Se verificó con SQLite:

- Existencia de la tabla `Contactos`.
- Correspondencia de sus diez columnas con el modelo.
- Presencia de 20 contactos iniciales.

### Pruebas funcionales en navegador

Se ejecutó el flujo sobre una copia temporal de la base:

1. Carga de los 20 contactos.
2. Orden alfabético correcto para apellidos con acentos.
3. Búsqueda por empresa y actualización automática del detalle.
4. Rechazo de un alta vacía con los cuatro mensajes obligatorios.
5. Creación de un contacto de prueba.
6. Edición de su cargo.
7. Confirmación y eliminación del mismo contacto.
8. Retorno de la colección a 20 registros.
9. Revisión visual en escritorio.
10. Revisión responsiva con viewport de 390 x 844 píxeles.

La prueba se realizó sobre una copia para no alterar los datos entregados.

## 8. Limitaciones y supuestos

- La aplicación es académica y no implementa autenticación ni autorización.
- La búsqueda usa `LIKE`; para miles de contactos sería conveniente agregar
  índices y paginación.
- No se agregaron migraciones porque la base y la tabla ya fueron entregadas.
- Los enlaces de Bootstrap y Bootstrap Icons se cargan desde CDN, tal como
  estaba configurado el proyecto inicial, por lo que requieren conexión a
  Internet para mostrar esos recursos.
