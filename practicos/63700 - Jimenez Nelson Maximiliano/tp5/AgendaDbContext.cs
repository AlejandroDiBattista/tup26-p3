using Microsoft.EntityFrameworkCore;

namespace AgendaWeb.Data;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options)
    {
    }

    public DbSet<Contacto> Contactos { get; set; } = null!;
}