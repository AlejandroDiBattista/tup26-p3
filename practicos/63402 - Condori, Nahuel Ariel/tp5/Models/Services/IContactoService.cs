using tp5.Models;

namespace tp5.Services;

/// <summary>
/// Define las operaciones de aplicación disponibles para administrar contactos.
/// </summary>
public interface IContactoService
{
    /// <summary>Obtiene la agenda ordenada y, opcionalmente, filtrada.</summary>
    Task<List<Contacto>> GetContactosAsync(
        string filtro = "",
        CancellationToken cancellationToken = default);

    /// <summary>Valida y agrega un contacto nuevo.</summary>
    Task AddContactoAsync(
        Contacto contacto,
        CancellationToken cancellationToken = default);

    /// <summary>Valida y actualiza un contacto existente.</summary>
    Task UpdateContactoAsync(
        Contacto contacto,
        CancellationToken cancellationToken = default);

    /// <summary>Elimina el contacto indicado cuando existe.</summary>
    Task DeleteContactoAsync(
        int id,
        CancellationToken cancellationToken = default);
}
