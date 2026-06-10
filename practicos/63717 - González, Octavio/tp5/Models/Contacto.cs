namespace tp5.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.ComponentModel.DataAnnotations;

public class Contacto

{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, MinimumLength = 2,
        ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, MinimumLength = 2,
        ErrorMessage = "El apellido debe tener entre 2 y 80 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [MaxLength(20)]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "La empresa es obligatoria.")]
    [MaxLength(80)]
    public string Empresa { get; set; } = "";

    public string Cargo { get; set; } = "";

    public string Direccion { get; set; } = "";
  
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}
//DB CONTEXT. 
class ContactoDb : DbContext
{
    public ContactoDb(DbContextOptions<ContactoDb> options) : base(options) { }
    public DbSet<Contacto> Contactos => Set<Contacto>();
}