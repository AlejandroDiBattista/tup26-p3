using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService
{
    private readonly AgendaaContext _context;

    public ContactoService(AgendaaContext context)
    {
        _context = context;
    }

    public async Task<List<Contacto>> ObtenerTodosAsync()
    {
        // Retorna todos los contactos ordenados por apellido por defecto
        return await _context.Contactos.OrderBy(c => c.Apellido).ToListAsync();
    }

    public async Task GuardarAsync(Contacto contacto)
    {
        if (contacto.Id == 0)
        {
            // Si el Id es 0, es un contacto nuevo (Alta)
            _context.Contactos.Add(contacto);
        }
        else
        {
            // Si el Id existe, es una modificación
            var original = await _context.Contactos.FindAsync(contacto.Id);
            if (original != null)
            {
                _context.Entry(original).CurrentValues.SetValues(contacto);
            }
        }
        

        await _context.SaveChangesAsync();
    }

    public async Task EliminarAsync(Contacto contacto)
    {
        _context.Contactos.Remove(contacto);
        await _context.SaveChangesAsync();
    }
}