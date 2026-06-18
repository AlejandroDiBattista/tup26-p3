using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

/// <summary>
/// Centraliza las operaciones de la agenda y evita que los componentes de la
/// interfaz conozcan detalles de Entity Framework Core.
/// </summary>
public sealed class ContactoService(IDbContextFactory<AgendaContext> contextFactory)
{
    /// <summary>
    /// Obtiene los contactos ordenados por apellido y nombre. Cuando se recibe
    /// un texto, busca sin distinguir mayúsculas en los datos más reconocibles.
    /// </summary>
    public async Task<IReadOnlyList<Contacto>> ListarAsync(
        string? busqueda = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            query = query.Where(contacto =>
                EF.Functions.Like(contacto.Nombre, patron) ||
                EF.Functions.Like(contacto.Apellido, patron) ||
                EF.Functions.Like(contacto.Empresa, patron) ||
                EF.Functions.Like(contacto.Email, patron) ||
                EF.Functions.Like(contacto.Telefono, patron));
        }

        return await query
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Busca un contacto por su identificador sin dejarlo asociado al contexto.</summary>
    public async Task<Contacto?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Contactos.AsNoTracking()
            .SingleOrDefaultAsync(contacto => contacto.Id == id, cancellationToken);
    }

    /// <summary>Agrega un contacto y devuelve su identificador generado.</summary>
    public async Task<int> CrearAsync(Contacto contacto, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        Normalizar(contacto);
        context.Contactos.Add(contacto);
        await context.SaveChangesAsync(cancellationToken);
        return contacto.Id;
    }

    /// <summary>
    /// Actualiza un contacto existente. Devuelve <see langword="false"/> si fue
    /// eliminado por otro proceso antes de guardar.
    /// </summary>
    public async Task<bool> ActualizarAsync(Contacto contacto, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var actual = await context.Contactos.FindAsync([contacto.Id], cancellationToken);
        if (actual is null)
        {
            return false;
        }

        Normalizar(contacto);
        context.Entry(actual).CurrentValues.SetValues(contacto);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Elimina el contacto indicado si todavía existe.</summary>
    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var eliminados = await context.Contactos
            .Where(contacto => contacto.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
        return eliminados == 1;
    }

    private static void Normalizar(Contacto contacto)
    {
        contacto.Nombre = contacto.Nombre.Trim();
        contacto.Apellido = contacto.Apellido.Trim();
        contacto.Telefono = contacto.Telefono.Trim();
        contacto.Email = contacto.Email.Trim();
        contacto.Empresa = contacto.Empresa.Trim();
        contacto.Cargo = contacto.Cargo.Trim();
        contacto.Direccion = contacto.Direccion.Trim();
        contacto.Notas = contacto.Notas.Trim();
    }
}
