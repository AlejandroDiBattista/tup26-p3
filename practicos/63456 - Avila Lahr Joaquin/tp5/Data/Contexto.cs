using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class Contexto : DbContext
{
    public Contexto(DbContextOptions<Contexto> options) : base(options) { }

    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contacto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired();
            entity.Property(e => e.Apellido).IsRequired();
            entity.Property(e => e.Telefono).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.Legajo).IsRequired();
        });
    }
}