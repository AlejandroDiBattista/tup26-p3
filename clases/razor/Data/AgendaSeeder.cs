using Microsoft.EntityFrameworkCore;
using razor.Models;

namespace razor.Data;

public static class AgendaSeeder {
    public static async Task SeedAsync(IServiceProvider services) {
        using IServiceScope scope = services.CreateScope();
        AgendaDbContext context = scope.ServiceProvider.GetRequiredService<AgendaDbContext>();

        await context.Database.EnsureCreatedAsync();

        if (await context.Contactos.AnyAsync()) {
            return;
        }

        context.Contactos.AddRange(
            new Contacto { Nombre = "Ana", Apellido = "Garcia", Telefono = "381-555-1001", Email = "ana@example.com", Favorito = true },
            new Contacto { Nombre = "Bruno", Apellido = "Lopez", Telefono = "381-555-1002", Email = "bruno@example.com", Favorito = false },
            new Contacto { Nombre = "Carla", Apellido = "Perez", Telefono = "381-555-1003", Email = "carla@example.com", Favorito = true },
            new Contacto { Nombre = "Diego", Apellido = "Romero", Telefono = "381-555-1004", Email = null, Favorito = false },
            new Contacto { Nombre = "Elena", Apellido = "Sosa", Telefono = "381-555-1005", Email = "elena@example.com", Favorito = true },
            new Contacto { Nombre = "Facundo", Apellido = "Molina", Telefono = "381-555-1006", Email = null, Favorito = false },
            new Contacto { Nombre = "Gabriela", Apellido = "Diaz", Telefono = "381-555-1007", Email = "gabriela@example.com", Favorito = false },
            new Contacto { Nombre = "Hector", Apellido = "Vega", Telefono = "381-555-1008", Email = "hector@example.com", Favorito = true },
            new Contacto { Nombre = "Ines", Apellido = "Navarro", Telefono = "381-555-1009", Email = null, Favorito = false },
            new Contacto { Nombre = "Juan", Apellido = "Torres", Telefono = "381-555-1010", Email = "juan@example.com", Favorito = false }
        );

        await context.SaveChangesAsync();
    }
}
