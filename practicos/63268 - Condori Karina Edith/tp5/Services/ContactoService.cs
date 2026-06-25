using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public sealed class ContactoService(IDbContextFactory<AgendaDbContext> contextFactory)
{
    public async Task<List<Contacto>> BuscarAsync(string? texto)
    {
        await using AgendaDbContext db = await contextFactory.CreateDbContextAsync();
        IQueryable<Contacto> consulta = db.Contactos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            string filtro = texto.Trim();
            consulta = consulta.Where(contacto =>
                contacto.Nombre.Contains(filtro) ||
                contacto.Apellido.Contains(filtro) ||
                contacto.Email.Contains(filtro) ||
                contacto.Telefono.Contains(filtro) ||
                contacto.Empresa.Contains(filtro));
        }

        return await consulta
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        await using AgendaDbContext db = await contextFactory.CreateDbContextAsync();
        Normalizar(contacto);
        db.Contactos.Add(contacto);
        await db.SaveChangesAsync();
        return contacto;
    }

    public async Task<bool> ActualizarAsync(Contacto contacto)
    {
        await using AgendaDbContext db = await contextFactory.CreateDbContextAsync();
        Contacto? guardado = await db.Contactos.FindAsync(contacto.Id);
        if (guardado is null)
        {
            return false;
        }

        Normalizar(contacto);
        guardado.Nombre = contacto.Nombre;
        guardado.Apellido = contacto.Apellido;
        guardado.Telefono = contacto.Telefono;
        guardado.Email = contacto.Email;
        guardado.Empresa = contacto.Empresa;
        guardado.Cargo = contacto.Cargo;
        guardado.Direccion = contacto.Direccion;
        guardado.FechaNacimiento = contacto.FechaNacimiento;
        guardado.Notas = contacto.Notas;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        await using AgendaDbContext db = await contextFactory.CreateDbContextAsync();
        Contacto? contacto = await db.Contactos.FindAsync(id);
        if (contacto is null)
        {
            return false;
        }

        db.Contactos.Remove(contacto);
        await db.SaveChangesAsync();
        return true;
    }

    public static Contacto Copiar(Contacto contacto) => new()
    {
        Id = contacto.Id,
        Nombre = contacto.Nombre,
        Apellido = contacto.Apellido,
        Telefono = contacto.Telefono,
        Email = contacto.Email,
        Empresa = contacto.Empresa,
        Cargo = contacto.Cargo,
        Direccion = contacto.Direccion,
        FechaNacimiento = contacto.FechaNacimiento,
        Notas = contacto.Notas
    };

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
