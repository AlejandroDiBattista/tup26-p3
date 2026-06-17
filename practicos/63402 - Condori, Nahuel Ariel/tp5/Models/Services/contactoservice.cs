using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

/// <summary>
/// Coordina las consultas y modificaciones de la agenda mediante contextos de
/// corta duración, apropiados para el ciclo de vida de Blazor Server.
/// </summary>
public sealed class ContactoService : IContactoService
{
    private readonly IDbContextFactory<AgendaContext> _contextFactory;

    public ContactoService(IDbContextFactory<AgendaContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<List<Contacto>> GetContactosAsync(
        string filtro = "",
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            filtro = filtro.Trim();
            query = query.Where(c =>
                c.Nombre.Contains(filtro) ||
                c.Apellido.Contains(filtro) ||
                c.Telefono.Contains(filtro) ||
                c.Email.Contains(filtro) ||
                c.Empresa.Contains(filtro));
        }

        return await query
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddContactoAsync(
        Contacto contacto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contacto);
        NormalizarYValidar(contacto);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Contactos.Add(contacto);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateContactoAsync(
        Contacto contacto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contacto);
        NormalizarYValidar(contacto);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var almacenado = await context.Contactos.FindAsync([contacto.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"No existe el contacto {contacto.Id}.");

        CopiarDatos(contacto, almacenado);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteContactoAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var contacto = await context.Contactos.FindAsync([id], cancellationToken);

        if (contacto != null)
        {
            context.Contactos.Remove(contacto);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Elimina espacios accidentales en los extremos y ejecuta las anotaciones
    /// del modelo antes de que los datos lleguen a SQLite.
    /// </summary>
    private static void NormalizarYValidar(Contacto contacto)
    {
        contacto.Nombre = contacto.Nombre.Trim();
        contacto.Apellido = contacto.Apellido.Trim();
        contacto.Telefono = contacto.Telefono.Trim();
        contacto.Email = contacto.Email.Trim();
        contacto.Empresa = contacto.Empresa.Trim();
        contacto.Cargo = contacto.Cargo.Trim();
        contacto.Direccion = contacto.Direccion.Trim();
        contacto.Notas = contacto.Notas.Trim();

        Validator.ValidateObject(
            contacto,
            new ValidationContext(contacto),
            validateAllProperties: true);
    }

    /// <summary>
    /// Copia únicamente los campos editables sobre la entidad rastreada para
    /// impedir que el identificador o el estado interno sean sobrescritos.
    /// </summary>
    private static void CopiarDatos(Contacto origen, Contacto destino)
    {
        destino.Nombre = origen.Nombre;
        destino.Apellido = origen.Apellido;
        destino.Telefono = origen.Telefono;
        destino.Email = origen.Email;
        destino.Empresa = origen.Empresa;
        destino.Cargo = origen.Cargo;
        destino.Direccion = origen.Direccion;
        destino.FechaNacimiento = origen.FechaNacimiento;
        destino.Notas = origen.Notas;
    }
}
