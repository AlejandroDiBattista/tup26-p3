using tp5.Data;
using tp5.Models;
using Microsoft.EntityFrameworkCore;

namespace tp5.Services;

public class ContactoService(IDbContextFactory<AgendaDbContext> dbFactory)
{
    public async Task<List<Contacto>> BuscarAsync(string? texto)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var consulta = db.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var filtro = texto.Trim().ToLower();
            consulta = consulta.Where(c =>
                c.Nombre.ToLower().Contains(filtro) ||
                c.Apellido.ToLower().Contains(filtro) ||
                c.Telefono.ToLower().Contains(filtro) ||
                c.Email.ToLower().Contains(filtro) ||
                (c.Empresa != null && c.Empresa.ToLower().Contains(filtro)));
        }

        return await consulta
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Contactos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Contactos.Add(contacto);
        await db.SaveChangesAsync();
        return contacto;
    }

    public async Task ActualizarAsync(Contacto contacto)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Contactos.Update(contacto);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var contacto = await db.Contactos.FindAsync(id);

        if (contacto is null)
        {
            return;
        }

        db.Contactos.Remove(contacto);
        await db.SaveChangesAsync();
    }
}
