using Microsoft.EntityFrameworkCore;
using CrmAgente.Models;

namespace CrmAgente.Data;

// El DbContext es el puente entre nuestras clases C# y la base SQLite.
// Cada DbSet se mapea a una tabla. EF Core traduce LINQ a SQL.
public class CrmContext : DbContext {
    public CrmContext(DbContextOptions<CrmContext> options) : base(options) { }

    public DbSet<Contacto> Contactos => this.Set<Contacto>();

    public DbSet<Oportunidad> Oportunidades => this.Set<Oportunidad>();

    public DbSet<Actividad> Actividades => this.Set<Actividad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        // Guardamos el enum Etapa como texto en la base, mas legible
        // que un numero si alguien mira la tabla directamente.
        modelBuilder.Entity<Oportunidad>()
            .Property(o => o.Etapa)
            .HasConversion<string>();

        // Si se borra un contacto, se borran en cascada sus oportunidades
        // y actividades (no tiene sentido que queden huerfanas).
        modelBuilder.Entity<Contacto>()
            .HasMany(c => c.Oportunidades)
            .WithOne(o => o.Contacto)
            .HasForeignKey(o => o.ContactoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Contacto>()
            .HasMany(c => c.Actividades)
            .WithOne(a => a.Contacto)
            .HasForeignKey(a => a.ContactoId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    // Carga datos de ejemplo si la base esta vacia, asi la app
    // arranca con algo que mostrar.
    public static void Seed(CrmContext db) {
        if (db.Contactos.Any()) {
            return;
        }

        var perez = new Contacto
        {
            Nombre   = "Juan Perez",
            Empresa  = "Perez Construcciones",
            Email    = "jperez@construcciones.com",
            Telefono = "381-555-1234"
        };

        var aceros = new Contacto
        {
            Nombre   = "Maria Gomez",
            Empresa  = "Aceros del Norte",
            Email    = "mgomez@aceros.com",
            Telefono = "381-555-9876"
        };

        db.Contactos.AddRange(perez, aceros);
        db.SaveChanges();

        db.Oportunidades.AddRange(
            new Oportunidad { ContactoId = perez.Id,  Descripcion = "Plan anual", Monto = 480000, Etapa = Etapa.Negociacion },
            new Oportunidad { ContactoId = perez.Id,  Descripcion = "Soporte premium", Monto = 120000, Etapa = Etapa.Ganada },
            new Oportunidad { ContactoId = aceros.Id, Descripcion = "Licencias", Monto = 250000, Etapa = Etapa.Negociacion },
            new Oportunidad { ContactoId = aceros.Id, Descripcion = "Capacitacion", Monto = 80000, Etapa = Etapa.Propuesta }
        );

        db.Actividades.AddRange(
            new Actividad { ContactoId = perez.Id,  Tipo = "Reunion", Detalle = "Primer contacto en la feria", Fecha = DateTime.Now.AddDays(-26) },
            new Actividad { ContactoId = perez.Id,  Tipo = "Mail",    Detalle = "Le envie la propuesta del plan anual", Fecha = DateTime.Now.AddDays(-20) },
            new Actividad { ContactoId = perez.Id,  Tipo = "Llamada", Detalle = "Interesado, quedo en revisar la propuesta", Fecha = DateTime.Now.AddDays(-5) },
            new Actividad { ContactoId = aceros.Id, Tipo = "Llamada", Detalle = "Consulta inicial por licencias", Fecha = DateTime.Now.AddDays(-12) }
        );

        db.SaveChanges();
    }
}
