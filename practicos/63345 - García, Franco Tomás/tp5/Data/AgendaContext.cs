using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data
{
    public class AgendaContext : DbContext
    {
        public DbSet<Contacto> Contactos { get; set; }

        public AgendaContext(DbContextOptions<AgendaContext> options)
            : base(options)
        {
        }
    }
}