using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

/// <summary>
/// Centraliza las consultas y modificaciones disponibles sobre la agenda.
/// </summary>
/// <remarks>
/// La interfaz de usuario no conoce detalles de Entity Framework ni de SQLite.
/// Cada método crea y descarta su propio contexto, por lo que puede invocarse
/// de forma segura desde componentes Blazor de larga duración.
/// </remarks>
public sealed class AgendaService(IDbContextFactory<AgendaDbContext> contextFactory)
{
    /// <summary>
    /// Obtiene los contactos ordenados alfabéticamente y aplica un filtro
    /// opcional sobre los principales datos visibles.
    /// </summary>
    /// <param name="busqueda">Texto libre ingresado por el usuario.</param>
    /// <param name="cancellationToken">Permite cancelar la consulta en curso.</param>
    public async Task<IReadOnlyList<Contacto>> BuscarAsync(
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync(cancellationToken);
        var consulta = contexto.Contactos.AsNoTracking();

        var termino = busqueda?.Trim();
        if (!string.IsNullOrWhiteSpace(termino))
        {
            // LIKE se traduce a SQL y evita cargar toda la tabla para filtrar
            // en memoria. Los comodines permiten coincidencias parciales.
            var patron = $"%{termino}%";
            consulta = consulta.Where(contacto =>
                EF.Functions.Like(contacto.Nombre, patron)
                || EF.Functions.Like(contacto.Apellido, patron)
                || EF.Functions.Like(contacto.Email, patron)
                || EF.Functions.Like(contacto.Telefono, patron)
                || EF.Functions.Like(contacto.Empresa, patron)
                || EF.Functions.Like(contacto.Cargo, patron));
        }

        return await consulta
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Obtiene un contacto por su identificador o <see langword="null"/> si no existe.</summary>
    public async Task<Contacto?> ObtenerAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await contexto.Contactos
            .AsNoTracking()
            .SingleOrDefaultAsync(contacto => contacto.Id == id, cancellationToken);
    }

    /// <summary>Agrega un contacto y devuelve la entidad con su identificador generado.</summary>
    public async Task<Contacto> CrearAsync(
        Contacto contacto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contacto);
        Normalizar(contacto);

        await using var contexto = await contextFactory.CreateDbContextAsync(cancellationToken);
        contexto.Contactos.Add(contacto);
        await contexto.SaveChangesAsync(cancellationToken);
        return contacto;
    }

    /// <summary>
    /// Actualiza un contacto existente sin confiar en el estado de seguimiento
    /// recibido desde la interfaz.
    /// </summary>
    /// <returns><see langword="true"/> si se actualizó; de lo contrario, <see langword="false"/>.</returns>
    public async Task<bool> ActualizarAsync(
        Contacto contacto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contacto);
        Normalizar(contacto);

        await using var contexto = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existente = await contexto.Contactos
            .SingleOrDefaultAsync(item => item.Id == contacto.Id, cancellationToken);

        if (existente is null)
        {
            return false;
        }

        CopiarDatos(contacto, existente);
        await contexto.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Elimina el contacto indicado cuando todavía existe.</summary>
    /// <returns><see langword="true"/> si se eliminó una fila.</returns>
    public async Task<bool> EliminarAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync(cancellationToken);
        var filasAfectadas = await contexto.Contactos
            .Where(contacto => contacto.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
        return filasAfectadas > 0;
    }

    /// <summary>
    /// Quita espacios accidentales y garantiza cadenas no nulas antes de
    /// persistir. La validación de campos obligatorios permanece a cargo del
    /// formulario y de las anotaciones del modelo.
    /// </summary>
    private static void Normalizar(Contacto contacto)
    {
        contacto.Nombre = contacto.Nombre?.Trim() ?? string.Empty;
        contacto.Apellido = contacto.Apellido?.Trim() ?? string.Empty;
        contacto.Telefono = contacto.Telefono?.Trim() ?? string.Empty;
        contacto.Email = contacto.Email?.Trim() ?? string.Empty;
        contacto.Empresa = contacto.Empresa?.Trim() ?? string.Empty;
        contacto.Cargo = contacto.Cargo?.Trim() ?? string.Empty;
        contacto.Direccion = contacto.Direccion?.Trim() ?? string.Empty;
        contacto.Notas = contacto.Notas?.Trim() ?? string.Empty;
    }

    /// <summary>Copia únicamente los campos editables sobre la entidad seguida por EF.</summary>
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
