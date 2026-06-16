using Microsoft.EntityFrameworkCore;

namespace tp5.Models;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> opciones) : base(opciones)
    {
    }

    public DbSet<Contacto> Contactos => Set<Contacto>();
}