using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace AgendaWeb.Data;

public class AgendaContext : DbContext {
    
    public AgendaContext(DbContextOptions<AgendaContext> options) : base(options) { }

    public DbSet<Contacto> Contactos { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        
         base.OnModelCreating(modelBuilder);

            // Configuración de la tabla Contactos
            modelBuilder.Entity<Contacto>(entity =>
            {
                // La tabla se llamará "Contactos"
                entity.ToTable("Contactos");

                // La clave primaria es Id
                entity.HasKey(e => e.Id);

                // Configurar los campos obligatorios
                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Apellido)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Telefono)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                // Campos opcionales
                entity.Property(e => e.Empresa)
                    .HasMaxLength(100);

                entity.Property(e => e.Cargo)
                    .HasMaxLength(100);

                entity.Property(e => e.Direccion)
                    .HasMaxLength(200);

                entity.Property(e => e.Notas)
                    .HasMaxLength(500);
                entity.Property(e => e.Legajo)
                    .HasMaxLength(50);

    });
    }
}
