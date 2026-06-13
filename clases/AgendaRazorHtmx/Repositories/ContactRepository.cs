using AgendaRazorHtmx.Data;
using AgendaRazorHtmx.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaRazorHtmx.Repositories;

public class ContactRepository(IDbContextFactory<AgendaDbContext> dbContextFactory) : IContactRepository {
    public List<Contact> GetAll(string? search = null) {
        using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();
        IQueryable<Contact> query = dbContext.Contacts
            .OrderBy(contact => contact.Apellido)
            .ThenBy(contact => contact.Nombre);

        if (!string.IsNullOrWhiteSpace(search)) {
            string term = search.Trim();

            query = query.Where(contact =>
                EF.Functions.Like(contact.Nombre, $"%{term}%")
                || EF.Functions.Like(contact.Apellido, $"%{term}%")
                || EF.Functions.Like(contact.Telefono, $"%{term}%")
                || EF.Functions.Like(contact.Email, $"%{term}%")
                || EF.Functions.Like(contact.Notas, $"%{term}%")
            );
        }

        return query.ToList();
    }

    public Contact? GetById(int id) {
        using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();

        return dbContext.Contacts.SingleOrDefault(contact => contact.Id == id);
    }

    public Contact Add(Contact contact) {
        using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();

        dbContext.Contacts.Add(contact);
        dbContext.SaveChanges();

        return contact;
    }

    public Contact Update(Contact contact) {
        using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();

        dbContext.Contacts.Update(contact);
        dbContext.SaveChanges();

        return contact;
    }

    public void Delete(int id) {
        using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();
        Contact? contact = dbContext.Contacts.SingleOrDefault(existingContact => existingContact.Id == id);

        if (contact is null) { return; }

        dbContext.Contacts.Remove(contact);
        dbContext.SaveChanges();
    }
}
