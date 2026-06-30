using tp5.Models;
using Microsoft.EntityFrameworkCore;

namespace tp5.Data;

public class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options)
{
    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contacto>(entity =>
        {
            entity.Property(c => c.Nombre).IsRequired();
            entity.Property(c => c.Apellido).IsRequired();
            entity.Property(c => c.Telefono).IsRequired();
            entity.Property(c => c.CorreoElectronico).IsRequired();

            entity.HasData(
                new Contacto { Id = 1, Nombre = "Juan", Apellido = "Perez", Telefono = "381 555-1234", CorreoElectronico = "juan.perez@acme.com", Empresa = "Acme S.A.", Cargo = "Gerente de Ventas", Direccion = "San Martin 123, San Miguel de Tucuman", FechaNacimiento = new DateTime(1990, 5, 14), Notas = "Cliente frecuente. Prefiere contacto por WhatsApp." },
                new Contacto { Id = 2, Nombre = "Ana", Apellido = "Gomez", Telefono = "381 555-2345", CorreoElectronico = "ana.gomez@globant.com", Empresa = "Globant", Cargo = "Analista", Direccion = "25 de Mayo 456, San Miguel de Tucuman", FechaNacimiento = new DateTime(1994, 8, 20), Notas = "Enviar novedades por correo." },
                new Contacto { Id = 3, Nombre = "Carlos", Apellido = "Medina", Telefono = "381 555-3456", CorreoElectronico = "carlos.medina@mail.com", Empresa = "Independiente", Cargo = "Tecnico", Direccion = "Rivadavia 650, Yerba Buena", FechaNacimiento = new DateTime(1988, 11, 3), Notas = "Disponible por la tarde." },
                new Contacto { Id = 4, Nombre = "Laura", Apellido = "Torres", Telefono = "381 555-4567", CorreoElectronico = "laura.torres@correo.com", Empresa = "Norte SRL", Cargo = "Administrativa", Direccion = "Belgrano 320, Tafi Viejo", FechaNacimiento = new DateTime(1992, 2, 10), Notas = "Confirmar reuniones con anticipacion." },
                new Contacto { Id = 5, Nombre = "Martin", Apellido = "Diaz", Telefono = "381 555-5678", CorreoElectronico = "martin.diaz@estudio.com", Empresa = "Estudio Juridico Diaz", Cargo = "Abogado", Direccion = "Salta 980, San Miguel de Tucuman", FechaNacimiento = new DateTime(1985, 7, 27), Notas = "Contacto laboral." },
                new Contacto { Id = 6, Nombre = "Sofia", Apellido = "Romano", Telefono = "381 555-6789", CorreoElectronico = "sofia.romano@consultora.com", Empresa = "Consultora SRL", Cargo = "Recruiter", Direccion = "Laprida 112, San Miguel de Tucuman", FechaNacimiento = new DateTime(1996, 4, 5), Notas = "Enviar CV actualizado." },
                new Contacto { Id = 7, Nombre = "Diego", Apellido = "Salas", Telefono = "381 555-7890", CorreoElectronico = "diego.salas@soluciones.com", Empresa = "Soluciones Informaticas", Cargo = "Programador", Direccion = "Chile 700, San Miguel de Tucuman", FechaNacimiento = new DateTime(1991, 12, 18), Notas = "Trabaja remoto." },
                new Contacto { Id = 8, Nombre = "Valeria", Apellido = "Nunez", Telefono = "381 555-8901", CorreoElectronico = "valeria.nunez@mail.com", Empresa = "Independiente", Cargo = "Disenadora", Direccion = "Aconquija 1450, Yerba Buena", FechaNacimiento = new DateTime(1993, 9, 9), Notas = "Mandar referencias visuales." },
                new Contacto { Id = 9, Nombre = "Pablo", Apellido = "Herrera", Telefono = "381 555-9012", CorreoElectronico = "pablo.herrera@herrera.com", Empresa = "Herrera Tech", Cargo = "Soporte", Direccion = "Mate de Luna 2400, San Miguel de Tucuman", FechaNacimiento = new DateTime(1989, 1, 30), Notas = "Llamar despues de las 10." },
                new Contacto { Id = 10, Nombre = "Micaela", Apellido = "Funes", Telefono = "381 555-1122", CorreoElectronico = "micaela.funes@facturas.com", Empresa = "Funes Facturacion", Cargo = "Contadora", Direccion = "Cordoba 410, San Miguel de Tucuman", FechaNacimiento = new DateTime(1995, 6, 22), Notas = "Tiene documentacion pendiente." },
                new Contacto { Id = 11, Nombre = "Ricardo", Apellido = "Leiva", Telefono = "381 555-2233", CorreoElectronico = "ricardo.leiva@mail.com", Empresa = "Leiva Construcciones", Cargo = "Arquitecto", Direccion = "Maipu 850, San Miguel de Tucuman", FechaNacimiento = new DateTime(1982, 10, 15), Notas = "Revisar presupuesto." },
                new Contacto { Id = 12, Nombre = "Camila", Apellido = "Arias", Telefono = "381 555-3344", CorreoElectronico = "camila.arias@salud.com", Empresa = "Clinica Norte", Cargo = "Medica", Direccion = "Santiago 520, San Miguel de Tucuman", FechaNacimiento = new DateTime(1990, 3, 12), Notas = "Atiende por la manana." },
                new Contacto { Id = 13, Nombre = "Federico", Apellido = "Molina", Telefono = "381 555-4455", CorreoElectronico = "federico.molina@ventas.com", Empresa = "Ventas NOA", Cargo = "Vendedor", Direccion = "Jujuy 230, San Miguel de Tucuman", FechaNacimiento = new DateTime(1987, 5, 8), Notas = "Pedir lista de precios." },
                new Contacto { Id = 14, Nombre = "Luciana", Apellido = "Rojas", Telefono = "381 555-5566", CorreoElectronico = "luciana.rojas@marketing.com", Empresa = "Rojas Marketing", Cargo = "Community Manager", Direccion = "Mendoza 1010, San Miguel de Tucuman", FechaNacimiento = new DateTime(1997, 11, 25), Notas = "Coordinar publicaciones." },
                new Contacto { Id = 15, Nombre = "Esteban", Apellido = "Vega", Telefono = "381 555-6677", CorreoElectronico = "esteban.vega@transporte.com", Empresa = "Transporte Vega", Cargo = "Chofer", Direccion = "Av. Alem 1500, San Miguel de Tucuman", FechaNacimiento = new DateTime(1984, 2, 19), Notas = "Tiene disponibilidad los viernes." },
                new Contacto { Id = 16, Nombre = "Natalia", Apellido = "Campos", Telefono = "381 555-7788", CorreoElectronico = "natalia.campos@educacion.com", Empresa = "Instituto Central", Cargo = "Docente", Direccion = "Junin 740, San Miguel de Tucuman", FechaNacimiento = new DateTime(1992, 8, 2), Notas = "Enviar material de clase." },
                new Contacto { Id = 17, Nombre = "Gaston", Apellido = "Paz", Telefono = "381 555-8899", CorreoElectronico = "gaston.paz@correo.com", Empresa = "Paz Repuestos", Cargo = "Encargado", Direccion = "Av. Colon 900, San Miguel de Tucuman", FechaNacimiento = new DateTime(1986, 4, 28), Notas = "Consultar stock." },
                new Contacto { Id = 18, Nombre = "Florencia", Apellido = "Castillo", Telefono = "381 555-9900", CorreoElectronico = "florencia.castillo@eventos.com", Empresa = "Castillo Eventos", Cargo = "Organizadora", Direccion = "Italia 670, Yerba Buena", FechaNacimiento = new DateTime(1998, 1, 6), Notas = "Confirmar salon." },
                new Contacto { Id = 19, Nombre = "Mariano", Apellido = "Sosa", Telefono = "381 555-1010", CorreoElectronico = "mariano.sosa@correo.com", Empresa = "Sosa Servicios", Cargo = "Tecnico", Direccion = "San Juan 345, San Miguel de Tucuman", FechaNacimiento = new DateTime(1991, 7, 17), Notas = "Pendiente de visita." },
                new Contacto { Id = 20, Nombre = "Julieta", Apellido = "Acosta", Telefono = "381 555-2020", CorreoElectronico = "julieta.acosta@correo.com", Empresa = "Acosta Studio", Cargo = "Fotografa", Direccion = "Buenos Aires 1250, San Miguel de Tucuman", FechaNacimiento = new DateTime(1999, 12, 1), Notas = "Enviar contrato." }
            );
        });
    }
}
