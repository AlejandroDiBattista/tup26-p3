using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public class AgendaaContext : DbContext
{
    public AgendaaContext(DbContextOptions<AgendaaContext> options): base(options)
    {
    }

    public DbSet<Contacto> Contactos { get; set; } = default!;
}