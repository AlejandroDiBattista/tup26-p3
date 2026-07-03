using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Contacto> Contactos => Set<Contacto>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Contacto>(entity =>
            {
                entity.ToTable("Contactos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Telefono).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CorreoElectronico).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Empresa).HasMaxLength(200);
                entity.Property(e => e.Cargo).HasMaxLength(100);
                entity.Property(e => e.Direccion).HasMaxLength(300);
                entity.Property(e => e.Notas).HasMaxLength(1000);
            });
        }
    }
}
