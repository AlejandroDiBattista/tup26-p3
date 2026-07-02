using tp5.Components;
using tp5.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AgendaContext>(opciones =>
    opciones.UseSqlite($"Data Source={Path.Combine(builder.Environment.ContentRootPath, "contactos.db")}"));

var app = builder.Build();

using (var alcance = app.Services.CreateScope())
{
    var fabricaContexto = alcance.ServiceProvider.GetRequiredService<IDbContextFactory<AgendaContext>>();
    await using var contexto = await fabricaContexto.CreateDbContextAsync();
    // await DatosIniciales.CargarContactos(contexto);
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
