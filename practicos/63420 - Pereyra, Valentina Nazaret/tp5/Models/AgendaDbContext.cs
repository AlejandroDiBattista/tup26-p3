using Microsoft.EntityFrameworkCore;

namespace tp5.Models;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contacto> Contactos { get; set; }
}