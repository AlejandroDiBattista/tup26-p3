using tp5.Models;
using Microsoft.EntityFrameworkCore;
namespace tp5.Datos;
public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options)
        : base(options) { }
    // Por convencion, este DbSet se mapea a la tabla "Contactos".
    public DbSet<Contacto> Contactos => this.Set<Contacto>();
}