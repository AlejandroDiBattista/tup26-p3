using Microsoft.EntityFrameworkCore;
using tp5.Datos;
using tp5.Models;

namespace tp5.Servicios
{
    public class ContactoService
    {
        private readonly AgendaContext _context;

        public ContactoService(AgendaContext context)
        {
            _context = context;
        }

        public async Task<List<Contacto>> ObtenerContactosAsync()
        {
            return await _context.Contactos.ToListAsync();
        }

        public async Task<List<Contacto>> BuscarContactosAsync(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return await ObtenerContactosAsync();

            return await _context.Contactos
                .Where(c => c.Nombre.ToLower().Contains(termino.ToLower()) || 
                            c.Apellido.ToLower().Contains(termino.ToLower()))
                .ToListAsync();
        }

        public async Task AgregarContactoAsync(Contacto nuevoContacto)
        {
            _context.Contactos.Add(nuevoContacto);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarContactoAsync(Contacto contactoModificado)
        {
            _context.Contactos.Update(contactoModificado);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarContactoAsync(int id)
        {
            var contacto = await _context.Contactos.FindAsync(id);
            if (contacto != null)
            {
                _context.Contactos.Remove(contacto);
                await _context.SaveChangesAsync();
            }
        }
    }
}