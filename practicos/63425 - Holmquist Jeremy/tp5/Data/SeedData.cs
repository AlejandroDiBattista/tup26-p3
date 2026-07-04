using tp5.Models;

namespace tp5.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Contactos.Any()) return;

            var contactos = new List<Contacto>
            {
                new() { Nombre = "Carlos", Apellido = "López", Telefono = "11-2345-6789", CorreoElectronico = "carlos.lopez@email.com", Empresa = "Tech Solutions", Cargo = "Desarrollador Senior", Direccion = "Av. Corrientes 1234, CABA", FechaNacimiento = new DateOnly(1985, 3, 15), Notas = "Especialista en .NET" },
                new() { Nombre = "María", Apellido = "García", Telefono = "11-3456-7890", CorreoElectronico = "maria.garcia@email.com", Empresa = "Design Studio", Cargo = "Diseñadora UX", Direccion = "Calle Florida 567, CABA", FechaNacimiento = new DateOnly(1990, 7, 22), Notas = "Trabaja remoto" },
                new() { Nombre = "Juan", Apellido = "Martínez", Telefono = "11-4567-8901", CorreoElectronico = "juan.martinez@email.com", Empresa = "DataCorp", Cargo = "Analista de Datos", Direccion = "Av. Libertador 890, CABA", FechaNacimiento = new DateOnly(1988, 11, 8), Notas = "" },
                new() { Nombre = "Ana", Apellido = "Rodríguez", Telefono = "11-5678-9012", CorreoElectronico = "ana.rodriguez@email.com", Empresa = "HealthPlus", Cargo = "Médica", Direccion = "Calle Belgrano 345, CABA", FechaNacimiento = new DateOnly(1982, 5, 30), Notas = "Disponible los lunes" },
                new() { Nombre = "Pedro", Apellido = "Fernández", Telefono = "11-6789-0123", CorreoElectronico = "pedro.fernandez@email.com", Empresa = "EduTech", Cargo = "Profesor", Direccion = "Av. Rivadavia 123, CABA", FechaNacimiento = new DateOnly(1979, 9, 12), Notas = "" },
                new() { Nombre = "Laura", Apellido = "Díaz", Telefono = "11-7890-1234", CorreoElectronico = "laura.diaz@email.com", Empresa = "FinTrust", Cargo = "Contadora", Direccion = "Calle San Martín 678, CABA", FechaNacimiento = new DateOnly(1992, 1, 25), Notas = "Cliente preferencial" },
                new() { Nombre = "Diego", Apellido = "Pérez", Telefono = "11-8901-2345", CorreoElectronico = "diego.perez@email.com", Empresa = "AutoParts", Cargo = "Gerente de Ventas", Direccion = "Av. Alem 456, CABA", FechaNacimiento = new DateOnly(1986, 8, 3), Notas = "" },
                new() { Nombre = "Sofía", Apellido = "González", Telefono = "11-9012-3456", CorreoElectronico = "sofia.gonzalez@email.com", Empresa = "Media Group", Cargo = "Periodista", Direccion = "Calle Tucumán 789, CABA", FechaNacimiento = new DateOnly(1994, 4, 18), Notas = "Contacto de prensa" },
                new() { Nombre = "Roberto", Apellido = "Álvarez", Telefono = "11-0123-4567", CorreoElectronico = "roberto.alvarez@email.com", Empresa = "ConstruCorp", Cargo = "Ingeniero Civil", Direccion = "Av. Santa Fe 234, CABA", FechaNacimiento = new DateOnly(1980, 12, 1), Notas = "" },
                new() { Nombre = "Valentina", Apellido = "Moreno", Telefono = "11-1234-5678", CorreoElectronico = "valentina.moreno@email.com", Empresa = "Foodies", Cargo = "Chef Ejecutiva", Direccion = "Calle Defensa 567, CABA", FechaNacimiento = new DateOnly(1991, 6, 14), Notas = "Proveedora de catering" },
                new() { Nombre = "Martín", Apellido = "Sánchez", Telefono = "11-2345-6780", CorreoElectronico = "martin.sanchez@email.com", Empresa = "CloudNet", Cargo = "Arquitecto de Software", Direccion = "Av. Callao 890, CABA", FechaNacimiento = new DateOnly(1987, 2, 28), Notas = "Especialista en cloud" },
                new() { Nombre = "Camila", Apellido = "Romero", Telefono = "11-3456-7891", CorreoElectronico = "camila.romero@email.com", Empresa = "GreenEnergy", Cargo = "Ingeniera Ambiental", Direccion = "Calle Perú 123, CABA", FechaNacimiento = new DateOnly(1993, 10, 5), Notas = "" },
                new() { Nombre = "Fernando", Apellido = "Torres", Telefono = "11-4567-8902", CorreoElectronico = "fernando.torres@email.com", Empresa = "LogiTech", Cargo = "Logístico", Direccion = "Av. Independencia 456, CABA", FechaNacimiento = new DateOnly(1984, 7, 19), Notas = "Horario flexible" },
                new() { Nombre = "Lucía", Apellido = "Herrera", Telefono = "11-5678-9013", CorreoElectronico = "lucia.herrera@email.com", Empresa = "Fashion Now", Cargo = "Diseñadora de Moda", Direccion = "Calle Lavalle 789, CABA", FechaNacimiento = new DateOnly(1995, 3, 8), Notas = "" },
                new() { Nombre = "Gustavo", Apellido = "Medina", Telefono = "11-6789-0124", CorreoElectronico = "gustavo.medina@email.com", Empresa = "Segurix", Cargo = "Analista de Seguridad", Direccion = "Av. Pueyrredón 234, CABA", FechaNacimiento = new DateOnly(1983, 9, 21), Notas = "Contacto de emergencia" },
                new() { Nombre = "Florencia", Apellido = "Castillo", Telefono = "11-7890-1235", CorreoElectronico = "florencia.castillo@email.com", Empresa = "LegalPro", Cargo = "Abogada", Direccion = "Calle Viamonte 567, CABA", FechaNacimiento = new DateOnly(1989, 12, 12), Notas = "" },
                new() { Nombre = "Andrés", Apellido = "Ramos", Telefono = "11-8901-2346", CorreoElectronico = "andres.ramos@email.com", Empresa = "MarketPlace", Cargo = "Community Manager", Direccion = "Av. Córdoba 890, CABA", FechaNacimiento = new DateOnly(1996, 5, 1), Notas = "Redes sociales" },
                new() { Nombre = "Paula", Apellido = "Ortiz", Telefono = "11-9012-3457", CorreoElectronico = "paula.ortiz@email.com", Empresa = "TravelWorld", Cargo = "Agente de Viajes", Direccion = "Calle Esmeralda 123, CABA", FechaNacimiento = new DateOnly(1981, 8, 16), Notas = "Ofrece descuentos" },
                new() { Nombre = "Santiago", Apellido = "Flores", Telefono = "11-0123-4568", CorreoElectronico = "santiago.flores@email.com", Empresa = "AgroTech", Cargo = "Ingeniero Agrónomo", Direccion = "Av. Del Libertador 456, CABA", FechaNacimiento = new DateOnly(1978, 4, 9), Notas = "Proveedor de insumos" },
                new() { Nombre = "Elena", Apellido = "Vargas", Telefono = "11-1234-5679", CorreoElectronico = "elena.vargas@email.com", Empresa = "PharmaCorp", Cargo = "Farmacéutica", Direccion = "Calle Riobamba 789, CABA", FechaNacimiento = new DateOnly(1987, 11, 23), Notas = "" }
            };

            context.Contactos.AddRange(contactos);
            context.SaveChanges();
        }
    }
}
