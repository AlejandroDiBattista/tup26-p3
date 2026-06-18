using tp5.Components;
using Microsoft.EntityFrameworkCore;
using tp5.Models;
using BlazorBlueprint.Components;
using tp5.Data;

//pasos: configuracion --inicilizacion bd - endpoints - modelo - dbcontext -- repositorio.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddDbContextFactory<AgendaDbContext>(opciones => opciones.UseSqlite("Data Source=contactos.db"));
builder.Services.AddScoped<Repositorio>();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<Repositorio>();
    repo.Iniciar();
}

app.ContactoEndpoint();

app.Run();
