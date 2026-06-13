using AgendaRazorHtmx.Models;

namespace AgendaRazorHtmx.Repositories;

public interface IContactRepository {
    List<Contact> GetAll(string? search = null);
    Contact? GetById(int id);
    Contact Add(Contact contact);
    Contact Update(Contact contact);
    void Delete(int id);
}
