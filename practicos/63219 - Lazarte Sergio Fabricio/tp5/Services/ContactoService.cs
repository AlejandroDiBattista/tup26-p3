using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService
{
    private readonly AgendaDbContext _db;

    public ContactoService(AgendaDbContext db)
    {
        _db = db;
    }

    public async Task<List<Contacto>> ObtenerTodosAsync(string? busqueda = null)
    {
        var query = _db.Contactos.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var b = busqueda.ToLower();
            query = query.Where(c =>
                c.Nombre.ToLower().Contains(b) ||
                c.Apellido.ToLower().Contains(b) ||
                c.Email.ToLower().Contains(b) ||
                c.Empresa.ToLower().Contains(b) ||
                c.Telefono.Contains(b));
        }
        return await query.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Contacto?> ObtenerPorIdAsync(int id)
        => await _db.Contactos.FindAsync(id);

    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        _db.Contactos.Add(contacto);
        await _db.SaveChangesAsync();
        return contacto;
    }

    public async Task ActualizarAsync(Contacto contacto)
    {
        _db.Contactos.Update(contacto);
        await _db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        var c = await _db.Contactos.FindAsync(id);
        if (c is not null)
        {
            _db.Contactos.Remove(c);
            await _db.SaveChangesAsync();
        }
    }
}