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

    public async Task<Contacto> GuardarAsync(Contacto contacto)
    {
        await using AgendaDbContext db = await dbFactory.CreateDbContextAsync();
        Normalizar(contacto);

        if (contacto.Id == 0)
        {
            db.Contactos.Add(contacto);
        }
        else
        {
            db.Contactos.Update(contacto);
        }

        await db.SaveChangesAsync();
        return contacto;
    }

    public async Task EliminarAsync(int id)
    {
        await using AgendaDbContext db = await dbFactory.CreateDbContextAsync();
        Contacto? contacto = await db.Contactos.FindAsync(id);

        if (contacto is null)
        {
            return;
        }

        db.Contactos.Remove(contacto);
        await db.SaveChangesAsync();
    }

    private static void Normalizar(Contacto contacto)
    {
        contacto.Nombre = contacto.Nombre.Trim();
        contacto.Apellido = contacto.Apellido.Trim();
        contacto.Telefono = contacto.Telefono.Trim();
        contacto.Email = contacto.Email.Trim();
        contacto.Empresa = contacto.Empresa.Trim();
        contacto.Cargo = contacto.Cargo.Trim();
        contacto.Direccion = contacto.Direccion.Trim();
        contacto.Notas = contacto.Notas.Trim();
    }
}

