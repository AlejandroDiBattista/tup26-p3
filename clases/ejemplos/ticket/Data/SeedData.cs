using Agenda.Domain;
using Agenda.Security;

namespace Agenda.Data;

public static class SeedData {
    public static async Task SembrarAsync(TicketContext context) {
        if (context.Usuarios.Any()) {
            return;
        }

        var ana   = CrearUsuario("Soporte Ana",   "ana@empresa.com",   "clave1234", TipoUsuario.Interno);
        var beto  = CrearUsuario("Soporte Beto",  "beto@empresa.com",  "clave1234", TipoUsuario.Interno);
        var clara = CrearUsuario("Clara Cliente", "clara@cliente.com", "clave5678", TipoUsuario.Cliente);
        var diego = CrearUsuario("Diego Cliente", "diego@cliente.com", "clave5678", TipoUsuario.Cliente);
        var elsa  = CrearUsuario("Elsa Cliente",  "elsa@cliente.com",  "clave5678", TipoUsuario.Cliente);

        await context.Usuarios.AddRangeAsync(ana, beto, clara, diego, elsa);
        await context.SaveChangesAsync();

        var hoy = DateTime.Now;

        var ticketCerrado = CrearTicket(
            "No llegan los mails de aviso",
            "Deje de recibir notificaciones por correo",
            clara,
            ana,
            EstadoTicket.Cerrado,
            hoy.AddDays(-10));

        var ticketEnProceso1 = CrearTicket(
            "La factura sale con el total mal",
            "El IVA se calcula sobre el monto equivocado",
            diego,
            beto,
            EstadoTicket.EnProceso,
            hoy.AddDays(-3));

        var ticketEnProceso2 = CrearTicket(
            "La app se cierra al exportar PDF",
            "Al tocar Exportar la pantalla se va a blanco",
            elsa,
            ana,
            EstadoTicket.EnProceso,
            hoy.AddDays(-1));

        var ticketAbierto = CrearTicket(
            "Quiero cambiar mi correo de contacto",
            "Necesito actualizar el email de mi cuenta",
            clara,
            null,
            EstadoTicket.Abierto,
            hoy.AddHours(-2));

        await context.Tickets.AddRangeAsync(ticketCerrado, ticketEnProceso1, ticketEnProceso2, ticketAbierto);
        await context.SaveChangesAsync();

        var acciones = new List<Accion> {
            CrearAccion(ticketCerrado, "Reproduje el problema en el servidor de prueba", ana, hoy.AddDays(-9)),
            CrearAccion(ticketCerrado, "Detecte el filtro de spam mal configurado", ana, hoy.AddDays(-8)),
            CrearAccion(ticketCerrado, "Aplique el fix y confirme con la clienta", ana, hoy.AddDays(-7)),
            CrearAccion(ticketEnProceso1, "Revise la formula de calculo del IVA", beto, hoy.AddDays(-2)),
            CrearAccion(ticketEnProceso1, "Reunion con contabilidad para validar la alicuota", beto, hoy.AddDays(2)),
            CrearAccion(ticketEnProceso2, "Analizar los logs de la exportacion", ana, hoy.AddDays(1)),
            CrearAccion(ticketAbierto, "Llamar a la clienta para verificar identidad", ana, hoy.AddDays(1))
        };

        await context.Acciones.AddRangeAsync(acciones);
        await context.SaveChangesAsync();
    }

    private static Usuario CrearUsuario(string nombre, string email, string password, TipoUsuario tipo) {
        var (hash, salt) = PasswordHasher.Hashear(password);

        return new Usuario {
            Nombre = nombre,
            Email = email,
            Tipo = tipo,
            PasswordHash = hash,
            PasswordSalt = salt
        };
    }

    private static Ticket CrearTicket(
        string titulo,
        string descripcion,
        Usuario originadoPor,
        Usuario? responsable,
        EstadoTicket estado,
        DateTime creado) {
        return new Ticket {
            Titulo = titulo,
            Descripcion = descripcion,
            OriginadoPorId = originadoPor.Id,
            ResponsableId = responsable?.Id,
            Estado = estado,
            FechaCreacion = creado
        };
    }

    private static Accion CrearAccion(
        Ticket ticket,
        string descripcion,
        Usuario registradaPor,
        DateTime fecha) {
        return new Accion {
            TicketId = ticket.Id,
            Descripcion = descripcion,
            RegistradaPorId = registradaPor.Id,
            Fecha = fecha,
            Realizada = fecha <= DateTime.Now
        };
    }
}