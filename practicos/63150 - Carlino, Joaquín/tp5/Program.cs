using tp5.Components;
using tp5.Datos;
using tp5.Servicios;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// EF Core + SQLite usando una FABRICA de DbContext.
builder.Services.AddDbContextFactory<AgendaDbContext>(opciones =>
    opciones.UseSqlite("Data Source=contactos.db"));

// Nuestra logica de aplicacion.
builder.Services.AddScoped<IContactoService, ContactoService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
