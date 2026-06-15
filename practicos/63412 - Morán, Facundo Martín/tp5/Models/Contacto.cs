namespace tp5.Models;
using System.ComponentModel.DataAnnotations;
public class Contacto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre no puede exceder los 80 caracteres.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, ErrorMessage = "El apellido no puede exceder los 80 caracteres.")]
    public string Apellido { get; set; } = "";
    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres.")]
    [Phone(ErrorMessage = "El número de teléfono no es válido.")]
    public string Telefono { get; set; } = "";
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(100, ErrorMessage = "El correo electrónico no puede exceder los 100 caracteres.")]
    public string Email { get; set; } = "";
    [StringLength(100, ErrorMessage = "La empresa no puede exceder los 100 caracteres.")]
    public string Empresa { get; set; } = "";
    [StringLength(100, ErrorMessage = "El cargo no puede exceder los 100 caracteres.")]
    public string Cargo { get; set; } = "";
    [StringLength(200, ErrorMessage = "La dirección no puede exceder los 200 caracteres.")]
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    [StringLength(500, ErrorMessage = "Las notas no pueden exceder los 500 caracteres.")]
    public string Notas { get; set; } = "";
}
