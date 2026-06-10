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


// //-----------------------------------
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<Repositorio>();
    
    repo.Iniciar();

}
//-----------------------------------

app.MapGet("/contactos", (Repositorio repositorio) =>
{
    var contactos = repositorio.TraerContactos();
    if (contactos is null) return Results.NotFound();
    return Results.Ok(contactos);
});
app.MapGet("/contactos/{id:int}", (int id, Repositorio repositorio) =>
{
    var contacto = repositorio.TraerContacto(id);
    if (contacto is null) return Results.NotFound();
    return Results.Ok(contacto);
});

app.MapPost("/contactos", async (Contacto nuevo, Repositorio repositorio) =>
{
    await repositorio.AgregarContacto(nuevo);
    return Results.Ok(nuevo);
});

app.MapPut("/contactos/{id:int}", async (int id, Contacto actualizacion, Repositorio repositorio) =>
{
    await repositorio.Actualizar(id, actualizacion);
    return Results.Ok(actualizacion);
});

app.MapDelete("/contactos/{id:int}", async (int id, Repositorio repositorio) =>
{
    await repositorio.Eliminar(id);
    return Results.Ok();
});


app.Run("http://localhost:3000");



//-----------------------------------
