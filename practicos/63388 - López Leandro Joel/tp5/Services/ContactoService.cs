using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService : IContactService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ContactoService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Contacto>> GetTodosAsync(string? busqueda = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var query = ctx.Contactos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.Trim().ToLower();
            query = query.Where(c =>
                c.Nombre.ToLower().Contains(term) ||
                c.Apellido.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Telefono.Contains(term) ||
                (c.Empresa != null && c.Empresa.ToLower().Contains(term)) ||
                (c.Cargo != null && c.Cargo.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> GetPorIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.Contactos.FindAsync(id);
    }

    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.Contactos.Add(contacto);
        await ctx.SaveChangesAsync();
        return contacto;
    }

    public async Task<Contacto> ActualizarAsync(Contacto contacto)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.Contactos.Update(contacto);
        await ctx.SaveChangesAsync();
        return contacto;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var contacto = await ctx.Contactos.FindAsync(id);
        if (contacto is null) return false;
        ctx.Contactos.Remove(contacto);
        await ctx.SaveChangesAsync();
        return true;
    }
}
