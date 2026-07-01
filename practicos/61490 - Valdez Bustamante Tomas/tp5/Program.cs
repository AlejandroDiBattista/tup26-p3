using AgendaWeb.Components;
using AgendaWeb.Datos;
using AgendaWeb.Servicios;
using Microsoft.EntityFrameworkCore;

var constructor = WebApplication.CreateBuilder(args);

// Blazor con interactividad del lado del servidor.
constructor.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Base de datos SQLite con EF Core. Usamos una fábrica para crear
// un contexto nuevo y de vida corta en cada operación.
constructor.Services.AddDbContextFactory<AgendaDbContext>(opciones =>
    opciones.UseSqlite("Data Source=contactos.db"));

// Servicio para acceder a los contactos a través de la interfaz.
constructor.Services.AddScoped<IContactoService, ContactoService>();

var aplicacion = constructor.Build();

if (!aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseExceptionHandler("/Error", createScopeForErrors: true);
    aplicacion.UseHsts();
}

aplicacion.UseHttpsRedirection();
aplicacion.UseAntiforgery();
aplicacion.MapStaticAssets();

aplicacion.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

aplicacion.Run();
