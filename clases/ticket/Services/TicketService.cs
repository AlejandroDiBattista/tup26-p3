using Agenda.Domain;
using Agenda.Repositories;

namespace Agenda.Services;

public class TicketService {
    private readonly ITicketRepository repositorio;

    public TicketService(ITicketRepository repositorio) {
        this.repositorio = repositorio;
    }

    public async Task<Ticket> CrearTicketAsync(
        string titulo,
        string? descripcion,
        int originadoPorId,
        int? responsableId = null) {
        var ticket = new Ticket {
            Titulo = titulo,
            Descripcion = descripcion,
            OriginadoPorId = originadoPorId,
            ResponsableId = responsableId,
            Estado = EstadoTicket.Abierto,
            FechaCreacion = DateTime.Now
        };

        await this.repositorio.AgregarAsync(ticket);
        await this.repositorio.GuardarCambiosAsync();
        return ticket;
    }

    public async Task<Ticket?> ObtenerTicketAsync(int id) {
        return await this.repositorio.ObtenerPorIdAsync(id);
    }

    public async Task<IEnumerable<Ticket>> ListarTicketsAsync() {
        return await this.repositorio.ObtenerTodosAsync();
    }

    public async Task<IEnumerable<Ticket>> ListarPorEstadoAsync(EstadoTicket estado) {
        return await this.repositorio.ObtenerPorEstadoAsync(estado);
    }

    public async Task CambiarEstadoAsync(int ticketId, EstadoTicket nuevoEstado) {
        var ticket = await this.repositorio.ObtenerPorIdAsync(ticketId)
        ?? throw new InvalidOperationException($"No existe el ticket {ticketId}.");
        
        ticket.Estado = nuevoEstado;
        await this.repositorio.GuardarCambiosAsync();
    }

    public async Task AsignarResponsableAsync(int ticketId, int responsableId) {
        var ticket = await this.repositorio.ObtenerPorIdAsync(ticketId)
            ?? throw new InvalidOperationException($"No existe el ticket {ticketId}.");

        ticket.ResponsableId = responsableId;
        await this.repositorio.GuardarCambiosAsync();
    }

    public async Task<Accion> RegistrarAccionAsync(
        int ticketId,
        string descripcion,
        int registradaPorId,
        DateTime fecha) {
        _ = await this.repositorio.ObtenerPorIdAsync(ticketId)
            ?? throw new InvalidOperationException($"No existe el ticket {ticketId}.");

        var accion = new Accion {
            TicketId = ticketId,
            Descripcion = descripcion,
            RegistradaPorId = registradaPorId,
            Fecha = fecha,
            Realizada = fecha <= DateTime.Now
        };

        await this.repositorio.AgregarAccionAsync(accion);
        await this.repositorio.GuardarCambiosAsync();
        return accion;
    }

    public async Task MarcarAccionRealizadaAsync(int ticketId, int accionId) {
        var ticket = await this.repositorio.ObtenerPorIdAsync(ticketId)
            ?? throw new InvalidOperationException($"No existe el ticket {ticketId}.");

        var accion = ticket.Acciones.FirstOrDefault(item => item.Id == accionId)
            ?? throw new InvalidOperationException($"No existe la accion {accionId} en el ticket.");

        accion.Realizada = true;
        await this.repositorio.GuardarCambiosAsync();
    }
}