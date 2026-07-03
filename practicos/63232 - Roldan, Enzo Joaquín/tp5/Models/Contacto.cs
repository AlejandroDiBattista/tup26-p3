using System.ComponentModel.DataAnnotations;

namespace AgendaWeb.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    public string CorreoElectronico { get; set; } = string.Empty;

    public string? Empresa { get; set; }

    public string? Cargo { get; set; }

    public string? Direccion { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public string? Notas { get; set; }

    public string NombreCompleto => $"{Nombre} {Apellido}";
}
