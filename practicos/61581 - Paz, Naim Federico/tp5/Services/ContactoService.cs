using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService(IDbContextFactory<AgendaDbContext> dbContextFactory)
{
    public async Task<List<Contacto>> ObtenerTodosAsync(string? busqueda = null)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var consulta = db.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            consulta = consulta.Where(contacto =>
                EF.Functions.Like(contacto.Nombre, patron) ||
                EF.Functions.Like(contacto.Apellido, patron) ||
                EF.Functions.Like(contacto.Telefono, patron) ||
                EF.Functions.Like(contacto.Email, patron) ||
                EF.Functions.Like(contacto.Empresa, patron) ||
                EF.Functions.Like(contacto.Cargo, patron));
        }

        return await consulta
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerPorIdAsync(int id)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        return await db.Contactos
            .AsNoTracking()
            .FirstOrDefaultAsync(contacto => contacto.Id == id);
    }

    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        db.Contactos.Add(contacto);
        await db.SaveChangesAsync();

        return contacto;
    }

    public async Task<bool> ActualizarAsync(Contacto contactoActualizado)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var contacto = await db.Contactos.FindAsync(contactoActualizado.Id);

        if (contacto is null)
        {
            return false;
        }

        contacto.Nombre = contactoActualizado.Nombre;
        contacto.Apellido = contactoActualizado.Apellido;
        contacto.Telefono = contactoActualizado.Telefono;
        contacto.Email = contactoActualizado.Email;
        contacto.Empresa = contactoActualizado.Empresa;
        contacto.Cargo = contactoActualizado.Cargo;
        contacto.Direccion = contactoActualizado.Direccion;
        contacto.FechaNacimiento = contactoActualizado.FechaNacimiento;
        contacto.Notas = contactoActualizado.Notas;

        await db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var contacto = await db.Contactos.FindAsync(id);

        if (contacto is null)
        {
            return false;
        }

        db.Contactos.Remove(contacto);
        await db.SaveChangesAsync();

        return true;
    }
}
