using RazorHolaMundo.Models;
using RazorHolaMundo.Repositories;

namespace RazorHolaMundo.Services;

public class AgendaService(ContactoRepository contactos) {
    public List<Contacto> Listar() {
        return contactos.GetAll();
    }

    public List<Contacto> Buscar(string? texto) {
        return contactos.Search(texto);
    }

    public Contacto? Obtener(int id) {
        return contactos.GetById(id);
    }

    public Contacto Crear(ContactoInput input) {
        Contacto contacto = new() {
            Nombre = input.Nombre.Trim(),
            Apellido = input.Apellido.Trim(),
            Telefono = input.Telefono.Trim(),
            Email = NormalizarEmail(input.Email),
            EsFavorito = input.EsFavorito
        };

        return contactos.Add(contacto);
    }

    public bool Actualizar(int id, ContactoInput input) {
        Contacto? contacto = contactos.GetById(id);

        if (contacto is null) {
            return false;
        }

        contacto.Nombre = input.Nombre.Trim();
        contacto.Apellido = input.Apellido.Trim();
        contacto.Telefono = input.Telefono.Trim();
        contacto.Email = NormalizarEmail(input.Email);
        contacto.EsFavorito = input.EsFavorito;

        contactos.Update(contacto);

        return true;
    }

    public bool Eliminar(int id) {
        Contacto? contacto = contactos.GetById(id);

        if (contacto is null) {
            return false;
        }

        contactos.Delete(contacto);

        return true;
    }

    public bool CambiarFavorito(int id) {
        Contacto? contacto = contactos.GetById(id);

        if (contacto is null) {
            return false;
        }

        contacto.EsFavorito = !contacto.EsFavorito;
        contactos.Update(contacto);

        return true;
    }

    private static string? NormalizarEmail(string? email) {
        if (string.IsNullOrWhiteSpace(email)) {
            return null;
        }

        return email.Trim();
    }
}
