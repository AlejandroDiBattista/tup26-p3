using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Datos;

public class AgendaDb : DbContext
{
    public AgendaDb(DbContextOptions<AgendaDb> options) : base(options) { }

    public DbSet<Contacto> Contactos => Set<Contacto>();
}