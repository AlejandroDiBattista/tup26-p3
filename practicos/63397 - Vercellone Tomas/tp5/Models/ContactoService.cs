using Microsoft.EntityFrameworkCore;

namespace tp5.Models
{
    public class ContactoService
    {
        private readonly AgendaDbContext _db;

        public ContactoService(AgendaDbContext db)
        {
            _db = db;
        }

       
        public async Task<List<Contacto>> ObtenerTodosAsync(string? busqueda = null)
        {
            var query = _db.Contactos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string textoBusqueda = busqueda.ToLower();

                var lista = await query.ToListAsync();
                var filtrados = new List<Contacto>();

                foreach (var c in lista)
                {
                    bool coincideNombre = c.Nombre.ToLower().Contains(textoBusqueda);
                    bool coincideApellido = c.Apellido.ToLower().Contains(textoBusqueda);
                    bool coincideEmpresa = c.Empresa != null && c.Empresa.ToLower().Contains(textoBusqueda);
                    bool coincideEmail = c.Email.ToLower().Contains(textoBusqueda);

                    if (coincideNombre || coincideApellido || coincideEmpresa || coincideEmail)
                    {
                        filtrados.Add(c);
                    }
                }

                return filtrados;
            }

            return await query.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToListAsync();
        }

        public async Task<Contacto?> ObtenerPorIdAsync(int id)
        {
            return await _db.Contactos.FindAsync(id);
        }

        public async Task AgregarAsync(Contacto contacto)
        {
            _db.Contactos.Add(contacto);
            await _db.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Contacto contacto)
        {
            _db.Contactos.Update(contacto);
            await _db.SaveChangesAsync();
        }

        public async Task EliminarAsync(int id)
        {
            var contacto = await _db.Contactos.FindAsync(id);
            if (contacto != null)
            {
                _db.Contactos.Remove(contacto);
                await _db.SaveChangesAsync();
            }
        }
    }
}