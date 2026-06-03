# Especificación — Agenda CRUD (Razor Pages + HTMX)

Pautas para generar la aplicación. Describe el **qué** y las **decisiones no obvias**; el **cómo** (estructura concreta de vistas, atributos HTMX, reglas CSS) queda a criterio del agente según buenas prácticas de Razor Pages y HTMX.

---

## Objetivo

Agenda de contactos con CRUD completo (listar, buscar, ver, crear, editar, borrar) en una pantalla **maestro-detalle**: lista a la izquierda, panel de detalle/formulario a la derecha. Toda la interacción es con **HTMX** devolviendo fragmentos de HTML; **sin JavaScript propio** ni API JSON.

## Stack

- **.NET 10**, Razor Pages con **handlers inline** (`@functions`, sin code-behind).
- **EF Core + SQLite** para persistencia (base `agenda.db`).
- **HTMX 2.x servido localmente** desde `wwwroot/js/` (no CDN).
- **CSS propio**, sin framework de UI.

## Modelo de datos

- `Contact { Id, Nombre, Apellido, Telefono, Email, Notas }` — entidad persistida.
- `ContactInput` — record con los mismos campos, usado en los formularios.
- Un **único DTO/formulario** sirve para alta y edición: `Id == 0` significa contacto nuevo; `Id > 0`, edición.

## Funcionalidad

- Listar contactos ordenados por apellido, con **búsqueda en caliente** (debounce ~250 ms) sobre todos los campos de texto (usar `EF.Functions.Like`).
- Ver detalle en modo solo lectura.
- Crear, editar y eliminar (con confirmación) contactos.
- Tras crear/editar/eliminar, la **lista se refresca sola**.
- Pre-sembrar datos de ejemplo solo si la base está vacía.

## Decisiones de diseño (respetar)

Estas son las elecciones que conviene fijar porque un agente podría resolverlas de otra forma menos deseable:

1. **Acceso a datos**: encapsularlo en un único servicio (p. ej. `ContactStore`) que use `IDbContextFactory` y abra un contexto efímero por operación.
2. **Refresco de la lista por evento, no por out-of-band swap**: el panel de la lista escucha un evento HTMX (p. ej. `contactsChanged`); los handlers que modifican datos lo disparan devolviendo el header **`HX-Trigger`**. Evitar `hx-swap-oob`.
3. **Antiforgery activado**: no desactivarlo. Los `POST` envían el token incluyéndolo desde el formulario con `hx-include`.
4. **Selección de la lista accesible**: navegable por teclado (p. ej. inputs `radio` con sus labels), no solo clic.
5. **Persistencia real**: los datos sobreviven al reinicio; `agenda.db` y sus archivos `-shm`/`-wal` van en `.gitignore`. El htmx local **sí** se versiona.
6. **CSS responsable**: variables para el tema, layout maestro-detalle responsive, foco visible y respeto de `prefers-reduced-motion`. Nombres de clase planos y descriptivos (sin BEM).

## Criterios de aceptación

1. `dotnet build` compila sin errores ni warnings; `dotnet run` crea y siembra la base.
2. La lista carga y filtra en tiempo real sin recargar la página.
3. Seleccionar un contacto muestra su detalle (solo lectura, no formulario).
4. Crear, editar y eliminar funcionan, y la lista queda sincronizada en cada caso.
5. Los `POST` no devuelven 400 (token antiforgery correcto).
6. HTMX se sirve local (sin peticiones a CDNs) y no hay errores en consola.
7. Los datos persisten entre reinicios.
8. No existe JavaScript propio en la solución.
