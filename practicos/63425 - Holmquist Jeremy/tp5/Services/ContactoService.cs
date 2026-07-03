using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;

namespace tp5.Services
{
    public class ContactoService
    {
        private readonly AppDbContext _db;

        public ContactoService(AppDbContext db) => _db = db;

        public Task<List<Contacto>> GetAllAsync(string? filtro = null)
        {
            var query = _db.Contactos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var term = filtro.Trim().ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(term) ||
                    c.Apellido.ToLower().Contains(term) ||
                    c.Telefono.Contains(term) ||
                    c.CorreoElectronico.ToLower().Contains(term) ||
                    (c.Empresa != null && c.Empresa.ToLower().Contains(term)) ||
                    (c.Cargo != null && c.Cargo.ToLower().Contains(term)));
            }
            return query.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToListAsync();
        }

        public Task<Contacto?> GetByIdAsync(int id) =>
            _db.Contactos.FindAsync(id).AsTask();

        public async Task AddAsync(Contacto contacto)
        {
            _db.Contactos.Add(contacto);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Contacto contacto)
        {
            _db.Contactos.Update(contacto);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var c = await _db.Contactos.FindAsync(id);
            if (c is not null)
            {
                _db.Contactos.Remove(c);
                await _db.SaveChangesAsync();
            }
        }
    }
}
