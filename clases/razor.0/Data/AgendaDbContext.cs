using Microsoft.EntityFrameworkCore;
using RazorHolaMundo.Models;

namespace RazorHolaMundo.Data;

public class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options) {
    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Contacto>(entity => {
            entity.ToTable("Contactos");

            entity.HasKey(contacto => contacto.Id);

            entity.Property(contacto => contacto.Nombre)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(contacto => contacto.Apellido)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(contacto => contacto.Telefono)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(contacto => contacto.Email)
                .HasMaxLength(120);

            entity.Property(contacto => contacto.EsFavorito)
                .IsRequired();
        });
    }
}
