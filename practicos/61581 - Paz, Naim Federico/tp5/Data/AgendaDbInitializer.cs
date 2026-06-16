using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public static class AgendaDbInitializer
{
    public static async Task InitializeAsync(IDbContextFactory<AgendaDbContext> dbContextFactory)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        await db.Database.EnsureCreatedAsync();

        if (await db.Contactos.AnyAsync())
        {
            return;
        }

        db.Contactos.AddRange(
            new Contacto
            {
                Nombre = "Ana",
                Apellido = "Lopez",
                Telefono = "2664-111111",
                Email = "ana.lopez@mail.com",
                Empresa = "Soluciones Centro",
                Cargo = "Administrativa",
                Direccion = "Mitre 120",
                FechaNacimiento = new DateOnly(1995, 4, 12),
                Notas = "Prefiere contacto por correo."
            },
            new Contacto
            {
                Nombre = "Bruno",
                Apellido = "Martinez",
                Telefono = "2664-222222",
                Email = "bruno.martinez@mail.com",
                Empresa = "Andes Tech",
                Cargo = "Soporte",
                Direccion = "San Martin 450",
                FechaNacimiento = new DateOnly(1992, 8, 3),
                Notas = "Disponible por la tarde."
            },
            new Contacto
            {
                Nombre = "Camila",
                Apellido = "Rojas",
                Telefono = "2664-333333",
                Email = "camila.rojas@mail.com",
                Empresa = "Estudio Norte",
                Cargo = "Disenadora",
                Direccion = "Belgrano 88",
                FechaNacimiento = new DateOnly(1998, 1, 21)
            },
            new Contacto
            {
                Nombre = "Diego",
                Apellido = "Fernandez",
                Telefono = "2664-444444",
                Email = "diego.fernandez@mail.com",
                Empresa = "Logistica Cuyo",
                Cargo = "Coordinador",
                Direccion = "Colon 305",
                FechaNacimiento = new DateOnly(1989, 11, 9)
            },
            new Contacto
            {
                Nombre = "Elena",
                Apellido = "Gomez",
                Telefono = "2664-555555",
                Email = "elena.gomez@mail.com",
                Empresa = "Clinica Sur",
                Cargo = "Recepcionista",
                Direccion = "Rivadavia 740",
                FechaNacimiento = new DateOnly(1990, 7, 14)
            },
            new Contacto
            {
                Nombre = "Facundo",
                Apellido = "Sosa",
                Telefono = "2664-666666",
                Email = "facundo.sosa@mail.com",
                Empresa = "Taller Oeste",
                Cargo = "Encargado",
                Direccion = "Pringles 190",
                FechaNacimiento = new DateOnly(1987, 2, 28)
            },
            new Contacto
            {
                Nombre = "Gabriela",
                Apellido = "Quiroga",
                Telefono = "2664-777777",
                Email = "gabriela.quiroga@mail.com",
                Empresa = "Colegio Norte",
                Cargo = "Docente",
                Direccion = "Lafinur 510",
                FechaNacimiento = new DateOnly(1993, 12, 5)
            },
            new Contacto
            {
                Nombre = "Hernan",
                Apellido = "Castro",
                Telefono = "2664-888888",
                Email = "hernan.castro@mail.com",
                Empresa = "Municipalidad",
                Cargo = "Tecnico",
                Direccion = "Junin 22",
                FechaNacimiento = new DateOnly(1985, 5, 17)
            },
            new Contacto
            {
                Nombre = "Ivana",
                Apellido = "Pereyra",
                Telefono = "2664-999999",
                Email = "ivana.pereyra@mail.com",
                Empresa = "Farmacia Central",
                Cargo = "Farmaceutica",
                Direccion = "Pedernera 333",
                FechaNacimiento = new DateOnly(1991, 9, 30)
            },
            new Contacto
            {
                Nombre = "Joaquin",
                Apellido = "Molina",
                Telefono = "2664-101010",
                Email = "joaquin.molina@mail.com",
                Empresa = "Mercado Sur",
                Cargo = "Vendedor",
                Direccion = "Chacabuco 661",
                FechaNacimiento = new DateOnly(1999, 6, 11)
            },
            new Contacto
            {
                Nombre = "Karina",
                Apellido = "Diaz",
                Telefono = "2664-121212",
                Email = "karina.diaz@mail.com",
                Empresa = "Banco Regional",
                Cargo = "Analista",
                Direccion = "Ayacucho 100",
                FechaNacimiento = new DateOnly(1988, 10, 22)
            },
            new Contacto
            {
                Nombre = "Lucas",
                Apellido = "Herrera",
                Telefono = "2664-131313",
                Email = "lucas.herrera@mail.com",
                Empresa = "Red Digital",
                Cargo = "Programador",
                Direccion = "Italia 41",
                FechaNacimiento = new DateOnly(1996, 3, 7)
            },
            new Contacto
            {
                Nombre = "Marina",
                Apellido = "Ortiz",
                Telefono = "2664-141414",
                Email = "marina.ortiz@mail.com",
                Empresa = "Viajes Cuyo",
                Cargo = "Asesora",
                Direccion = "Espana 700",
                FechaNacimiento = new DateOnly(1994, 4, 25)
            },
            new Contacto
            {
                Nombre = "Nicolas",
                Apellido = "Torres",
                Telefono = "2664-151515",
                Email = "nicolas.torres@mail.com",
                Empresa = "Constructora Sur",
                Cargo = "Arquitecto",
                Direccion = "Maipu 210",
                FechaNacimiento = new DateOnly(1986, 1, 13)
            },
            new Contacto
            {
                Nombre = "Olivia",
                Apellido = "Acosta",
                Telefono = "2664-161616",
                Email = "olivia.acosta@mail.com",
                Empresa = "Libreria Plaza",
                Cargo = "Atencion al cliente",
                Direccion = "Lavalle 55",
                FechaNacimiento = new DateOnly(2000, 8, 19)
            },
            new Contacto
            {
                Nombre = "Pablo",
                Apellido = "Navarro",
                Telefono = "2664-171717",
                Email = "pablo.navarro@mail.com",
                Empresa = "Radio Local",
                Cargo = "Productor",
                Direccion = "Chile 177",
                FechaNacimiento = new DateOnly(1984, 7, 2)
            },
            new Contacto
            {
                Nombre = "Romina",
                Apellido = "Vega",
                Telefono = "2664-181818",
                Email = "romina.vega@mail.com",
                Empresa = "Hotel Centro",
                Cargo = "Gerente",
                Direccion = "Urquiza 400",
                FechaNacimiento = new DateOnly(1990, 11, 16)
            },
            new Contacto
            {
                Nombre = "Santiago",
                Apellido = "Arias",
                Telefono = "2664-191919",
                Email = "santiago.arias@mail.com",
                Empresa = "Electro Hogar",
                Cargo = "Tecnico",
                Direccion = "Sucre 93",
                FechaNacimiento = new DateOnly(1997, 5, 4)
            },
            new Contacto
            {
                Nombre = "Tatiana",
                Apellido = "Morales",
                Telefono = "2664-202020",
                Email = "tatiana.morales@mail.com",
                Empresa = "Consultora Uno",
                Cargo = "Contadora",
                Direccion = "Buenos Aires 812",
                FechaNacimiento = new DateOnly(1983, 12, 27)
            },
            new Contacto
            {
                Nombre = "Ulises",
                Apellido = "Benitez",
                Telefono = "2664-212121",
                Email = "ulises.benitez@mail.com",
                Empresa = "Deportes Norte",
                Cargo = "Entrenador",
                Direccion = "Falucho 64",
                FechaNacimiento = new DateOnly(1995, 9, 8)
            });

        await db.SaveChangesAsync();
    }
}
