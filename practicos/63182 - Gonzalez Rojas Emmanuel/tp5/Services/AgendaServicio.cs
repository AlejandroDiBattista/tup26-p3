using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using tp5.Datos;
using tp5.Models;

namespace tp5.Services;

/// <summary>
/// Centraliza las operaciones de agenda para que los componentes Blazor no dependan
/// directamente de consultas EF Core. Cada metodo crea su propio DbContext porque
/// la aplicacion usa componentes interactivos server-side y las operaciones pueden
/// ejecutarse en distintos eventos de UI.
/// </summary>
public sealed class AgendaServicio
{
    private readonly IDbContextFactory<AgendaContexto> _contextFactory;

    public AgendaServicio(IDbContextFactory<AgendaContexto> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Devuelve todos los contactos ordenados alfabeticamente para alimentar el
    /// panel maestro de la interfaz.
    /// </summary>
    public async Task<List<Contacto>> ObtenerContactosAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        return await db.Contactos
            .AsNoTracking()
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Filtra en memoria por nombre, apellido, telefono, email, empresa o cargo.
    /// La normalizacion permite buscar sin depender de mayusculas ni acentos.
    /// </summary>
    public static List<Contacto> Filtrar(IEnumerable<Contacto> contactos, string busqueda, string filtro)
    {
        var texto = Normalizar(busqueda);
        var tipo = filtro.Trim().ToLowerInvariant();

        return contactos
            .Where(contacto => tipo switch
            {
                "empresa" => !string.IsNullOrWhiteSpace(contacto.Empresa),
                "cumpleanos" => contacto.FechaNacimiento.HasValue,
                _ => true
            })
            .Where(contacto => texto.Length == 0 || Coincide(contacto, texto))
            .OrderBy(c => Normalizar(c.Apellido))
            .ThenBy(c => Normalizar(c.Nombre))
            .ToList();
    }

    public async Task<Contacto> GuardarAsync(Contacto contacto)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        if (contacto.Id == 0)
        {
            var nuevo = Copiar(contacto);
            db.Contactos.Add(nuevo);
            await db.SaveChangesAsync();
            return nuevo;
        }

        var existente = await db.Contactos.FindAsync(contacto.Id)
            ?? throw new InvalidOperationException("El contacto que se quiere editar ya no existe.");

        existente.Nombre = contacto.Nombre.Trim();
        existente.Apellido = contacto.Apellido.Trim();
        existente.Telefono = contacto.Telefono.Trim();
        existente.Email = contacto.Email.Trim();
        existente.Empresa = contacto.Empresa.Trim();
        existente.Cargo = contacto.Cargo.Trim();
        existente.Direccion = contacto.Direccion.Trim();
        existente.FechaNacimiento = contacto.FechaNacimiento;
        existente.Notas = contacto.Notas.Trim();

        await db.SaveChangesAsync();
        return Copiar(existente);
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var contacto = await db.Contactos.FindAsync(id);

        if (contacto is null)
            return;

        db.Contactos.Remove(contacto);
        await db.SaveChangesAsync();
    }

    public static Contacto Copiar(Contacto contacto)
    {
        return new Contacto
        {
            Id = contacto.Id,
            Nombre = contacto.Nombre.Trim(),
            Apellido = contacto.Apellido.Trim(),
            Telefono = contacto.Telefono.Trim(),
            Email = contacto.Email.Trim(),
            Empresa = contacto.Empresa.Trim(),
            Cargo = contacto.Cargo.Trim(),
            Direccion = contacto.Direccion.Trim(),
            FechaNacimiento = contacto.FechaNacimiento,
            Notas = contacto.Notas.Trim()
        };
    }

    private static bool Coincide(Contacto contacto, string texto)
    {
        var contenido = string.Join(" ", contacto.Nombre, contacto.Apellido, contacto.Telefono,
            contacto.Email, contacto.Empresa, contacto.Cargo);

        return Normalizar(contenido).Contains(texto, StringComparison.Ordinal);
    }

    private static string Normalizar(string texto)
    {
        var normalizado = texto.Trim().Normalize(NormalizationForm.FormD);
        var caracteres = normalizado
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(caracteres).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
