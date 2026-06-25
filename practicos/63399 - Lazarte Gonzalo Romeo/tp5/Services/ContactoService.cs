using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService
{
    private readonly IDbContextFactory<AgendaContext> _factory;

    public ContactoService(IDbContextFactory<AgendaContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Contacto>> BuscarAsync(string? filtro = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        IQueryable<Contacto> consulta = db.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var texto = filtro.Trim().ToLower();
            consulta = consulta.Where(c =>
                c.Nombre.ToLower().Contains(texto) ||
                c.Apellido.ToLower().Contains(texto) ||
                c.Email.ToLower().Contains(texto) ||
                c.Telefono.Contains(texto) ||
                c.Empresa.ToLower().Contains(texto) ||
                c.Cargo.ToLower().Contains(texto));
        }

        return await consulta
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Contactos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Contactos.Add(contacto);
        await db.SaveChangesAsync();
        return contacto;
    }

    public async Task ActualizarAsync(Contacto contacto)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Contactos.Update(contacto);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var contacto = await db.Contactos.FindAsync(id);
        if (contacto is not null)
        {
            db.Contactos.Remove(contacto);
            await db.SaveChangesAsync();
        }
    }
}
