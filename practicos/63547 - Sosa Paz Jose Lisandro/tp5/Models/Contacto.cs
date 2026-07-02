using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El telefono es obligatorio")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingrese un correo valido")]
    public string Email { get; set; } = string.Empty;

    public string? Empresa { get; set; }

    public string? Cargo { get; set; }

    public string? Legajo { get; set; }

    public string? Direccion { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public string? Notas { get; set; }
}
