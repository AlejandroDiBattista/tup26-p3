using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class AgendaContext : DbContext
{
    public AgendaContext(DbContextOptions<AgendaContext> opciones) : base(opciones)
    {
    }

    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contacto>().ToTable("Contactos");
        modelBuilder.Entity<Contacto>().HasKey(contacto => contacto.Id);
    }
}
