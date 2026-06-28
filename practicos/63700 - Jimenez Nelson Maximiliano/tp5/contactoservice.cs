using AgendaWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace AgendaWeb.Services;

public class ContactoService
{
    private readonly AgendaDbContext _context;

    public ContactoService(AgendaDbContext context)
    {
        _context = context;
    }

    // Obtener contactos con filtro de búsqueda opcional
    public async Task<List<Contacto>> GetContactosAsync(string filtro = "")
    {
        if (string.IsNullOrWhiteSpace(filtro))
        {
            return await _context.Contactos.ToListAsync();
        }

        return await _context.Contactos
            .Where(c => (c.Nombre != null && c.Nombre.Contains(filtro)) || 
                        (c.Apellido != null && c.Apellido.Contains(filtro)) ||
                        (c.Empresa != null && c.Empresa.Contains(filtro)))
            .ToListAsync();
    }

    public async Task<Contacto?> GetContactoByIdAsync(int id)
    {
        return await _context.Contactos.FindAsync(id);
    }

    public async Task AddContactoAsync(Contacto contacto)
    {
        _context.Contactos.Add(contacto);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateContactoAsync(Contacto contacto)
    {
        _context.Entry(contacto).State = EntityState.Modified;
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