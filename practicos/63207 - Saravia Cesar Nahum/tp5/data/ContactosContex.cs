using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data
{
    public class ContactosContext : DbContext
    {
        public ContactosContext(DbContextOptions<ContactosContext> options)
        : base(options)
        {
        }

        public DbSet<Contacto> Contactos {get; set;}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=contactos.db");
            }
        }
    }
}