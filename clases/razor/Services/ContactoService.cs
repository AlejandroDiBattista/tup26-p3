using razor.Models;
using razor.Repositories;

namespace razor.Services;

public class ContactoService : IContactoService {
    private readonly IContactoRepository repository;

    public ContactoService(IContactoRepository repository) {
        this.repository = repository;
    }

    public Task<List<Contacto>> ListarAsync() {
        return repository.ObtenerTodosAsync();
    }

    public Task<Contacto?> BuscarAsync(int id) {
        return repository.ObtenerPorIdAsync(id);
    }

    public Task CrearAsync(string nombre, string apellido, string telefono, string? email, bool favorito) {
        Contacto contacto = new() {
            Nombre = nombre,
            Apellido = apellido,
            Telefono = telefono,
            Email = email,
            Favorito = favorito
        };

        return repository.CrearAsync(contacto);
    }

    public async Task ActualizarAsync(int id, string nombre, string apellido, string telefono, string? email, bool favorito) {
        Contacto? contacto = await repository.ObtenerPorIdAsync(id);

        if (contacto is null) {
            return;
        }

        contacto.Nombre = nombre;
        contacto.Apellido = apellido;
        contacto.Telefono = telefono;
        contacto.Email = email;
        contacto.Favorito = favorito;

        await repository.ActualizarAsync(contacto);
    }

    public Task EliminarAsync(int id) {
        return repository.EliminarAsync(id);
    }
}
