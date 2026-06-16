using System.ComponentModel.DataAnnotations;


namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = "";
    [Required, Phone(ErrorMessage = "Telefono invalido.")]
    public string Telefono { get; set; } = "";
    [Required, EmailAddress(ErrorMessage = "Correo invalido.")]
    public string Email { get; set; } = "";
    public string? Empresa { get; set; } = "";       // opcionales:
    public string? Cargo { get; set; } = "";         // string? admite null
    public string? Direccion { get; set; } = "";
    [DataType(DataType.Date)]
    public DateOnly? FechaNacimiento { get; set; }
    public string? Notas { get; set; } = "";
}
