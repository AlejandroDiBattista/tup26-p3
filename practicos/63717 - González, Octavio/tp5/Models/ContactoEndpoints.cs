namespace tp5.Models;

public static class Endpoints
{
    public static void ContactoEndpoint(this IEndpointRouteBuilder app)
    {
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
            actualizacion.Id = id;
            await repositorio.Actualizar(actualizacion);
            return Results.Ok(actualizacion);
        });

        app.MapDelete("/contactos/{id:int}", async (int id, Repositorio repositorio) =>
        {
            await repositorio.Eliminar(id);
            return Results.Ok();
        });
    }
}   