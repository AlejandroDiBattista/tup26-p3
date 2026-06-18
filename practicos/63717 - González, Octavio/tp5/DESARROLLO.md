# TP5 - Agenda de contactos

## Objetivo y alcance

Se implementó una agenda web de contactos con Blazor Web App, Entity Framework Core y SQLite. La solución permite consultar, buscar, crear, editar y eliminar contactos desde una interfaz maestro/detalle.

## Requisitos interpretados

- Mostrar una lista de contactos cargados desde `contactos.db`.
- Permitir seleccionar un contacto para ver su detalle.
- Permitir alta, edición y eliminación de contactos.
- Incorporar búsqueda por nombre o apellido.
- Respetar los campos del enunciado, incluyendo `FechaNacimiento` como dato opcional.
- Mantener una organización clara entre UI, modelo y persistencia.

## Arquitectura y decisiones

- `Models/Contacto.cs` contiene la entidad persistida y sus validaciones.
- `Data/AgendaDbContext.cs` concentra el `DbContext` de EF Core.
- `Models/Repositorio.cs` encapsula las operaciones de acceso a datos para no mezclar EF con la UI.
- `Components/Pages/Home.razor` implementa el flujo principal maestro/detalle.
- `Models/ContactoEndpoints.cs` expone un API simple de lectura y escritura sobre los contactos.

Se mantuvo una única pantalla principal para reducir complejidad y seguir el esquema pedido por el enunciado. La base SQLite ya incluía datos de ejemplo, por lo que se usó `EnsureCreated()` para abrirla o crearla si no existiera.

## Implementación paso a paso

1. Se corrigió el arranque del proyecto y la resolución de assets estáticos para que la compilación funcionara con el SDK actual.
2. Se separó el contexto de EF del modelo de dominio.
3. Se endureció el repositorio con consultas asincrónicas, `AsNoTracking()` en lecturas y actualización explícita del campo `FechaNacimiento`.
4. Se completó la interfaz principal con:
   - listado maestro,
   - buscador en tiempo real,
   - vista de detalle,
   - formularios de alta y edición,
   - eliminación de contactos,
   - edición de fecha de nacimiento.
5. Se agregó una capa visual más cuidada en `theme.css`.

## Estructura de archivos

- `Program.cs`: registro de servicios, inicialización de base de datos y mapeo de componentes/endpoints.
- `Data/AgendaDbContext.cs`: contexto EF Core.
- `Models/Contacto.cs`: entidad `Contacto` y validaciones.
- `Models/Repositorio.cs`: CRUD sobre SQLite.
- `Models/ContactoEndpoints.cs`: endpoints HTTP.
- `Components/Pages/Home.razor`: pantalla principal de la agenda.
- `wwwroot/css/theme.css`: ajustes visuales globales.

## Validaciones y manejo de errores

- Se valida que `Nombre`, `Apellido`, `Telefono` y `Email` estén presentes.
- Se limita la longitud de los campos de texto para evitar datos inconsistentes.
- `Email` usa validación de formato.
- `FechaNacimiento` es opcional.
- El repositorio devuelve `null` o `false` cuando no encuentra el contacto solicitado.

## Compilar, ejecutar y probar

Compilar:

```bash
dotnet build "practicos/63717 - González, Octavio/tp5/tp5.csproj"
```

Ejecutar:

```bash
dotnet run --project "practicos/63717 - González, Octavio/tp5/tp5.csproj" --urls http://127.0.0.1:5055
```

Pruebas realizadas:

- Compilación exitosa con `dotnet build`.
- Inicio correcto de la app con SQLite inicializado.
- Verificación HTTP de la raíz `/` con respuesta `200`.
- Verificación HTTP del endpoint `/contactos` con respuesta `200` y JSON de contactos.

## Casos de prueba

- Carga inicial de la lista de contactos.
- Búsqueda por texto parcial.
- Acceso al detalle de un contacto.
- Creación y edición desde formulario.
- Eliminación y refresco de la lista.

## Limitaciones y supuestos

- Se asumió que `contactos.db` es la fuente de datos válida del TP y que no hacía falta una estrategia de migraciones.
- La UI prioriza claridad y funcionalidad por sobre una composición visual compleja.
- El API de endpoints existe como apoyo, aunque la experiencia principal está pensada desde la interfaz Blazor.
