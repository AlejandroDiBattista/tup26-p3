# Tutorial: Aplicaciones web con Razor Pages, HTMX y EF Core

## El problema que resuelve HTMX

Cuando querés que una página web actualice parte de su contenido sin recargar todo, históricamente había dos caminos:

**Camino A — JavaScript en el cliente.** Usás React, Vue, Angular o vanilla JS. El navegador descarga un bundle, hace fetch a una API, recibe JSON, y vos escribís código para convertir ese JSON en HTML.

**Camino B — Recarga completa.** Cada acción manda un form, el servidor devuelve una página entera, el navegador la descarga y muestra todo de nuevo.

HTMX propone un tercer camino: el servidor sigue siendo quien renderiza HTML, pero el navegador puede pedir solo un fragmento y reemplazarlo en la página, sin recargar. La diferencia con el Camino A es que no escribís ningún código JavaScript. La diferencia con el Camino B es que no recargás la página entera.

La apuesta de HTMX es que el servidor ya sabe cómo hacer HTML. En lugar de duplicar esa lógica en el cliente con JSON y componentes, extendés el HTML con atributos que le dicen al navegador cuándo y cómo pedir fragmentos nuevos.

## Conceptos fundamentales de HTMX

HTMX funciona mediante atributos en el HTML. Los más importantes son:

**`hx-get` / `hx-post`**: hacen un GET o POST cuando ocurre algo.

```html
<button hx-get="/Agenda?handler=List">
    Cargar contactos
</button>
```

Cuando el usuario hace clic, HTMX hace un GET a esa URL y usa la respuesta.

**`hx-target`**: dónde poner la respuesta.

```html
<button
    hx-get="/Agenda?handler=List"
    hx-target="#master">
    Cargar contactos
</button>
```

La respuesta del servidor va dentro del elemento con `id="master"`.

**`hx-swap`**: cómo reemplazar el contenido. El valor por defecto es `innerHTML`, que reemplaza el contenido interno del target.

**`hx-trigger`**: cuándo disparar la request. Por defecto es el evento natural del elemento (clic para botón, submit para form). Pero podés cambiarlo:

```html
<div
    hx-get="/Agenda?handler=List"
    hx-trigger="load">
</div>
```

Esto hace el GET automáticamente cuando el elemento se carga en el DOM.

**`hx-include`**: incluir campos adicionales en la request. Necesario cuando querés mandar datos desde un elemento que no es el form directo.

```html
<button
    hx-post="/Agenda?handler=Delete&id=3"
    hx-include="closest form">
    Eliminar
</button>
```

El selector `closest form` le dice a HTMX que incluya los campos del formulario más cercano en el árbol DOM.

## Razor Pages: cómo funciona el modelo de páginas

Razor Pages es el sistema de ASP.NET Core que asocia un archivo `.cshtml` (la vista) con su lógica de servidor. Es diferente de MVC porque la lógica vive junto a la página, no en un controlador separado.

Una página Razor tiene dos características principales:

**1. La directiva `@page`** convierte al archivo en un endpoint HTTP. Sin ella, es solo una vista parcial.

**2. Los handlers** son métodos con nombres que siguen la convención `On{Verbo}{Nombre}`:
- `OnGet` responde a GET sin nombre específico
- `OnGetList` responde a `?handler=List`
- `OnPostSave` responde a POST con `?handler=Save`

La convención es fundamental: ASP.NET enruta automáticamente a los métodos según el verbo HTTP y el parámetro `handler` en la URL.

En esta app, toda la lógica de la página Agenda vive dentro del bloque `@functions` en `Pages/Agenda/Index.cshtml`:

```cshtml
@page
@inject ContactStore ContactStore

@functions {
    public IActionResult OnGet() {
        return Page();
    }

    public IActionResult OnGetList(string? search) {
        return Partial("_ContactList", ContactStore.GetAll(search));
    }

    public IActionResult OnPostSave(ContactInput input) {
        Contact savedContact = ContactStore.Save(input);
        return Partial("_ContactDetail", savedContact);
    }
}
```

Cada método retorna un `IActionResult`. Los más usados en esta app son:
- `Page()` → renderiza la página completa
- `Partial("_NombreDelPartial", modelo)` → renderiza solo esa vista parcial
- `Content("html aquí", "text/html")` → devuelve HTML literal
- `NotFound()` → devuelve 404

## Partial Views: fragmentos reutilizables

Un partial view es un archivo `.cshtml` que no tiene `@page`, solo el marcado de un fragmento. Se renderiza desde otro lugar y devuelve solo ese fragmento, no una página completa.

El nombre empieza con guión bajo por convención: `_ContactList.cshtml`, `_ContactDetail.cshtml`.

Un partial puede recibir un modelo tipado con `@model`. Eso garantiza que el compilador sabe qué propiedades están disponibles en la vista.

```cshtml
@model List<Contact>

@foreach (Contact contact in Model)
{
    <button
        hx-get="/Agenda?handler=Detail&id=@contact.Id"
        hx-target="#detail"
        hx-swap="innerHTML">
        @contact.Apellido, @contact.Nombre
    </button>
}
```

Cuando HTMX pide `/Agenda?handler=List`, el servidor renderiza `_ContactList.cshtml` con la lista de contactos y devuelve solo ese HTML. HTMX lo pone dentro de `#master`.

## Cómo se construye el patrón maestro-detalle

El patrón tiene dos zonas en la página: una lista a la izquierda y un panel de detalle a la derecha. El objetivo es que al seleccionar algo en la lista, solo cambie el panel derecho, sin tocar nada más.

La estructura base es esta:

```html
<div class="agenda">
    <section class="panel">
        <div id="master"
             hx-get="/Agenda?handler=List"
             hx-trigger="load"
             hx-swap="innerHTML">
        </div>
    </section>

    <section class="panel">
        <div id="detail">
            <p>Seleccioná un contacto.</p>
        </div>
    </section>
</div>
```

El `div#master` se auto-carga al llegar al DOM (`hx-trigger="load"`) pidiendo la lista. Cada ítem de la lista apunta a `#detail` como target:

```html
<button
    hx-get="/Agenda?handler=Detail&id=@contact.Id"
    hx-target="#detail"
    hx-swap="innerHTML">
    @contact.Apellido, @contact.Nombre
</button>
```

Cuando el usuario hace clic, HTMX reemplaza el contenido de `#detail` con el HTML del detalle. El panel izquierdo no se toca.

## El ciclo completo de una acción

Tomemos el borrado como ejemplo porque involucra todos los conceptos juntos: POST, antiforgery, token, HTMX.

**Paso 1: el servidor renderiza el detalle del contacto.**

El handler `OnGetDetail` devuelve el partial `_ContactDetail`, que incluye:

```cshtml
<form class="actions" hx-target="#detail" hx-swap="innerHTML">
    @Html.AntiForgeryToken()

    <button
        type="button"
        class="danger"
        hx-post="/Agenda?handler=Delete&id=@Model.Id"
        hx-include="closest form"
        hx-confirm="¿Eliminar este contacto?">
        Eliminar
    </button>
</form>
```

`@Html.AntiForgeryToken()` genera un campo oculto con un token criptográfico que el servidor emite y puede verificar. Ese token llega al navegador como parte del HTML.

**Paso 2: el usuario hace clic en Eliminar.**

HTMX detecta el clic, muestra el confirm, y arma un POST a `/Agenda?handler=Delete&id=3`. Gracias a `hx-include="closest form"`, incluye también el token antiforgery del form.

**Paso 3: el servidor valida y ejecuta.**

Antes de ejecutar `OnPostDelete`, ASP.NET verifica el token. Si es válido, borra el contacto y devuelve HTML con el mensaje de confirmación y un disparador para recargar la lista.

**Paso 4: HTMX actualiza la UI.**

La respuesta reemplaza `#detail`. El HTML de la respuesta incluye un div con `hx-trigger="load"` que recarga la lista automáticamente.

## Por qué existe antiforgery

Supongamos que no hay ninguna protección. Un atacante podría crear una página maliciosa con un form oculto que apunta a tu app:

```html
<!-- Página del atacante -->
<form action="https://tuapp.com/Agenda?handler=Delete&id=1" method="post">
    <input type="hidden" name="algo" value="algo" />
</form>
<script>document.forms[0].submit();</script>
```

Si tenés una sesión abierta en tu app y visitás esa página, el navegador manda el POST con tus cookies de sesión. El servidor lo recibe y borra el contacto porque no puede distinguir si vino de tu app o del sitio malicioso.

Antiforgery corta esto: el servidor emite un token secreto que el sitio malicioso no conoce. Cuando llega el POST sin el token correcto, el servidor lo rechaza con 400.

Razor Pages lo valida automáticamente en todos los POST. Solo tenés que asegurarte de incluir el token en las requests.

## Entity Framework Core: la capa de datos

EF Core es un ORM que traduce entre objetos C# y filas en una base de datos. En lugar de escribir SQL, trabajás con clases y LINQ. EF Core genera el SQL por vos.

Los tres componentes básicos son:

**La entidad**: una clase C# que representa una tabla.

```csharp
public class Contact {
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
}
```

**El DbContext**: la clase que representa la sesión con la base de datos y expone las tablas como propiedades.

```csharp
public class AgendaDbContext(DbContextOptions<AgendaDbContext> options)
    : DbContext(options) {

    public DbSet<Contact> Contacts => Set<Contact>();
}
```

**La configuración**: en `Program.cs` le decís a ASP.NET qué proveedor usar y dónde está la base de datos.

```csharp
builder.Services.AddDbContextFactory<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=agenda.db")
);
```

SQLite guarda todo en un archivo. En producción se cambiaría a SQL Server o PostgreSQL solo cambiando el paquete y la cadena de conexión, sin tocar el código de la app.

## La fachada ContactStore

Un patrón útil es separar la lógica de acceso a datos en una clase propia. Esto desacopla los handlers de EF Core directamente y hace más fácil cambiar el almacenamiento después.

En `Program.cs` el `ContactStore` usa `IDbContextFactory<AgendaDbContext>` en lugar de un `DbContext` directamente. Esto es importante: en apps web, el DbContext tiene un ciclo de vida por request. La factory permite crear y descartar instancias manualmente con `using`, lo cual es más seguro en servicios con ciclo de vida no controlado:

```csharp
public List<Contact> GetAll(string? search = null) {
    using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();

    IQueryable<Contact> query = dbContext.Contacts
        .OrderBy(contact => contact.Apellido)
        .ThenBy(contact => contact.Nombre);

    if (!string.IsNullOrWhiteSpace(search)) {
        string term = search.Trim();
        query = query.Where(contact =>
            EF.Functions.Like(contact.Nombre, $"%{term}%")
            || EF.Functions.Like(contact.Apellido, $"%{term}%")
        );
    }

    return query.ToList();
}
```

`EF.Functions.Like` genera un `LIKE` en SQL, que SQLite evalúa en la base de datos, no en memoria. Si tuvieras miles de contactos, esto es importante.

## Búsqueda en caliente con HTMX

El campo de búsqueda es el ejemplo más claro del poder de HTMX para interacciones en tiempo real sin JavaScript propio:

```html
<form id="search-form">
    <input
        name="search"
        type="search"
        hx-get="/Agenda?handler=List"
        hx-trigger="input changed delay:250ms, search"
        hx-target="#master"
        hx-swap="innerHTML" />
</form>
```

`hx-trigger="input changed delay:250ms"` significa: cuando el input cambie, esperá 250ms, y si no cambia de nuevo, hacé el GET. Eso es debounce sin una línea de JavaScript.

El servidor recibe el valor del input como query string porque el input tiene `name="search"` y está dentro del form que HTMX serializa. El handler `OnGetList` recibe ese valor y filtra:

```csharp
public IActionResult OnGetList(string? search) {
    return Partial("_ContactList", ContactStore.GetAll(search));
}
```

## El flujo completo de una operación de guardado

Para que veas cómo se conecta todo, el flujo de editar un contacto pasa por cuatro pasos:

**1. El usuario selecciona un contacto.**
HTMX hace `GET /Agenda?handler=Detail&id=2`. El servidor ejecuta `OnGetDetail` y devuelve `_ContactDetail`. HTMX reemplaza `#detail` con la vista readonly.

**2. El usuario hace clic en Editar.**
HTMX hace `GET /Agenda?handler=Edit&id=2`. El servidor ejecuta `OnGetEdit` y devuelve `_ContactForm` con los datos actuales. HTMX reemplaza `#detail` con el formulario.

**3. El usuario guarda.**
HTMX hace `POST /Agenda?handler=Save` con todos los campos del form más el token antiforgery. El servidor valida el token, ejecuta `OnPostSave`, persiste en SQLite, y devuelve `_ContactDetail` con los datos actualizados. HTMX reemplaza `#detail` con la vista readonly.

**4. La lista se actualiza sola.**
El partial devuelto incluye un `div` con `hx-trigger="load"` que dispara `GET /Agenda?handler=List`. HTMX reemplaza `#master` con la lista actualizada.

En ningún momento se recarga la página completa. En ningún momento se escribió JavaScript.

## Cómo pensar en términos de fragmentos

El cambio mental que requiere HTMX es dejar de pensar en endpoints que devuelven JSON y empezar a pensar en endpoints que devuelven fragmentos HTML.

Cuando diseñás una app con HTMX:

1. **Identificás las zonas de la UI que cambian** independientemente. En esta app son `#master` y `#detail`.
2. **Creás un partial por cada estado de cada zona.** `_ContactList` es el estado de `#master`. `_ContactDetail` y `_ContactForm` son dos estados posibles de `#detail`.
3. **Cada acción del usuario dispara un request que devuelve el nuevo estado** de una zona.
4. **El servidor siempre devuelve HTML listo para mostrar**, no datos en crudo.

Esta separación es lo que permite que la app no tenga JavaScript propio. La lógica de presentación vive en el servidor, en los partials. El navegador solo necesita saber dónde poner cada fragmento.

## Cómo crear el proyecto desde cero

```bash
dotnet new webapp -n AgendaRazorHtmx
cd AgendaRazorHtmx
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

La plantilla `webapp` genera una app con Razor Pages. A partir de ahí:

1. Agregar la referencia a HTMX en `_Layout.cshtml`:

```html
<script src="https://unpkg.com/htmx.org@2"></script>
```

2. Definir el modelo de datos y el DbContext en `Program.cs`.
3. Registrar los servicios y configurar la cadena de conexión.
4. Crear la página `Pages/Agenda/Index.cshtml` con los handlers.
5. Crear los partials `_ContactList`, `_ContactDetail` y `_ContactForm`.
6. Agregar los estilos en `wwwroot/css/site.css`.

## Qué podés construir a partir de acá

Una vez que entendés este patrón, podés aplicarlo a cualquier interfaz con zonas independientes:

- Una lista de tareas con filtros en caliente
- Un dashboard con widgets que se actualizan por separado
- Un formulario de varios pasos donde cada paso es un partial distinto
- Una tabla con paginación o sorting sin recargar la página

HTMX también tiene atributos más avanzados para casos específicos:
- `hx-boost` convierte links en requests parciales
- `hx-push-url` actualiza la URL del navegador
- `hx-out-of-band` actualiza múltiples zonas con una sola respuesta
- `hx-indicator` muestra indicadores de carga mientras espera la respuesta

La combinación de Razor Pages + HTMX + EF Core es particularmente adecuada cuando querés una app full-stack donde el servidor es la fuente de verdad, sin necesidad de una API separada ni un frontend framework, pero con la experiencia fluida que da la actualización parcial.
