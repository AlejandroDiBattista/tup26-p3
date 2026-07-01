using tp5.Components;
using Microsoft.EntityFrameworkCore;
using tp5.Models;

//pasos: configuracion --inicilizacion bd - endpoints - modelo - dbcontext -- repositorio.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<ContactoDb>(opciones => opciones.UseSqlite("Data Source=contactos.db"));
builder.Services.AddScoped<Repositorio>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<Repositorio>();
    repo.Iniciar();
}

app.Run();