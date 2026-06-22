using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public static class AgendaSeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        AgendaDbContext db = scope.ServiceProvider.GetRequiredService<AgendaDbContext>();

        await db.Database.EnsureCreatedAsync();

        if (await db.Contactos.AnyAsync())
        {
            return;
        }

        db.Contactos.AddRange(
            new Contacto { Nombre = "Ana", Apellido = "Gomez", Telefono = "381-455-1020", Email = "ana.gomez@example.com", Empresa = "NorteSoft", Cargo = "Analista", Direccion = "San Martin 120", FechaNacimiento = new DateOnly(1992, 4, 18), Notas = "Prefiere contacto por email." },
            new Contacto { Nombre = "Bruno", Apellido = "Paz", Telefono = "381-455-2040", Email = "bruno.paz@example.com", Empresa = "Estudio Paz", Cargo = "Contador", Direccion = "25 de Mayo 88", FechaNacimiento = new DateOnly(1987, 11, 5) },
            new Contacto { Nombre = "Carla", Apellido = "Rios", Telefono = "381-455-3090", Email = "carla.rios@example.com", Empresa = "Clinica Centro", Cargo = "Recepcion", Direccion = "Muñecas 410" },
            new Contacto { Nombre = "Diego", Apellido = "Luna", Telefono = "381-455-4120", Email = "diego.luna@example.com", Empresa = "Luna Design", Cargo = "Diseñador", FechaNacimiento = new DateOnly(1990, 8, 22), Notas = "Cliente frecuente." },
            new Contacto { Nombre = "Elena", Apellido = "Morales", Telefono = "381-455-5150", Email = "elena.morales@example.com", Empresa = "AgroSur", Cargo = "Gerente", Direccion = "Av. Belgrano 950" },
            new Contacto { Nombre = "Facundo", Apellido = "Sosa", Telefono = "381-455-6180", Email = "facundo.sosa@example.com", Empresa = "TecnoRed", Cargo = "Soporte", Direccion = "Laprida 230" },
            new Contacto { Nombre = "Gabriela", Apellido = "Vega", Telefono = "381-455-7210", Email = "gabriela.vega@example.com", Empresa = "Municipalidad", Cargo = "Administrativa", FechaNacimiento = new DateOnly(1985, 2, 14) },
            new Contacto { Nombre = "Hector", Apellido = "Silva", Telefono = "381-455-8240", Email = "hector.silva@example.com", Empresa = "Silva Hnos.", Cargo = "Ventas", Direccion = "Rivadavia 135" },
            new Contacto { Nombre = "Ines", Apellido = "Castro", Telefono = "381-455-9270", Email = "ines.castro@example.com", Empresa = "Colegio Central", Cargo = "Docente", Notas = "Llamar por la tarde." },
            new Contacto { Nombre = "Javier", Apellido = "Herrera", Telefono = "381-456-0300", Email = "javier.herrera@example.com", Empresa = "Herrera Legal", Cargo = "Abogado", Direccion = "Congreso 76" },
            new Contacto { Nombre = "Laura", Apellido = "Molina", Telefono = "381-456-1330", Email = "laura.molina@example.com", Empresa = "BioLab", Cargo = "Tecnica", FechaNacimiento = new DateOnly(1994, 6, 9) },
            new Contacto { Nombre = "Martin", Apellido = "Peralta", Telefono = "381-456-2360", Email = "martin.peralta@example.com", Empresa = "Peralta Obras", Cargo = "Arquitecto", Direccion = "Italia 520" },
            new Contacto { Nombre = "Natalia", Apellido = "Torres", Telefono = "381-456-3390", Email = "natalia.torres@example.com", Empresa = "Radio Norte", Cargo = "Productora" },
            new Contacto { Nombre = "Oscar", Apellido = "Medina", Telefono = "381-456-4420", Email = "oscar.medina@example.com", Empresa = "Medina Repuestos", Cargo = "Dueño", Direccion = "Av. Alem 760" },
            new Contacto { Nombre = "Paula", Apellido = "Farias", Telefono = "381-456-5450", Email = "paula.farias@example.com", Empresa = "Farias Eventos", Cargo = "Coordinadora", Notas = "Enviar presupuestos por correo." },
            new Contacto { Nombre = "Ramiro", Apellido = "Acosta", Telefono = "381-456-6480", Email = "ramiro.acosta@example.com", Empresa = "Logistica NOA", Cargo = "Chofer" },
            new Contacto { Nombre = "Sofia", Apellido = "Benitez", Telefono = "381-456-7510", Email = "sofia.benitez@example.com", Empresa = "Tienda Sol", Cargo = "Encargada", FechaNacimiento = new DateOnly(1998, 12, 1) },
            new Contacto { Nombre = "Tomas", Apellido = "Núñez", Telefono = "381-456-8540", Email = "tomas.nunez@example.com", Empresa = "Independiente", Cargo = "Programador", Direccion = "Chile 99" },
            new Contacto { Nombre = "Valeria", Apellido = "Ortega", Telefono = "381-456-9570", Email = "valeria.ortega@example.com", Empresa = "Ortega Salud", Cargo = "Medica" },
            new Contacto { Nombre = "Walter", Apellido = "Quiroga", Telefono = "381-457-0600", Email = "walter.quiroga@example.com", Empresa = "Metalurgica Q", Cargo = "Supervisor", Direccion = "Ruta 9 Km 1301" }
        );

        await db.SaveChangesAsync();
    }
}
