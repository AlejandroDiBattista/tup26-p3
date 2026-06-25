namespace tp5.Models;
using System.ComponentModel.DataAnnotations;
public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electronico valido.")]
    public string Email { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}
