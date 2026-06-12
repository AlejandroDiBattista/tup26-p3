using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService
{
    private readonly AgendaContext _context;

    public ContactoService(AgendaContext context)
    {
        _context = context;
    }

    public async Task<List<Contacto>> GetContactosAsync(string filtro = "")
    {
        var query = _context.Contactos.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(c => c.Nombre.Contains(filtro) || 
                                     c.Apellido.Contains(filtro) || 
                                     c.Telefono.Contains(filtro));
        }
        return await query.ToListAsync();
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
        var contacto = await _context.Contactos.FindAsync(id);
        if (contacto != null)
        {
            _context.Contactos.Remove(contacto);
            await _context.SaveChangesAsync();
        }
    }
}