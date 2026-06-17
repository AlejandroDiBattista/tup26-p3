using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

/// <summary>Representa la sesión de acceso a la base SQLite de la agenda.</summary>
public sealed class AgendaContext : DbContext
{
    /// <summary>Construye el contexto con las opciones registradas en DI.</summary>
    public AgendaContext(DbContextOptions<AgendaContext> options) : base(options)
    {
    }

    /// <summary>Contactos persistidos por la aplicación.</summary>
    public DbSet<Contacto> Contactos => Set<Contacto>();
}
