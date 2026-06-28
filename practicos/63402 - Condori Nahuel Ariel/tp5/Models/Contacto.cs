using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

public class Contacto
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El Nombre es obligatorio")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El Apellido es obligatorio")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El Teléfono es obligatorio")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El Email es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    public string Email { get; set; } = "";

    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}