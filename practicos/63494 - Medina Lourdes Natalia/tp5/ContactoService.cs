using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService(IDbContextFactory<AgendaDbContext> dbFactory)
{
    public async Task<List<Contacto>> BuscarAsync(string? texto)
    {
        await using AgendaDbContext db = await dbFactory.CreateDbContextAsync();
        string filtro = texto?.Trim() ?? "";

        IQueryable<Contacto> query = db.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(contacto =>
                contacto.Nombre.Contains(filtro) ||
                contacto.Apellido.Contains(filtro) ||
                contacto.Telefono.Contains(filtro) ||
                contacto.Email.Contains(filtro) ||
                contacto.Empresa.Contains(filtro));
        }

        return await query
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerAsync(int id)
    {
        await using AgendaDbContext db = await dbFactory.CreateDbContextAsync();
        return await db.Contactos.AsNoTracking().FirstOrDefaultAsync(contacto => contacto.Id == id);
    }
