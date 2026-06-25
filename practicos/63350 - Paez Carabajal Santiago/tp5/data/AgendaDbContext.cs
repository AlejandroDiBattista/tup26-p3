using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options)
    {
    }

    public DbSet<Contacto> Contactos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Le avisamos a EF que la tabla en SQLite se llama "Contactos"
        modelBuilder.Entity<Contacto>().ToTable("Contactos");
    }
}
