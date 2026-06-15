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
            entity.ToTable("Contactos");
            entity.HasKey(contacto => contacto.Id);
            entity.Property(contacto => contacto.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(contacto => contacto.Apellido).HasMaxLength(80).IsRequired();
            entity.Property(contacto => contacto.Telefono).HasMaxLength(40).IsRequired();
            entity.Property(contacto => contacto.Email).HasMaxLength(120).IsRequired();
            entity.Property(contacto => contacto.Empresa).HasMaxLength(120);
            entity.Property(contacto => contacto.Cargo).HasMaxLength(120);
            entity.Property(contacto => contacto.Direccion).HasMaxLength(180);
            entity.Property(contacto => contacto.Notas).HasMaxLength(1000);
        });
    }
}