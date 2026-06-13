# Especificación — Agenda CRUD (Razor Pages + HTMX)

Esta especificación describe el comportamiento esperado del proyecto actual y las decisiones que deben respetarse al modificarlo.

---

## Objetivo

Agenda de contactos con CRUD completo en una pantalla **maestro-detalle**:

- Panel izquierdo: botón de alta, buscador y lista de contactos.
- Panel derecho: detalle de solo lectura, formulario de alta/edición o mensaje de estado.
- Toda la interacción se realiza con **HTMX** devolviendo fragmentos HTML renderizados por Razor.
- No debe haber JavaScript propio ni API JSON.

## Referencia visual

La interfaz debe verse como estas capturas.

### Vista de detalle

![Vista de detalle de un contacto seleccionado](mostrar.png)

### Vista de edición

![Vista de edición de un contacto](editar.png)

## Stack

- **.NET 10**, Razor Pages con handlers inline (`@functions`, sin code-behind).
- **EF Core + SQLite** para persistencia en `agenda.db`.
- **HTMX 2.x servido localmente** desde `wwwroot/js/htmx.min.js`.
- **CSS propio** en `wwwroot/css/site.css`, sin framework de UI.

## Modelo de datos

- `Contact { Id, Nombre, Apellido, Telefono, Email, Notas }`: entidad persistida.
- `ContactInput`: `record` usado por el formulario de alta/edición.
- `ContactListViewModel`: view model con contactos filtrados, selección actual y señales para cargar o limpiar el detalle.
- Un único formulario sirve para alta y edición: `Id == 0` crea; `Id > 0` actualiza.

## Funcionalidad

- La lista carga al iniciar, ordenada por apellido y nombre.
- Si hay contactos disponibles al iniciar, se selecciona automáticamente el primer contacto visible y se muestra su detalle.
- La búsqueda filtra en caliente con debounce de 250 ms sobre nombre, apellido, teléfono, email y notas.
- Si al buscar el contacto seleccionado sigue visible, conserva la selección.
- Si al buscar el contacto seleccionado deja de estar visible y hay resultados, se selecciona automáticamente el primer resultado visible y se carga su detalle.
- Si la búsqueda no tiene resultados, la lista muestra “No hay contactos para esa búsqueda.” y el panel de detalle se limpia.
- Seleccionar un contacto en la lista muestra su detalle de solo lectura.
- “Nuevo contacto” muestra el formulario vacío y limpia la selección de la lista.
- “Editar” muestra el formulario con los datos del contacto sin tocar la lista ni mover su scroll.
- Al guardar un contacto nuevo o editado, vuelve al detalle de ese contacto, refresca la lista y deja seleccionado el contacto guardado.
- Al eliminar, pide confirmación, borra el contacto, muestra “Contacto eliminado.” y refresca la lista.
- Los datos se siembran solo si la base está vacía y persisten entre reinicios.

## Decisiones de diseño

1. **Código modularizado**: `Contact` vive en `Models`, `ContactInput` en `Dtos`, `ContactListViewModel` en `ViewModels`, EF Core en `Data`, el acceso a SQLite en `Repositories` y la lógica de aplicación en `Services`.
2. **Interfaces explícitas**: `ContactRepository` implementa `IContactRepository` y `ContactService` implementa `IContactService`. La inyección de dependencias debe registrar y consumir las interfaces.
3. **Acceso a datos**: usar `ContactRepository` con `IDbContextFactory<AgendaDbContext>` y un contexto efímero por operación. `ContactService` coordina la lógica de crear, editar, listar, buscar y borrar.
4. **Refresco HTMX declarativo y único**: `#master` debe saber cómo recargarse con `hx-trigger="refresh-master from:body"` y `autoSelect=false`. La carga inicial de la lista debe estar separada y mandar `autoSelect=true`. Los handlers que reemplazan `#detail` y necesitan sincronizar la lista deben responder con `HX-Trigger-After-Swap: refresh-master`, para que la recarga ocurra cuando el nuevo detalle ya está en el DOM. No usar `hx-swap-oob` ni refrescos parciales por ítem para este flujo.
5. **Estado compartido mínimo**: la selección actual debe viajar en `#current-selection` como `selectedId`. Los refrescos de `#master` deben incluir `#search-form` y `#current-selection`, no todo el panel `#detail`.
6. **Selección accesible**: la lista usa radios nativos ocultos visualmente y labels clickeables; debe ser navegable por teclado.
7. **Antiforgery activado**: no desactivar la protección. Los `POST` deben incluir `@Html.AntiForgeryToken()` y, cuando corresponda, `hx-include="closest form"`.
8. **HTMX local**: `htmx.min.js` se versiona en `wwwroot/js/`; no usar CDN.
9. **Persistencia real**: `agenda.db`, `agenda.db-shm`, `agenda.db-wal` y journals quedan fuera de Git.
10. **CSS propio y responsive**: mantener variables CSS, layout maestro-detalle, foco visible, soporte mobile y respeto de `prefers-reduced-motion` y `forced-colors`.

## Criterios de aceptación

1. `dotnet build` compila sin errores ni warnings.
2. `dotnet run` crea la base si no existe y siembra datos solo cuando está vacía.
3. Al cargar la app, la lista no queda vacía si hay contactos: se ve el primer contacto seleccionado y su detalle.
4. La búsqueda filtra sin recargar la página y mantiene/actualiza la selección según los resultados visibles.
5. Crear, editar y eliminar funcionan con la lista sincronizada.
6. Después de guardar una edición, el contacto modificado queda seleccionado en la lista.
7. Los `POST` de guardar y borrar no devuelven 400 por antiforgery.
8. HTMX se sirve localmente y no hay errores de consola.
9. No existe JavaScript propio en la solución.
