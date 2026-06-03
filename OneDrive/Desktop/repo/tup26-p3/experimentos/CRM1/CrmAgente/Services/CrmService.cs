using Microsoft.EntityFrameworkCore;
using CrmAgente.Data;
using CrmAgente.Models;

namespace CrmAgente.Services;

// Esta es la capa que concentra TODA la logica de negocio del CRM.
// Regla de oro de la arquitectura: tanto los componentes Blazor como
// el agente de IA consumen estos mismos metodos. No hay logica de
// negocio duplicada en la UI ni en el agente. Si esto funciona, el
// agente funciona; si no, no hay IA que lo salve.
public class CrmService {
    private readonly CrmContext db;

    public CrmService(CrmContext db) {
        this.db = db;
    }

    // ---- Contactos ----

    public async Task<List<Contacto>> ListarContactosAsync(string? texto = null) {
        var query = this.db.Contactos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(texto)) {
            var patron = $"%{texto}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Nombre, patron) ||
                EF.Functions.Like(c.Empresa, patron));
        }

        return await query.OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Contacto?> BuscarContactoAsync(string texto) {
        return await this.db.Contactos.FirstOrDefaultAsync(c =>
            c.Nombre.Contains(texto) || c.Empresa.Contains(texto));
    }

    // La ficha 360: trae el contacto CON sus oportunidades y actividades
    // en una sola consulta usando Include. Es el agregado completo que
    // describe el estado total de la relacion.
    public async Task<Contacto?> FichaCompletaAsync(int contactoId) {
        return await this.db.Contactos
            .Include(c => c.Oportunidades)
            .Include(c => c.Actividades)
            .FirstOrDefaultAsync(c => c.Id == contactoId);
    }

    public async Task<Contacto> CrearContactoAsync(string nombre, string empresa, string email, string telefono) {
        var contacto = new Contacto {
            Nombre = nombre,
            Empresa = empresa,
            Email = email,
            Telefono = telefono
        };

        this.db.Contactos.Add(contacto);
        await this.db.SaveChangesAsync();
        return contacto;
    }

    // Solo pisa los campos que vienen con valor; los null se ignoran.
    public async Task<Contacto?> ActualizarContactoAsync(
        int id, string? nombre, string? empresa, string? email, string? telefono) {
        var contacto = await this.db.Contactos.FindAsync(id);
        if (contacto is null) return null;

        if (!string.IsNullOrEmpty(nombre)) contacto.Nombre = nombre;
        if (!string.IsNullOrEmpty(empresa)) contacto.Empresa = empresa;
        if (!string.IsNullOrEmpty(email)) contacto.Email = email;
        if (!string.IsNullOrEmpty(telefono)) contacto.Telefono = telefono;

        await this.db.SaveChangesAsync();
        return contacto;
    }

    // ---- Actividades ----

    // fecha opcional: si no se indica, queda la fecha y hora actual (default del modelo).
    public async Task<Actividad> RegistrarActividadAsync(int contactoId, string tipo, string detalle, DateTime? fecha = null) {
        var actividad = new Actividad {
            ContactoId = contactoId,
            Tipo = tipo,
            Detalle = detalle,
            Fecha = fecha ?? DateTime.Now
        };

        this.db.Actividades.Add(actividad);
        await this.db.SaveChangesAsync();
        return actividad;
    }

    // ---- Oportunidades ----

    // Metodo de busqueda con filtros opcionales. Este es EXACTAMENTE el
    // metodo que el agente expone como herramienta. Notar como los
    // parametros opcionales permiten que el LLM combine filtros segun
    // lo que el usuario pregunte en lenguaje natural.
    public async Task<List<Oportunidad>> BuscarOportunidadesAsync(
        Etapa? etapa = null, decimal? montoMinimo = null) {
        var query = this.db.Oportunidades
            .Include(o => o.Contacto)
            .AsQueryable();

        if (etapa is not null) {
            query = query.Where(o => o.Etapa == etapa);
        }

        if (montoMinimo is not null) {
            query = query.Where(o => o.Monto >= montoMinimo);
        }

        return await query.OrderByDescending(o => o.Monto).ToListAsync();
    }

    // etapa opcional: las oportunidades nuevas arrancan como Prospecto salvo
    // que se indique otra cosa.
    public async Task<Oportunidad> CrearOportunidadAsync(
        int contactoId, string descripcion, decimal monto, Etapa etapa = Etapa.Prospecto) {
        var oportunidad = new Oportunidad {
            ContactoId = contactoId,
            Descripcion = descripcion,
            Monto = monto,
            Etapa = etapa
        };

        this.db.Oportunidades.Add(oportunidad);
        await this.db.SaveChangesAsync();
        return oportunidad;
    }

    // Devuelve la oportunidad borrada (o null si no existia) para poder, por
    // ejemplo, referenciar al contacto duenio luego de eliminarla.
    public async Task<Oportunidad?> EliminarOportunidadAsync(int oportunidadId) {
        var oportunidad = await this.db.Oportunidades.FindAsync(oportunidadId);
        if (oportunidad is null) return null;

        this.db.Oportunidades.Remove(oportunidad);
        await this.db.SaveChangesAsync();
        return oportunidad;
    }

    // Devuelve la oportunidad actualizada (o null si no existe) para que quien
    // la llame pueda, por ejemplo, referenciar al contacto duenio.
    public async Task<Oportunidad?> MoverEtapaAsync(int oportunidadId, Etapa etapa) {
        var oportunidad = await this.db.Oportunidades.FindAsync(oportunidadId);
        if (oportunidad is null) {
            return null;
        }

        oportunidad.Etapa = etapa;
        await this.db.SaveChangesAsync();
        return oportunidad;
    }

    // Devuelve las oportunidades agrupadas por etapa: la data cruda del
    // tablero de pipeline.
    public async Task<List<Oportunidad>> ListarOportunidadesAsync() {
        return await this.db.Oportunidades
            .Include(o => o.Contacto)
            .ToListAsync();
    }
}
