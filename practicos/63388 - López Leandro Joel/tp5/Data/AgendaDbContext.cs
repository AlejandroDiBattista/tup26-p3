using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options) { }

    public DbSet<Contacto> Contactos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contacto>(entity =>
        {
            // Nombre explícito de tabla para que coincida con el .db del repo
            entity.ToTable("Contactos");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("Id");
            entity.Property(c => c.Nombre).HasColumnName("Nombre").IsRequired().HasMaxLength(100);
            entity.Property(c => c.Apellido).HasColumnName("Apellido").IsRequired().HasMaxLength(100);
            entity.Property(c => c.Telefono).HasColumnName("Telefono").IsRequired().HasMaxLength(30);
            entity.Property(c => c.Email).HasColumnName("Email").IsRequired().HasMaxLength(150);
            entity.Property(c => c.Empresa).HasColumnName("Empresa").HasMaxLength(150);
            entity.Property(c => c.Cargo).HasColumnName("Cargo").HasMaxLength(100);
            entity.Property(c => c.Direccion).HasColumnName("Direccion").HasMaxLength(250);
            entity.Property(c => c.FechaNacimiento).HasColumnName("FechaNacimiento");
            entity.Property(c => c.Notas).HasColumnName("Notas");
            entity.Ignore(c => c.NombreCompleto);
            entity.Ignore(c => c.Iniciales);
        });
    }
}
