using AgendaWeb.Components;
using AgendaWeb.Datos;
using AgendaWeb.Servicios;
using Microsoft.EntityFrameworkCore;

var constructor = WebApplication.CreateBuilder(args);


constructor.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


constructor.Services.AddDbContextFactory<LibretaDbContext>(config =>
    config.UseSqlite("Data Source=contactos.db"));

//lógica de aplicación, acceso a datos.
constructor.Services.AddScoped<IPersonaServicio, PersonaServicio>();

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
