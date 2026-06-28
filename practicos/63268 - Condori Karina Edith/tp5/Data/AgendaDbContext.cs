using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public sealed class AgendaDbContext(DbContextOptions<AgendaDbContext> options)
    : DbContext(options)
{
    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contacto>(entity =>
        {
            entity.HasKey(contacto => contacto.Id);
            entity.Property(contacto => contacto.Nombre).IsRequired();
            entity.Property(contacto => contacto.Apellido).IsRequired();
            entity.Property(contacto => contacto.Telefono).IsRequired();
            entity.Property(contacto => contacto.Email).IsRequired();
        });
    }
}
