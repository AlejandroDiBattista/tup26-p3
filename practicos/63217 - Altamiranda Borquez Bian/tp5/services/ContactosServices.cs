using Microsoft.EntipyFrameworkCore;
using tp5.Data;
using tp5.Models;
namespace tp5.Services;

public class ServicioContactos
{
    private readonly IDbContextFactory<AgendaDbContext> fabricaContexto;

    public ServicioContactos(IDbContextFactory<AgendaDbContext> fabricaContexto)
    {
        this.fabricaContexto = fabricaContexto;
    }
 public async Task<List<Contacto>> ObtenerContactosAsync()
    {
        await using var contexto = await fabricaContexto.CreateDbContextAsync();

        return await contexto.Contactos
            .AsNoTracking()
            .OrderBy(contacto => contacto.Nombre)
            .ThenBy(contacto => contacto.Apellido)
            .ToListAsync();
    }
public async Task<Contacto?> ObtenerPorIdAsync(int id)
    {
        await using var contexto = await fabricaContexto.CreateDbContextAsync();

        return await contexto.Contactos
            .AsNoTracking()
            .FirstOrDefaultAsync(contacto => contacto.Id == id);
    }
 public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        await using var contexto = await fabricaContexto.CreateDbContextAsync();

        NormalizarTexto(contacto);
        contexto.Contactos.Add(contacto);
        await contexto.SaveChangesAsync();

        return contacto;
    }
 public async Task ActualizarAsync(Contacto contactoEditado)
    {
        await using var contexto = await fabricaContexto.CreateDbContextAsync();
        var contactoActual = await contexto.Contactos.FindAsync(contactoEditado.Id);

        if (contactoActual is null)
        {
            return;
        }

        NormalizarTexto(contactoEditado);
        contactoActual.Nombre = contactoEditado.Nombre;
        contactoActual.Apellido = contactoEditado.Apellido;
        contactoActual.Telefono = contactoEditado.Telefono;
        contactoActual.Email = contactoEditado.Email;
        contactoActual.Empresa = contactoEditado.Empresa;
        contactoActual.Cargo = contactoEditado.Cargo;
        contactoActual.Direccion = contactoEditado.Direccion;
        contactoActual.FechaNacimiento = contactoEditado.FechaNacimiento;
        contactoActual.Notas = contactoEditado.Notas;

        await contexto.SaveChangesAsync();
    }
 public async Task EliminarAsync(int id)
    {
        await using var contexto = await fabricaContexto.CreateDbContextAsync();
        var contacto = await contexto.Contactos.FindAsync(id);

        if (contacto is null)
        {
            return;
        }

        contexto.Contactos.Remove(contacto);
        await contexto.SaveChangesAsync();
    }

 private static void NormalizarTexto(Contacto contacto)
    {
        contacto.Nombre = contacto.Nombre.Trim();
        contacto.Apellido = contacto.Apellido.Trim();
        contacto.Telefono = contacto.Telefono.Trim();
        contacto.Email = contacto.Email.Trim();
        contacto.Empresa = contacto.Empresa?.Trim() ?? "";
        contacto.Cargo = contacto.Cargo?.Trim() ?? "";
        contacto.Direccion = contacto.Direccion?.Trim() ?? "";
        contacto.Notas = contacto.Notas?.Trim() ?? "";
    }
}