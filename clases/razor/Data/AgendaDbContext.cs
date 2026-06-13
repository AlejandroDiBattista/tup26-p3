using Microsoft.EntityFrameworkCore;
using razor.Models;

namespace razor.Data;

public class AgendaDbContext : DbContext {
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options) {
    }

    public DbSet<Contacto> Contactos => Set<Contacto>();
}
