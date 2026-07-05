using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Requerido")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Requerido")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "Requerido")]
    public string Telefono { get; set; } = "";

    public int Legajo { get; set; } = 0;

    [Required(ErrorMessage = "Requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = "";

    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}