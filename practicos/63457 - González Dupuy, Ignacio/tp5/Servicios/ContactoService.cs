using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Servicios;

public class ContactoService
{
    private readonly AgendaContext db;

    public ContactoService(AgendaContext db)
    {
        this.db = db;
    }

    public async Task<List<Contacto>> ListarContactos()
    {
        return await db.Agenda.ToListAsync();
    }

    public async Task CrearContacto(Contacto nuevoContacto)
    {
        db.Agenda.Add(nuevoContacto);
        await db.SaveChangesAsync();
    }

    public async Task ModificarContacto(Contacto contacto)
    {
        db.Agenda.Update(contacto);
        await db.SaveChangesAsync();
    }

    public async Task BorrarContacto(int id)
    {
        var registro = await db.Agenda.FindAsync(id);

        if (registro != null)
        {
            db.Agenda.Remove(registro);
            await db.SaveChangesAsync();
        }
    }
}