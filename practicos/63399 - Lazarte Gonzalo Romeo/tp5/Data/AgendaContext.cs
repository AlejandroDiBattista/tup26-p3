using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class AgendaContext : DbContext
{
    public AgendaContext(DbContextOptions<AgendaContext> options) : base(options) { }

    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contacto>(entidad =>
        {
            entidad.Ignore(c => c.NombreCompleto);
            entidad.Ignore(c => c.Iniciales);
        });
    }
}
