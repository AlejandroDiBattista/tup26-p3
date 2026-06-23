using AgendaWeb.Datos;
using AgendaWeb.Modelos;
using Microsoft.EntityFrameworkCore;

namespace AgendaWeb.Servicios;

public class ContactoService : IContactoService
{
    private readonly IDbContextFactory<AgendaDbContext> fabrica;

    public ContactoService(IDbContextFactory<AgendaDbContext> fabrica)
    {
        this.fabrica = fabrica;
    }

    public async Task<List<Contacto>> Listar(string? filtro = null)
    {
        using var contexto = this.fabrica.CreateDbContext();
        IQueryable<Contacto> consulta = contexto.Contactos;

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var busqueda = filtro.Trim();
            // La base filtra con LIKE, no trae todo a memoria.
            consulta = consulta.Where(contacto =>
                EF.Functions.Like(contacto.Nombre, $"%{busqueda}%") ||
                EF.Functions.Like(contacto.Apellido, $"%{busqueda}%") ||
                EF.Functions.Like(contacto.Email, $"%{busqueda}%") ||
                (contacto.Empresa != null && EF.Functions.Like(contacto.Empresa, $"%{busqueda}%")));
        }

        return await consulta
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerPorId(int id)
    {
        using var contexto = this.fabrica.CreateDbContext();
        return await contexto.Contactos.FindAsync(id);
    }

    public async Task<Contacto> Crear(Contacto nuevo)
    {
        using var contexto = this.fabrica.CreateDbContext();
        nuevo.Id = 0; // la base asigna el Id sola
        contexto.Contactos.Add(nuevo);
        await contexto.SaveChangesAsync();
        return nuevo;
    }

    public async Task<bool> Actualizar(Contacto modificado)
    {
        using var contexto = this.fabrica.CreateDbContext();
        var existente = await contexto.Contactos.FindAsync(modificado.Id);
        if (existente is null)
        {
            return false;
        }

        // Copiamos los datos editados sobre el contacto guardado.
        contexto.Entry(existente).CurrentValues.SetValues(modificado);
        await contexto.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        using var contexto = this.fabrica.CreateDbContext();
        var existente = await contexto.Contactos.FindAsync(id);
        if (existente is null)
        {
            return false;
        }

        contexto.Contactos.Remove(existente);
        await contexto.SaveChangesAsync();
        return true;
    }
}
