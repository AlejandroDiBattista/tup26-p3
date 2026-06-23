using AgendaWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaWeb.Services
{
    public class ContactoService
    {
        private readonly IDbContextFactory<AgendaDbContext> _contextFactory;

        // Usamos una fábrica de contextos (DbContextFactory) porque en Blazor Server
        // es la manera segura de evitar hilos cruzados cuando se hacen consultas asíncronas.
        public ContactoService(IDbContextFactory<AgendaDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // 1. LEER (Obtener todos los contactos u ordenar/filtrar por texto)
        public async Task<List<Contacto>> GetContactosAsync(string buscarText = "")
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            IQueryable<Contacto> query = context.Contactos;

            if (!string.IsNullOrWhiteSpace(buscarText))
            {
                buscarText = buscarText.ToLower();
                // Filtra por nombre, apellido, empresa o email si el usuario escribe en la barra
                query = query.Where(c => 
                    c.Nombre.ToLower().Contains(buscarText) || 
                    c.Apellido.ToLower().Contains(buscarText) || 
                    (c.Empresa != null && c.Empresa.ToLower().Contains(buscarText)) ||
                    c.Email.ToLower().Contains(buscarText)
                );
            }

            // Los devolvemos ordenados alfabéticamente por Nombre y luego Apellido
            return await query.OrderBy(c => c.Nombre).ThenBy(c => c.Apellido).ToListAsync();
        }

        // 2. CONSULTAR DETALLE (Obtener un solo contacto por su ID)
        public async Task<Contacto?> GetContactoByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Contactos.FindAsync(id);
        }

        // 3. CREAR (Registrar un nuevo contacto)
        public async Task<Contacto> AddContactoAsync(Contacto contacto)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Contactos.Add(contacto);
            await context.SaveChangesAsync();
            return contacto;
        }

        // 4. MODIFICAR (Actualizar datos existentes)
        public async Task UpdateContactoAsync(Contacto contacto)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Entry(contacto).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        // 5. ELIMINAR (Quitar un contacto del sistema)
        public async Task DeleteContactoAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var contacto = await context.Contactos.FindAsync(id);
            if (contacto != null)
            {
                context.Contactos.Remove(contacto);
                await context.SaveChangesAsync();
            }
        }
    }
}