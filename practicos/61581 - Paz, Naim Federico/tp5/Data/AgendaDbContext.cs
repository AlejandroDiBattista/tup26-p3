using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options)
{
    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contacto>(entity =>
        {
            entity.HasKey(contacto => contacto.Id);
            entity.Property(contacto => contacto.Nombre).IsRequired().HasMaxLength(80);
            entity.Property(contacto => contacto.Apellido).IsRequired().HasMaxLength(80);
            entity.Property(contacto => contacto.Telefono).IsRequired().HasMaxLength(40);
            entity.Property(contacto => contacto.Email).IsRequired().HasMaxLength(120);
            entity.Property(contacto => contacto.Empresa).HasMaxLength(120);
            entity.Property(contacto => contacto.Cargo).HasMaxLength(120);
            entity.Property(contacto => contacto.Direccion).HasMaxLength(180);
            entity.Property(contacto => contacto.Notas).HasMaxLength(500);
        });
    }
}
