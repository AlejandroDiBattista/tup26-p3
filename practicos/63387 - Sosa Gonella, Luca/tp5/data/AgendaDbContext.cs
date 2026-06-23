using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(
        DbContextOptions<AgendaDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Contacto> Contactos => Set<Contacto>();
}