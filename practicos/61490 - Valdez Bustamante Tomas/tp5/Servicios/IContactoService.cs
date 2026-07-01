using AgendaWeb.Modelos;

namespace AgendaWeb.Servicios;

public interface IContactoService
{
    Task<List<Contacto>> Listar(string? filtro = null);
    Task<Contacto?> ObtenerPorId(int id);
    Task<Contacto> Crear(Contacto nuevo);
    Task<bool> Actualizar(Contacto modificado);
    Task<bool> Eliminar(int id);
}
