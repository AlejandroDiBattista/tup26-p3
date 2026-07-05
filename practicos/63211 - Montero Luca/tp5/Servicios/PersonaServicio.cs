using AgendaWeb.Datos;
using AgendaWeb.Modelos;
using Microsoft.EntityFrameworkCore;

namespace AgendaWeb.Servicios;

public class PersonaServicio : IPersonaServicio
{
    private readonly IDbContextFactory<LibretaDbContext> creadorContextos;

    public PersonaServicio(IDbContextFactory<LibretaDbContext> creadorContextos)
    {
        this.creadorContextos = creadorContextos;
    }

    public async Task<List<Persona>> BuscarTodasAsync(string? textoBusqueda = null)
    {
        using var ctx = this.creadorContextos.CreateDbContext();
        IQueryable<Persona> consulta = ctx.Personas;

        if (!string.IsNullOrWhiteSpace(textoBusqueda))
        {
            var patron = textoBusqueda.Trim();
            // El filtrado con like (no se trae todo a memoria).
            consulta = consulta.Where(p =>
                EF.Functions.Like(p.Nombre, $"%{patron}%") ||
                EF.Functions.Like(p.Apellido, $"%{patron}%") ||
                EF.Functions.Like(p.Email, $"%{patron}%") ||
                (p.Empresa != null && EF.Functions.Like(p.Empresa, $"%{patron}%")));
        }

        return await consulta
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Persona?> BuscarPorIdAsync(int id)
    {
        using var ctx = this.creadorContextos.CreateDbContext();
        return await ctx.Personas.FindAsync(id);
    }

    public async Task<Persona> AgregarAsync(Persona nueva)
    {
        using var ctx = this.creadorContextos.CreateDbContext();
        nueva.Id = 0; // la base asigna el Id automáticamente
        ctx.Personas.Add(nueva);
        await ctx.SaveChangesAsync();
        return nueva;
    }

    public async Task<bool> ModificarAsync(Persona editada)
    {
        using var ctx = this.creadorContextos.CreateDbContext();
        var registro = await ctx.Personas.FindAsync(editada.Id);
        if (registro is null)
        {
            return false;
        }

        // Copiamos los valores editados sobre la entidad que rastrea EF.
        ctx.Entry(registro).CurrentValues.SetValues(editada);
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BorrarAsync(int id)
    {
        using var ctx = this.creadorContextos.CreateDbContext();
        var registro = await ctx.Personas.FindAsync(id);
        if (registro is null)
        {
            return false;
        }

        ctx.Personas.Remove(registro);
        await ctx.SaveChangesAsync();
        return true;
    }
}
