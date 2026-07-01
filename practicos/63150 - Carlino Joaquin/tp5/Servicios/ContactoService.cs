using tp5.Datos;
using tp5.Models;
using Microsoft.EntityFrameworkCore;

namespace tp5.Servicios;
public class ContactoService : IContactoService
{
    private readonly IDbContextFactory<AgendaDbContext> fabrica;
    public ContactoService(IDbContextFactory<AgendaDbContext> fabrica)
    {
        this.fabrica = fabrica;
    }
    public async Task<List<Contacto>> ListarAsync(string? filtro = null)
    {
        using var db = this.fabrica.CreateDbContext();
        IQueryable<Contacto> consulta = db.Contactos;
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var f = filtro.Trim();
            // El filtrado lo hace la base con LIKE.
            consulta = consulta.Where(c =>
                EF.Functions.Like(c.Nombre, $"%{f}%") ||
                EF.Functions.Like(c.Apellido, $"%{f}%") ||
                EF.Functions.Like(c.Email, $"%{f}%"));
        }
        return await consulta
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Contacto?> ObtenerPorIdAsync(int id)
{
    using var db = this.fabrica.CreateDbContext();
    return await db.Contactos.FindAsync(id);
}
public async Task<Contacto> CrearAsync(Contacto nuevo)
{
    using var db = this.fabrica.CreateDbContext();
    db.Contactos.Add(nuevo);
    await db.SaveChangesAsync();   // la base asigna el Id
    return nuevo;
}
public async Task<bool> ActualizarAsync(Contacto modificado)
{
    using var db = this.fabrica.CreateDbContext();
    var existente = await db.Contactos.FindAsync(modificado.Id);
    if (existente is null) return false;
    db.Entry(existente).CurrentValues.SetValues(modificado);
    await db.SaveChangesAsync();
    return true;
}
public async Task<bool> EliminarAsync(int id)
{
    using var db = this.fabrica.CreateDbContext();
    var existente = await db.Contactos.FindAsync(id);
    if (existente is null) return false;
    db.Contactos.Remove(existente);
    await db.SaveChangesAsync();
    return true;
}
}