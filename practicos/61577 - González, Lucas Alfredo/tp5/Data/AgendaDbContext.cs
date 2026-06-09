using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

/// <summary>
/// Unidad de trabajo de Entity Framework Core para la agenda.
/// </summary>
/// <remarks>
/// El contexto se crea mediante <see cref="IDbContextFactory{TContext}"/> para
/// que cada operación interactiva de Blazor utilice una instancia corta e
/// independiente. Esto evita compartir un contexto no seguro para concurrencia
/// durante toda la vida del circuito del usuario.
/// </remarks>
public sealed class AgendaDbContext(DbContextOptions<AgendaDbContext> options)
    : DbContext(options)
{
    /// <summary>Contactos persistidos en el archivo SQLite provisto con el TP.</summary>
    public DbSet<Contacto> Contactos => Set<Contacto>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // La tabla ya existe en contactos.db. La configuración explícita
        // documenta el contrato y evita depender de convenciones implícitas.
        modelBuilder.Entity<Contacto>(entidad =>
        {
            entidad.ToTable("Contactos");
            entidad.HasKey(contacto => contacto.Id);
            entidad.Property(contacto => contacto.Id).ValueGeneratedOnAdd();
        });
    }
}
