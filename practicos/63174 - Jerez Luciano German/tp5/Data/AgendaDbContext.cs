using Microsoft.EntityFrameworkCore;

namespace AgendaWeb.Data
{
    public class AgendaDbContext : DbContext
    {
        // El constructor recibe las opciones de configuración (como la ruta de la base de datos)
        public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options) { }

        // Este DbSet representa la tabla de contactos en la base de datos
        public DbSet<Contacto> Contactos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Le indicamos explícitamente a EF Core que la tabla física en SQLite se llama "Contactos"
            modelBuilder.Entity<Contacto>().ToTable("Contactos");
        }
    }
}