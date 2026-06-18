namespace tp5.Models;

public static class Endpoints
{
    public static void ContactoEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/contactos", async (Repositorio repositorio) =>
        {
            var contactos = await repositorio.TraerContactos();
            return Results.Ok(contactos);
        });

        app.MapGet("/contactos/{id:int}", async (int id, Repositorio repositorio) =>
        {
            var contacto = await repositorio.TraerContacto(id);
            if (contacto is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(contacto);
        });

        app.MapPost("/contactos", async (Contacto nuevo, Repositorio repositorio) =>
        {
            await repositorio.AgregarContacto(nuevo);
            return Results.Created($"/contactos/{nuevo.Id}", nuevo);
        });

        app.MapPut("/contactos/{id:int}", async (int id, Contacto actualizacion, Repositorio repositorio) =>
        {
            actualizacion.Id = id;
            var contacto = await repositorio.Actualizar(actualizacion);

            if (contacto is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(actualizacion);
        });

        app.MapDelete("/contactos/{id:int}", async (int id, Repositorio repositorio) =>
        {
            var eliminado = await repositorio.Eliminar(id);

            if (!eliminado)
            {
                return Results.NotFound();
            }

            return Results.Ok();
        });
    }
}   
