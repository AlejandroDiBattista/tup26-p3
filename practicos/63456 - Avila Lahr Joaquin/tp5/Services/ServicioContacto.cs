using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services;

public class ServicioContacto
{
    private readonly Contexto _contexto;

    public ServicioContacto(Contexto contexto)
    {
        _contexto = contexto;
    }

    public async Task<List<Contacto>> ObtenerTodosAsync(string? busqueda = null)
    {
        var query = _contexto.Contactos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var b = busqueda.ToLower();
            query = query.Where(c =>
                c.Nombre.ToLower().Contains(b) ||
                c.Apellido.ToLower().Contains(b) ||
                c.Email.ToLower().Contains(b) ||
                c.Telefono.Contains(b) ||
                c.Empresa.ToLower().Contains(b) ||
                c.Legajo.ToString().ToLower().Contains(b));
        }

        return await query.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Contacto?> ObtenerPorIdAsync(int id)
        => await _contexto.Contactos.FindAsync(id);

    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        _contexto.Contactos.Add(contacto);
        await _contexto.SaveChangesAsync();
        return contacto;
    }

   public async Task ActualizarAsync(Contacto contacto)
{
    var existente = await _contexto.Contactos.FindAsync(contacto.Id);
    if (existente == null) return;

    existente.Nombre = contacto.Nombre;
    existente.Apellido = contacto.Apellido;
    existente.Telefono = contacto.Telefono;
    existente.Email = contacto.Email;
    existente.Empresa = contacto.Empresa;
    existente.Cargo = contacto.Cargo;
    existente.Direccion = contacto.Direccion;
    existente.FechaNacimiento = contacto.FechaNacimiento;
    existente.Notas = contacto.Notas;
    existente.Legajo = contacto.Legajo;

    await _contexto.SaveChangesAsync();
}

    public async Task EliminarAsync(int id)
    {
        var contacto = await _contexto.Contactos.FindAsync(id);
        if (contacto != null)
        {
            _contexto.Contactos.Remove(contacto);
            await _contexto.SaveChangesAsync();
        }
    }
}
