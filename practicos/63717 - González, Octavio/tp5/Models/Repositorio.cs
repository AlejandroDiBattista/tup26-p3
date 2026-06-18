namespace tp5.Models;

using Microsoft.EntityFrameworkCore;
using tp5.Data;

/// <summary>
/// Encapsula las operaciones de acceso a datos de la agenda.
/// Mantiene la lógica de persistencia fuera de la UI y concentra
/// las reglas de lectura, alta, edición y borrado en un único lugar.
/// </summary>
public class Repositorio
{
    private readonly IDbContextFactory<AgendaDbContext> dbFactory;

    public Repositorio(IDbContextFactory<AgendaDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    public void Iniciar()
    {
        using var db = dbFactory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    public async Task<List<Contacto>> TraerContactos()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Contactos
            .AsNoTracking()
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> TraerContacto(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Contactos.AsNoTracking().FirstOrDefaultAsync(contacto => contacto.Id == id);
    }

    public async Task<Contacto> AgregarContacto(Contacto contacto)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Contactos.Add(contacto);
        await db.SaveChangesAsync();
        return contacto;
    }

    public async Task<Contacto?> Actualizar(Contacto actualizacion)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var contacto = await db.Contactos.FirstOrDefaultAsync(item => item.Id == actualizacion.Id);

        if (contacto is null)
        {
            return null;
        }

        contacto.Nombre = actualizacion.Nombre;
        contacto.Apellido = actualizacion.Apellido;
        contacto.Telefono = actualizacion.Telefono;
        contacto.Email = actualizacion.Email;
        contacto.Empresa = actualizacion.Empresa;
        contacto.Cargo = actualizacion.Cargo;
        contacto.Direccion = actualizacion.Direccion;
        contacto.FechaNacimiento = actualizacion.FechaNacimiento;
        contacto.Notas = actualizacion.Notas;

        await db.SaveChangesAsync();
        return contacto;
    }

    public async Task<bool> Eliminar(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var contacto = await db.Contactos.FirstOrDefaultAsync(item => item.Id == id);

        if (contacto is null)
        {
            return false;
        }

        db.Contactos.Remove(contacto);
        await db.SaveChangesAsync();
        return true;
    }
}
