using Microsoft.EntityFrameworkCore;
using tp5.Models;
using AgendaWeb.Data;

namespace AgendaWeb.Services;

public class ContactoService {
    private readonly AgendaContext _context;

    public ContactoService(AgendaContext context) {
        _context = context;
    }

/*obtener contactos*/
    public async Task<List<Contacto>> ObtenerTodosAsync() {
        return await _context.Contactos
            .OrderBy(c => c.Apellido)
            .ThenBy( c => c.Nombre)
            .ToListAsync();
    }

    /*Obtener contacto por ID*/
    public async Task<Contacto?> ObtenerPorIdAsync(int id) {
        return await _context.Contactos.FindAsync(id);
    }

    /*Crear contacto nuevo*/
    public async Task<Contacto> CrearAsync(Contacto contacto) {
        _context.Contactos.Add(contacto);
        await _context.SaveChangesAsync();
        return contacto;
    }

    /*Actualizar contacto*/
    public async Task<bool> ActualizarAsync(Contacto contacto) {
        _context.Contactos.Update(contacto);
        var cambios = await _context.SaveChangesAsync();
        return cambios > 0;
    }
    /*Eliminar contacto*/
    public async Task<bool> EliminarAsync(int id) {
        var contacto = await _context.Contactos.FindAsync(id);
        if (contacto == null) return false;

        _context.Contactos.Remove(contacto);
        await _context.SaveChangesAsync();
        return true;
    }

    /*Buscar contacto*/
    public async Task<List<Contacto>> BuscarAsync(string termino) {
        var terminoLower = termino.ToLower();

        return await _context.Contactos
            .Where(c => c.Nombre.ToLower().Contains(terminoLower) ||
                    c.Apellido.ToLower().Contains(terminoLower) ||
                    c.Email.ToLower().Contains(terminoLower) ||
                    c.Telefono.Contains(termino))
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .ToListAsync();
        }
    
}