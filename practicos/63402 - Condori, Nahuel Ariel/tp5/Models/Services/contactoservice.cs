using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService
{
    private readonly IDbContextFactory<AgendaContext> _contextFactory;

    public ContactoService(IDbContextFactory<AgendaContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Contacto>> GetContactosAsync(string filtro = "")
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            filtro = filtro.Trim();
            query = query.Where(c =>
                c.Nombre.Contains(filtro) ||
                c.Apellido.Contains(filtro) ||
                c.Telefono.Contains(filtro) ||
                c.Email.Contains(filtro) ||
                c.Empresa.Contains(filtro));
        }

        return await query
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task AddContactoAsync(Contacto contacto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Contactos.Add(contacto);
        await context.SaveChangesAsync();
    }

    public async Task UpdateContactoAsync(Contacto contacto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Contactos.Update(contacto);
        await context.SaveChangesAsync();
    }

    public async Task DeleteContactoAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var contacto = await context.Contactos.FindAsync(id);

        if (contacto != null)
        {
            context.Contactos.Remove(contacto);
            await context.SaveChangesAsync();
        }
    }
}
