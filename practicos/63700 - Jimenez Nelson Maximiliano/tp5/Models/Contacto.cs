using System.ComponentModel.DataAnnotations;

namespace AgendaWeb.Data;

public class Contacto
{
    public int Id { get; set; }

    public int Legajo { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electronico no tiene un formato valido.")]
    public string Email { get; set; } = string.Empty;

    public string? Empresa { get; set; }

    public string? Cargo { get; set; }

    public string? Direccion { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public string? Notas { get; set; }
}
