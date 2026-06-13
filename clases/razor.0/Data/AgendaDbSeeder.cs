using RazorHolaMundo.Models;

namespace RazorHolaMundo.Data;

public static class AgendaDbSeeder {
    public static void Seed(AgendaDbContext dbContext) {
        Contacto[] contactos = {
            new() { Nombre = "Ana", Apellido = "Alvarez", Telefono = "3815551001", Email = "ana@mail.com", EsFavorito = true },
            new() { Nombre = "Bruno", Apellido = "Benitez", Telefono = "3815551002", Email = "bruno@mail.com", EsFavorito = false },
            new() { Nombre = "Carla", Apellido = "Castro", Telefono = "3815551003", Email = null, EsFavorito = true },
            new() { Nombre = "Diego", Apellido = "Diaz", Telefono = "3815551004", Email = "diego@mail.com", EsFavorito = false },
            new() { Nombre = "Elena", Apellido = "Escobar", Telefono = "3815551005", Email = "elena@mail.com", EsFavorito = true },
            new() { Nombre = "Franco", Apellido = "Fernandez", Telefono = "3815551006", Email = null, EsFavorito = false },
            new() { Nombre = "Gabriela", Apellido = "Gomez", Telefono = "3815551007", Email = "gabriela@mail.com", EsFavorito = true },
            new() { Nombre = "Hugo", Apellido = "Herrera", Telefono = "3815551008", Email = "hugo@mail.com", EsFavorito = false },
            new() { Nombre = "Irene", Apellido = "Ibarra", Telefono = "3815551009", Email = null, EsFavorito = true },
            new() { Nombre = "Joaquin", Apellido = "Juarez", Telefono = "3815551010", Email = "joaquin@mail.com", EsFavorito = false }
        };

        HashSet<string> telefonosDeEjemplo = contactos.Select(contacto => contacto.Telefono).ToHashSet();
        List<Contacto> contactosFueraDelEjemplo = dbContext.Contactos
            .Where(contacto => !telefonosDeEjemplo.Contains(contacto.Telefono))
            .ToList();

        dbContext.Contactos.RemoveRange(contactosFueraDelEjemplo);

        foreach (Contacto contacto in contactos) {
            List<Contacto> existentes = dbContext.Contactos
                .Where(actual => actual.Telefono == contacto.Telefono)
                .OrderBy(actual => actual.Id)
                .ToList();

            Contacto? existente = existentes.FirstOrDefault();

            if (existente is null) {
                dbContext.Contactos.Add(contacto);
            } else {
                existente.Nombre = contacto.Nombre;
                existente.Apellido = contacto.Apellido;
                existente.Email = contacto.Email;
            }

            dbContext.Contactos.RemoveRange(existentes.Skip(1));
        }

        dbContext.SaveChanges();
    }
}
