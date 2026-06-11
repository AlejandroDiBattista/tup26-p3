using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Datos;
public class AgendaContexto : DbContext
{
    public AgendaContexto(DbContextOptions<AgendaContexto> options) : base(options)
    {
    }
    public DbSet<Contacto> Contactos { get; set; }
}
