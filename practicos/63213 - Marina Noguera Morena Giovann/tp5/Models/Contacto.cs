using System;
using Microsoft.EntityFrameworkCore;

namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = string.Empty;
}

public class AgendaContext : DbContext
{
    public AgendaContext(DbContextOptions<AgendaContext> options) : base(options) { }
    public DbSet<Contacto> Contactos { get; set; }
}