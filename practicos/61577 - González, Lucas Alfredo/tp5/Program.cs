using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using tp5.Components;
using tp5.Data;
using tp5.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// La ruta se construye desde ContentRootPath para que la misma base se abra
// tanto con `dotnet run` dentro del TP como con `dotnet run --project ...`.
var rutaBaseDatos = Path.Combine(builder.Environment.ContentRootPath, "contactos.db");
var cadenaConexion = new SqliteConnectionStringBuilder
{
    DataSource = rutaBaseDatos,
    ForeignKeys = true
}.ToString();

// En Blazor Server los componentes viven más que una petición HTTP. La fábrica
// entrega un contexto nuevo por operación y evita problemas de concurrencia.
builder.Services.AddDbContextFactory<AgendaDbContext>(opciones =>
    opciones.UseSqlite(cadenaConexion));
builder.Services.AddScoped<AgendaService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
