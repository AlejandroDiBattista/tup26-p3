using Microsoft.EntityFrameworkCore;
using tp5.Components;
using tp5.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// configuro el DbContext para que use el archivo contactos.db
builder.Services.AddDbContext<AgendaDbContext>(opciones =>
    opciones.UseSqlite("Data Source=contactos.db"));

// registro el servicio de contactos para poder inyectarlo en los componentes
builder.Services.AddScoped<ContactoService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();