using AgendaRazorHtmx.Dtos;
using AgendaRazorHtmx.Models;

namespace AgendaRazorHtmx.Services;

public interface IContactService {
    List<Contact> GetAll(string? search = null);
    Contact? GetById(int id);
    Contact Save(ContactInput input);
    void Delete(int id);
}
