using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService
{
    private readonly AgendaDbContext _context;

    public ContactoService(AgendaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Contacto>> GetContactosAsync(string terminoBusqueda)
    {
        var query = _context.Contactos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            terminoBusqueda = terminoBusqueda.Trim();
            query = query.Where(c => c.Nombre.Contains(terminoBusqueda)
                || c.Apellido.Contains(terminoBusqueda)
                || c.CorreoElectronico.Contains(terminoBusqueda));
        }

        return await query.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToListAsync();
    }

    public async Task AddContactoAsync(Contacto contacto)
    {
        _context.Contactos.Add(contacto);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateContactoAsync(Contacto contacto)
    {
        _context.Contactos.Update(contacto);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteContactoAsync(int id)
    {
        var entity = await _context.Contactos.FindAsync(id);
        if (entity != null)
        {
            _context.Contactos.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
