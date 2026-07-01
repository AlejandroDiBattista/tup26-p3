using Agenda.Domain;

namespace Agenda.Repositories;

public interface ITicketRepository {
    Task<Ticket?> ObtenerPorIdAsync(int id);
    Task<IEnumerable<Ticket>> ObtenerTodosAsync();
    Task<IEnumerable<Ticket>> ObtenerPorEstadoAsync(EstadoTicket estado);
    Task AgregarAsync(Ticket ticket);
    Task AgregarAccionAsync(Accion accion);
    Task GuardarCambiosAsync();
}