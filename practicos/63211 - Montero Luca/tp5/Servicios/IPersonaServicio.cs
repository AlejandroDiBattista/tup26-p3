using AgendaWeb.Modelos;

namespace AgendaWeb.Servicios;

public interface IPersonaServicio
{
    Task<List<Persona>> BuscarTodasAsync(string? textoBusqueda = null);
    Task<Persona?> BuscarPorIdAsync(int id);
    Task<Persona> AgregarAsync(Persona nueva);
    Task<bool> ModificarAsync(Persona editada);
    Task<bool> BorrarAsync(int id);
}
