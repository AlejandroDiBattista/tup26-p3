using System.ComponentModel.DataAnnotations;
namespace tp5.Models;

public class Contacto
{[Required(ErrorMessage = "El ID es obligatorio.")]
    public int Id { get; set; }
  [Required(ErrorMessage = "El nombre es obligatorio.")]
public string Nombre { get; set; } = "";
[Required(ErrorMessage = "El Apellido es obligatorio.")]
    public string Apellido { get; set; } = "";
    [Required(ErrorMessage = "El telefono es obligatorio.")]
    public string Telefono { get; set; } = "";
    [Required(ErrorMessage = "El email es obligatorio.")]
    public string Email { get; set; } = "";
    
    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}
