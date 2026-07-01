using tp5.Components;
using Microsoft.EntityFrameworkCore;
using tp5.Datos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AgendaContext>(options =>
{
    options.UseSqlite("Data source=contactos.db");
});


var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
