using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ContactoService(IDbContextFactory<AgendaDbContext> dbContextFactory)
{
    public async Task<List<Contacto>> BuscarAsync(string texto)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var consulta = db.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var filtro = $"%{texto.Trim()}%";
            consulta = consulta.Where(c =>
                EF.Functions.Like(c.Nombre, filtro) ||
                EF.Functions.Like(c.Apellido, filtro) ||
                EF.Functions.Like(c.Nombre + " " + c.Apellido, filtro) ||
                EF.Functions.Like(c.Telefono, filtro) ||
                EF.Functions.Like(c.Email, filtro) ||
                EF.Functions.Like(c.Empresa, filtro));
        }

        return await consulta
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerAsync(int id)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        return await db.Contactos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        Normalizar(contacto);
        db.Contactos.Add(contacto);
        await db.SaveChangesAsync();
        return contacto;
    }

    public async Task ActualizarAsync(Contacto contacto)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        Normalizar(contacto);
        db.Contactos.Update(contacto);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var contacto = await db.Contactos.FindAsync(id);

        if (contacto is not null)
        {
            db.Contactos.Remove(contacto);
            await db.SaveChangesAsync();
        }
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
