using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

/// <summary>
/// Representa la base de datos de la agenda y configura la entidad persistida.
/// El contexto se crea mediante una fábrica porque los componentes Blazor viven
/// más tiempo que una petición HTTP y no deben compartir una misma instancia.
/// </summary>
public sealed class AgendaContext(DbContextOptions<AgendaContext> options) : DbContext(options)
{
    /// <summary>Contactos almacenados en la agenda.</summary>
    public DbSet<Contacto> Contactos => Set<Contacto>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var contacto = modelBuilder.Entity<Contacto>();

        contacto.ToTable("Contactos");
        contacto.HasKey(item => item.Id);
        contacto.Property(item => item.Id).ValueGeneratedOnAdd();
        contacto.Property(item => item.Nombre).IsRequired();
        contacto.Property(item => item.Apellido).IsRequired();
        contacto.Property(item => item.Telefono).IsRequired();
        contacto.Property(item => item.Email).IsRequired();
    }
}
