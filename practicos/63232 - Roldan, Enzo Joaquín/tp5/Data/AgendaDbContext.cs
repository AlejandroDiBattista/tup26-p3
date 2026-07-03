using Microsoft.EntityFrameworkCore;
using AgendaWeb.Models;

namespace AgendaWeb.Data;

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options) { }

    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contacto>(entity =>
        {
            entity.ToTable("Contactos");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Telefono).IsRequired().HasMaxLength(50);
            entity.Property(c => c.CorreoElectronico).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Empresa).HasMaxLength(200);
            entity.Property(c => c.Cargo).HasMaxLength(100);
            entity.Property(c => c.Direccion).HasMaxLength(300);
            entity.Property(c => c.Notas).HasMaxLength(1000);
        });

        modelBuilder.Entity<Contacto>().HasData(SeedContactos());
    }

    private static List<Contacto> SeedContactos()
    {
        return new List<Contacto>
        {
            new() { Id = 1,  Nombre = "Juan",     Apellido = "Pérez",        Telefono = "11-1234-5678",  CorreoElectronico = "juan.perez@email.com",       Empresa = "TechCorp",     Cargo = "Desarrollador",      Direccion = "Av. Siempre Viva 123", FechaNacimiento = new(1990, 5, 12) },
            new() { Id = 2,  Nombre = "María",    Apellido = "González",     Telefono = "11-2345-6789",  CorreoElectronico = "maria.gonzalez@email.com",   Empresa = "DataSoft",     Cargo = "Analista",           Direccion = "Calle Falsa 456",      FechaNacimiento = new(1988, 8, 23) },
            new() { Id = 3,  Nombre = "Carlos",   Apellido = "López",        Telefono = "11-3456-7890",  CorreoElectronico = "carlos.lopez@email.com",     Empresa = "InnovaSys",    Cargo = "Gerente",            Direccion = "Belgrano 789",         FechaNacimiento = new(1975, 12, 1) },
            new() { Id = 4,  Nombre = "Ana",      Apellido = "Martínez",     Telefono = "11-4567-8901",  CorreoElectronico = "ana.martinez@email.com",    Empresa = "WebStudio",    Cargo = "Diseñadora",         Direccion = "San Martín 321",      FechaNacimiento = new(1992, 3, 15) },
            new() { Id = 5,  Nombre = "Pedro",    Apellido = "Rodríguez",    Telefono = "11-5678-9012",  CorreoElectronico = "pedro.rodriguez@email.com",  Empresa = "CloudNet",     Cargo = "Administrador",      Direccion = "Rivadavia 654",        FechaNacimiento = new(1985, 7, 9) },
            new() { Id = 6,  Nombre = "Laura",    Apellido = "Fernández",    Telefono = "11-6789-0123",  CorreoElectronico = "laura.fernandez@email.com",  Empresa = "MobileApps",   Cargo = "Project Manager",    Direccion = "Mitre 987",            FechaNacimiento = new(1991, 1, 28) },
            new() { Id = 7,  Nombre = "Diego",    Apellido = "García",       Telefono = "11-7890-1234",  CorreoElectronico = "diego.garcia@email.com",     Empresa = "SoftSolutions", Cargo = "Programador",        Direccion = "Urquiza 234",          FechaNacimiento = new(1993, 11, 5) },
            new() { Id = 8,  Nombre = "Sofía",    Apellido = "Díaz",         Telefono = "11-8901-2345",  CorreoElectronico = "sofia.diaz@email.com",       Empresa = "TechCorp",     Cargo = "QA Tester",          Direccion = "Sarmiento 567",        FechaNacimiento = new(1995, 6, 18) },
            new() { Id = 9,  Nombre = "Luis",     Apellido = "Torres",       Telefono = "11-9012-3456",  CorreoElectronico = "luis.torres@email.com",      Empresa = "DataSoft",     Cargo = "DBA",                Direccion = "Moreno 890",           FechaNacimiento = new(1982, 4, 3) },
            new() { Id = 10, Nombre = "Valentina",Apellido = "Ramírez",      Telefono = "11-0123-4567",  CorreoElectronico = "valentina.ramirez@email.com",Empresa = "InnovaSys",    Cargo = "UX Designer",        Direccion = "Pueyrredón 123",      FechaNacimiento = new(1994, 9, 22) },
            new() { Id = 11, Nombre = "Martín",   Apellido = "Álvarez",      Telefono = "11-1111-2222",  CorreoElectronico = "martin.alvarez@email.com",   Empresa = "WebStudio",    Cargo = "Frontend Dev",       Direccion = "Alvear 456",           FechaNacimiento = new(1987, 2, 14) },
            new() { Id = 12, Nombre = "Camila",   Apellido = "Suárez",       Telefono = "11-2222-3333",  CorreoElectronico = "camila.suarez@email.com",    Empresa = "CloudNet",     Cargo = "Backend Dev",        Direccion = "Colón 789",            FechaNacimiento = new(1996, 10, 31) },
            new() { Id = 13, Nombre = "Federico", Apellido = "Romero",       Telefono = "11-3333-4444",  CorreoElectronico = "federico.romero@email.com",  Empresa = "MobileApps",   Cargo = "DevOps",             Direccion = "Saavedra 321",         FechaNacimiento = new(1989, 7, 7) },
            new() { Id = 14, Nombre = "Florencia",Apellido = "Morales",      Telefono = "11-4444-5555",  CorreoElectronico = "florencia.morales@email.com",Empresa = "SoftSolutions", Cargo = "Scrum Master",       Direccion = "Laprida 654",          FechaNacimiento = new(1990, 12, 25) },
            new() { Id = 15, Nombre = "Pablo",    Apellido = "Castillo",     Telefono = "11-5555-6666",  CorreoElectronico = "pablo.castillo@email.com",   Empresa = "TechCorp",     Cargo = "Arquitecto",         Direccion = "9 de Julio 987",       FechaNacimiento = new(1980, 8, 8) },
            new() { Id = 16, Nombre = "Lucía",    Apellido = "Ortiz",        Telefono = "11-6666-7777",  CorreoElectronico = "lucia.ortiz@email.com",      Empresa = "DataSoft",     Cargo = "Tester",             Direccion = "Corrientes 234",       FechaNacimiento = new(1997, 1, 12) },
            new() { Id = 17, Nombre = "Gabriel",  Apellido = "Mendoza",      Telefono = "11-7777-8888",  CorreoElectronico = "gabriel.mendoza@email.com",   Empresa = "InnovaSys",    Cargo = "Soporte Técnico",    Direccion = "Córdoba 567",          FechaNacimiento = new(1986, 5, 30) },
            new() { Id = 18, Nombre = "Elena",    Apellido = "Rivas",        Telefono = "11-8888-9999",  CorreoElectronico = "elena.rivas@email.com",       Empresa = "WebStudio",    Cargo = "Content Manager",    Direccion = "Santa Fe 890",         FechaNacimiento = new(1993, 3, 3) },
            new() { Id = 19, Nombre = "Hugo",     Apellido = "Pereyra",      Telefono = "11-9999-0000",  CorreoElectronico = "hugo.pereyra@email.com",      Empresa = "CloudNet",     Cargo = "Security Analyst",   Direccion = "Salta 123",            FechaNacimiento = new(1984, 9, 19) },
            new() { Id = 20, Nombre = "Rocío",    Apellido = "Medina",       Telefono = "11-0000-1111",  CorreoElectronico = "rocio.medina@email.com",      Empresa = "MobileApps",   Cargo = "Marketing Digital",  Direccion = "Tucumán 456",          FechaNacimiento = new(1995, 6, 14) },
        };
    }
}
