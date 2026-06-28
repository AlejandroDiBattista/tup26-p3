using Microsoft.EntityFrameworkCore;
using tp5.Models; 

namespace tp5.Datos
{
    public class AgendaContext : DbContext
    {
        public AgendaContext(DbContextOptions<AgendaContext> options) : base(options)
        {

        }
        public DbSet<Contacto> Contactos { get; set; }
    }
}