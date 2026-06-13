using AgendaRazorHtmx.Dtos;
using AgendaRazorHtmx.Models;
using AgendaRazorHtmx.Repositories;

namespace AgendaRazorHtmx.Services;

public class ContactService(IContactRepository contactRepository) : IContactService {
    public List<Contact> GetAll(string? search = null) {
        return contactRepository.GetAll(search);
    }

    public Contact? GetById(int id) {
        return contactRepository.GetById(id);
    }

    public Contact Save(ContactInput input) {
        if (input.Id == 0) {
            Contact newContact = FromInput(input);

            return contactRepository.Add(newContact);
        }

        Contact contact = contactRepository.GetById(input.Id)
            ?? throw new InvalidOperationException($"No existe el contacto {input.Id}.");

        contact.Nombre = input.Nombre;
        contact.Apellido = input.Apellido;
        contact.Telefono = input.Telefono;
        contact.Email = input.Email;
        contact.Notas = input.Notas;

        return contactRepository.Update(contact);
    }

    public void Delete(int id) {
        contactRepository.Delete(id);
    }

    private static Contact FromInput(ContactInput input) {
        return new Contact {
            Nombre = input.Nombre,
            Apellido = input.Apellido,
            Telefono = input.Telefono,
            Email = input.Email,
            Notas = input.Notas
        };
    }
}
