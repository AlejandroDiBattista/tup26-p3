using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public sealed class ContactoService
{
    private readonly IDbContextFactory<AgendaContext> contextoFactory;

    public ContactoService(IDbContextFactory<AgendaContext> contextoFactory)
    {
        this.contextoFactory = contextoFactory;
    }

    public async Task<List<Contacto>> ListarAsync(string? terminoBusqueda = null)
    {
        using var contexto = await contextoFactory.CreateDbContextAsync();

        IQueryable<Contacto> consulta = contexto.Contactos;

        if (!string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            string filtro = terminoBusqueda.Trim();
            consulta = consulta.Where(contacto =>
                contacto.Nombre.Contains(filtro) ||
                contacto.Apellido.Contains(filtro) ||
                contacto.Telefono.Contains(filtro) ||
                contacto.Email.Contains(filtro));
        }

        return await consulta
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerPorIdAsync(int contactoId)
    {
        using var contexto = await contextoFactory.CreateDbContextAsync();

        return await contexto.Contactos
            .AsNoTracking()
            .FirstOrDefaultAsync(contacto => contacto.Id == contactoId);
    }

    public async Task GuardarAsync(Contacto contacto)
    {
        using var contexto = await contextoFactory.CreateDbContextAsync();

        bool esNuevo = contacto.Id == 0;

        if (esNuevo)
        {
            contexto.Contactos.Add(contacto);
        }
        else
        {
            contexto.Contactos.Update(contacto);
        }

        await contexto.SaveChangesAsync();
    }

    public async Task<bool> EliminarAsync(int contactoId)
    {
        using var contexto = await contextoFactory.CreateDbContextAsync();

        Contacto? contacto = await contexto.Contactos.FindAsync(contactoId);

        if (contacto is null)
        {
            return false;
        }

        contexto.Contactos.Remove(contacto);
        await contexto.SaveChangesAsync();
        return true;
    }
}
