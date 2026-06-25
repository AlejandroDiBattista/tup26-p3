using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Data;

public static class DatosIniciales
{
    public static async Task CargarContactos(AgendaContext contexto)
    {
        var yaEstanCargados = await contexto.Contactos
            .AnyAsync(contacto => contacto.Nombre == "Maxi" && contacto.Apellido == "Jimenez");

        if (yaEstanCargados)
        {
            return;
        }

        var contactosViejos = await contexto.Contactos.ToListAsync();
        contexto.Contactos.RemoveRange(contactosViejos);

        var contactosNuevos = new List<Contacto>
        {
            CrearContacto("Maxi", "Jimenez", "381 612-9401", "maxi.jimenez@mail.com", "Mundo Repuestos", "Vendedor", "Av. Belgrano 1450", new DateOnly(2001, 4, 18), "Prefiere mensajes por WhatsApp."),
            CrearContacto("Lucas", "Chavez", "381 455-7812", "lucas.chavez@mail.com", "Chavez Tech", "Soporte tecnico", "San Martin 230", new DateOnly(2000, 9, 2), "Cliente responsable y puntual."),
            CrearContacto("Coronel", "Tomas", "381 504-3368", "coronel.tomas@mail.com", "Estudiante", "Programador inicial", "Barrio Norte 88", new DateOnly(2003, 6, 12), "Contacto principal del trabajo practico."),
            CrearContacto("Pacifico", "Nicolas", "381 730-2194", "pacifico.nicolas@mail.com", "Pacifico Servicios", "Administrativo", "Rivadavia 910", new DateOnly(1999, 12, 5), "Consultar por turnos a la tarde."),
            CrearContacto("Pereyra", "Valentina", "381 681-1470", "pereyra.valentina@mail.com", "Diseño VP", "Disenadora", "Laprida 345", new DateOnly(2002, 2, 21), "Le interesa recibir novedades por correo."),
            CrearContacto("Rosconi", "Ignacio", "381 790-6245", "rosconi.ignacio@mail.com", "Rosconi Hardware", "Tecnico", "Mate de Luna 1880", new DateOnly(1998, 7, 30), "Tiene disponibilidad por la manana."),
            CrearContacto("Emiliano", "Martinez", "381 431-9026", "emiliano.martinez@mail.com", "Deportes Sur", "Encargado", "Chile 552", new DateOnly(1992, 9, 2), "Cliente frecuente."),
            CrearContacto("Gonzalo", "Manzano", "381 612-3380", "gonzalo.manzano@mail.com", "Manzano Logistica", "Coordinador", "Italia 417", new DateOnly(1997, 11, 14), "Llamar despues de las 16 hs."),
            CrearContacto("Leo", "Messi", "381 777-1010", "leo.messi@mail.com", "Inter Miami", "Delantero", "Miami 10", new DateOnly(1987, 6, 24), "Contacto VIP de ejemplo."),
            CrearContacto("Juanfer", "Quintero", "381 650-2008", "juanfer.quintero@mail.com", "River Plate", "Mediocampista", "Monumental 10", new DateOnly(1993, 1, 18), "Enviar informacion por email."),
            CrearContacto("Pity", "Martinez", "381 699-2018", "pity.martinez@mail.com", "River Plate", "Volante", "Libertador 2018", new DateOnly(1993, 6, 13), "Anotar como contacto deportivo."),
            CrearContacto("Bruno", "Roldan", "381 533-8741", "bruno.roldan@mail.com", "Roldan Muebles", "Carpintero", "Junin 670", new DateOnly(1995, 3, 9), "Nombre inventado para completar la agenda."),
            CrearContacto("Camila", "Sosa", "381 588-4412", "camila.sosa@mail.com", "Sosa Eventos", "Organizadora", "Salta 122", new DateOnly(2001, 8, 27), "Contacto agregado como ejemplo.")
        };

        contexto.Contactos.AddRange(contactosNuevos);
        await contexto.SaveChangesAsync();
    }

    static Contacto CrearContacto(string nombre, string apellido, string telefono, string email, string empresa, string cargo, string direccion, DateOnly fechaNacimiento, string notas)
    {
        return new Contacto
        {
            Nombre = nombre,
            Apellido = apellido,
            Telefono = telefono,
            Email = email,
            Empresa = empresa,
            Cargo = cargo,
            Direccion = direccion,
            FechaNacimiento = fechaNacimiento,
            Notas = notas
        };
    }
}
