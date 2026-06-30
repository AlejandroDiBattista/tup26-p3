using Microsoft.EntityFrameworkCore;

namespace tp5.Models;

public class AgendaContext : DbContext
{
    public AgendaContext(DbContextOptions<AgendaContext> options) : base(options)
    {
    }

    public DbSet<Contacto> Contactos { get; set; }
}