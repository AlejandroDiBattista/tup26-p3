using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Contacto>(entity =>
        {
            entity.ToTable("Contactos");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CorreoElectronico)
                .HasColumnName("Email");
        });
    }
}
