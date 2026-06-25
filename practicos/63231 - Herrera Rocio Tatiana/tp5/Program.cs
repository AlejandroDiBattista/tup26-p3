using Microsoft.EntityFrameworkCore;
using tp5.Components;
using tp5.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AgendaDbContext>(opciones =>
    opciones.UseSqlite("Data Source=contactos.db"));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
