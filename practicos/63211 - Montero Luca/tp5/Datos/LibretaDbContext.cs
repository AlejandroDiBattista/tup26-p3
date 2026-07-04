using AgendaWeb.Modelos;
using Microsoft.EntityFrameworkCore;

namespace AgendaWeb.Datos;

public class LibretaDbContext : DbContext
{
    public LibretaDbContext(DbContextOptions<LibretaDbContext> opciones)
        : base(opciones)
    {
    }

    public DbSet<Persona> Personas => this.Set<Persona>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // La base SQLite existente usa la tabla "Contactos",
        // así que mapeamos la entidad a ese nombre de tabla.
        modelBuilder.Entity<Persona>().ToTable("Contactos");
    }
}
