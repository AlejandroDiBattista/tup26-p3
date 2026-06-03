using Microsoft.EntityFrameworkCore;
using Agenda.Data;
using Agenda.Domain;

namespace Agenda.Repositories;

public class TicketRepository : ITicketRepository {
    private readonly TicketContext context;

    public TicketRepository(TicketContext context) {
        this.context = context;
    }

    public async Task<Ticket?> ObtenerPorIdAsync(int id) {
        return await this.context.Tickets
            .Include(ticket => ticket.OriginadoPor)
            .Include(ticket => ticket.Responsable)
            .Include(ticket => ticket.Acciones)
                .ThenInclude(accion => accion.RegistradaPor)
            .FirstOrDefaultAsync(ticket => ticket.Id == id);
    }

    public async Task<IEnumerable<Ticket>> ObtenerTodosAsync() {
        return await this.context.Tickets
            .Include(ticket => ticket.OriginadoPor)
            .Include(ticket => ticket.Responsable)
            .Include(ticket => ticket.Acciones)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> ObtenerPorEstadoAsync(EstadoTicket estado) {
        return await this.context.Tickets
            .Include(ticket => ticket.OriginadoPor)
            .Include(ticket => ticket.Responsable)
            .Include(ticket => ticket.Acciones)
            .Where(ticket => ticket.Estado == estado)
            .ToListAsync();
    }

    public async Task AgregarAsync(Ticket ticket) {
        await this.context.Tickets.AddAsync(ticket);
    }

    public async Task AgregarAccionAsync(Accion accion) {
        await this.context.Acciones.AddAsync(accion);
    }

    public async Task GuardarCambiosAsync() {
        await this.context.SaveChangesAsync();
    }
}