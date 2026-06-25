using tp5.Data;
using tp5.Models;
using Microsoft.EntityFrameworkCore;

namespace tp5.Services
{
    public class ContactosService
    {
        private readonly ContactosContext _context;

        public ContactosService(ContactosContext context)
        {
            _context = context;
        }

        public async Task<List<Contacto>> ObtenerTodosAsync()
        {
            return await _context.Contactos
            .OrderBy(c=> c.Apellido)
            .ThenBy(c=> c.Nombre)
            .ToListAsync();
        }

        public async Task<List<Contacto>> BuscarAsync(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
            return await ObtenerTodosAsync();

            var terminoLower = termino.ToLower();
            return await _context.Contactos
            .Where(c => c.Nombre.ToLower().Contains(terminoLower) ||
                        c.Apellido.ToLower().Contains(terminoLower) ||
                        c.Email.ToLower().Contains(terminoLower))
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
        }

        public async Task<Contacto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Contactos.FindAsync(id);
        }

        public async Task<Contacto> CrearAsync(Contacto contacto)
        {
            _context.Contactos.Add(contacto);
            await _context.SaveChangesAsync();
            return contacto;
        }

        public async Task ActualizarAsync(Contacto contacto)
        {
            var original = await _context.Contactos.FindAsync(contacto.Id);

            if(original != null)
            {
                _context.Entry(original).CurrentValues.SetValues(contacto);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<bool> EliminarAsync(int id)
        {
            var contacto = await ObtenerPorIdAsync(id);
            if (contacto == null)
                return false;

            _context.Contactos.Remove(contacto);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}