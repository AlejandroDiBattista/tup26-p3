namespace tp5.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

class Repositorio
{
    private readonly IDbContextFactory<ContactoDb> db;
    public Repositorio(IDbContextFactory<ContactoDb> db) => this.db = db;

    public void Iniciar() 
    { 
        using var dd =  this.db.CreateDbContext(); 
        dd.Database.EnsureCreated();
    } 

    public async Task<List<Contacto>> TraerContactos() {
        using var dd = await this.db.CreateDbContextAsync(); 
        return await dd.Contactos.OrderBy(p => p.Id).ToListAsync();
    }
    public async Task<Contacto?> TraerContacto(int id) {
    using var dd = await this.db.CreateDbContextAsync(); 
    return await dd.Contactos.FirstOrDefaultAsync(p => p.Id == id);

    }
    public async Task<Contacto> AgregarContacto(Contacto contacto)
    {
        using var dd = await this.db.CreateDbContextAsync(); 
        dd.Contactos.Add(contacto);
        await dd.SaveChangesAsync();
        return contacto;
    }
    public async Task<Contacto?> Actualizar(Contacto actualizacion)
    {
    using var dd = await this.db.CreateDbContextAsync(); 
    var cambio = dd.Contactos.FirstOrDefault(p => p.Id == actualizacion.Id);

        if (cambio is null) return null;
        cambio.Nombre = actualizacion.Nombre;
        cambio.Apellido = actualizacion.Apellido;
        cambio.Telefono = actualizacion.Telefono;
        cambio.Email = actualizacion.Email;
        cambio.Empresa = actualizacion.Empresa;
        cambio.Cargo = actualizacion.Cargo;
        cambio.Direccion = actualizacion.Direccion;
        cambio.Notas = actualizacion.Notas;
        await dd.SaveChangesAsync();
        return cambio;
    }

    public async Task<bool> Eliminar(int id)
    {
    using var dd = await this.db.CreateDbContextAsync(); 
    var eliminado = dd.Contactos.FirstOrDefault(p => p.Id == id);
    if (eliminado is null) return false;
    dd.Contactos.Remove(eliminado);
    await dd.SaveChangesAsync();
        return true;
    }
}

