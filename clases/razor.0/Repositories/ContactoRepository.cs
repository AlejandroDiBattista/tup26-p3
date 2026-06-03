using Microsoft.EntityFrameworkCore;
using RazorHolaMundo.Data;
using RazorHolaMundo.Models;

namespace RazorHolaMundo.Repositories;

public class ContactoRepository(AgendaDbContext dbContext) {
    public List<Contacto> GetAll() {
        return dbContext.Contactos
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToList();
    }

    public Contacto? GetById(int id) {
        return dbContext.Contactos.SingleOrDefault(contacto => contacto.Id == id);
    }

    public List<Contacto> Search(string? texto) {
        IQueryable<Contacto> query = dbContext.Contactos;

        if (!string.IsNullOrWhiteSpace(texto)) {
            string patron = $"%{texto.Trim()}%";

            query = query.Where(contacto =>
                EF.Functions.Like(contacto.Nombre, patron)
                || EF.Functions.Like(contacto.Apellido, patron)
                || EF.Functions.Like(contacto.Telefono, patron)
                || (contacto.Email != null && EF.Functions.Like(contacto.Email, patron))
            );
        }

        return query
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ToList();
    }

    public Contacto Add(Contacto contacto) {
        dbContext.Contactos.Add(contacto);
        dbContext.SaveChanges();

        return contacto;
    }

    public void Update(Contacto contacto) {
        dbContext.Contactos.Update(contacto);
        dbContext.SaveChanges();
    }

    public void Delete(Contacto contacto) {
        dbContext.Contactos.Remove(contacto);
        dbContext.SaveChanges();
    }
}
