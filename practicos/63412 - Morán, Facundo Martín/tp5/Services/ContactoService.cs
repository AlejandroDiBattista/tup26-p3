using Microsoft.EntityFrameworkCore;
using tp5.Models;

namespace tp5.Services;

public class ContactoService
{
    private readonly IDbContextFactory<AgendaDbContext> _contextFactory;

    public ContactoService(IDbContextFactory<AgendaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Contacto>> ObtenerTodosAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Contactos
            .AsNoTracking()
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerPorIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Contactos
            .AsNoTracking()
            .FirstOrDefaultAsync(contacto => contacto.Id == id);
    }

    public async Task<Contacto> GuardarAsync(Contacto contacto)
    {
        Normalizar(contacto);

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (contacto.Id == 0)
        {
            context.Contactos.Add(contacto);
        }
        else
        {
            context.Contactos.Update(contacto);
        }

        await context.SaveChangesAsync();

        return contacto;
    }

    public async Task EliminarAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var contacto = await context.Contactos.FindAsync(id);

        if (contacto is null)
        {
            return;
        }

        context.Contactos.Remove(contacto);

        await context.SaveChangesAsync();
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