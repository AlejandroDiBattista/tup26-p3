using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

// DbContext: representa la conexion entre la aplicacion y la base SQLite.
public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> opciones) : base(opciones)
    {
    }

    // DbSet: coleccion de contactos que Entity Framework consulta y modifica.
    public DbSet<Contacto> Contactos => Set<Contacto>();

    // Configura la tabla, la clave primaria y las reglas basicas de cada campo.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contacto>(entidad =>
        {
            entidad.ToTable("Contactos");
            entidad.HasKey(contacto => contacto.Id);

            entidad.Property(contacto => contacto.Nombre).IsRequired();
            entidad.Property(contacto => contacto.Apellido).IsRequired();
            entidad.Property(contacto => contacto.Telefono).IsRequired();
            entidad.Property(contacto => contacto.Email).IsRequired();
            entidad.Property(contacto => contacto.Empresa).IsRequired();
            entidad.Property(contacto => contacto.Cargo).IsRequired();
            entidad.Property(contacto => contacto.Direccion).IsRequired();
            entidad.Property(contacto => contacto.Notas).IsRequired();
        });
    }
}