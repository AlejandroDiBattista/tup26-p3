using Microsoft.EntityFrameworkCore;
using razor.Data;
using razor.Models;

namespace razor.Repositories;

public class ContactoRepository : IContactoRepository {
    private readonly AgendaDbContext context;

    public ContactoRepository(AgendaDbContext context) {
        this.context = context;
    }

    public Task<List<Contacto>> ObtenerTodosAsync() {
        return context.Contactos
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync();
    }

    public Task<Contacto?> ObtenerPorIdAsync(int id) {
        return context.Contactos.FindAsync(id).AsTask();
    }

    public async Task CrearAsync(Contacto contacto) {
        context.Contactos.Add(contacto);
        await context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Contacto contacto) {
        context.Contactos.Update(contacto);
        await context.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id) {
        Contacto? contacto = await ObtenerPorIdAsync(id);

        if (contacto is null) {
            return;
        }

        context.Contactos.Remove(contacto);
        await context.SaveChangesAsync();
    }
}
