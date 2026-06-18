namespace tp5.Data;

using Microsoft.EntityFrameworkCore;
using tp5.Models;

/// <summary>
/// Contexto de Entity Framework Core para la agenda de contactos.
/// Centraliza el acceso a la tabla de contactos y permite inicializar
/// la base SQLite existente o crearla si no está presente.
/// </summary>
public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options)
    {
    }

    public DbSet<Contacto> Contactos => Set<Contacto>();
}
