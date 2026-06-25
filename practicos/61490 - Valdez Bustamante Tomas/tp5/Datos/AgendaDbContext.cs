using AgendaWeb.Modelos;
using Microsoft.EntityFrameworkCore;

namespace AgendaWeb.Datos;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> opciones)
        : base(opciones)
    {
    }

    // Se mapea a la tabla "Contactos".
    public DbSet<Contacto> Contactos => this.Set<Contacto>();
}
