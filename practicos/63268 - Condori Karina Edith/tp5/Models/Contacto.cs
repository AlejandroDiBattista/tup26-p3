using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre no puede superar 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, ErrorMessage = "El apellido no puede superar 80 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "Ingrese un teléfono válido.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string Email { get; set; } = "";

    [StringLength(120)]
    public string Empresa { get; set; } = "";

    [StringLength(120)]
    public string Cargo { get; set; } = "";

    [StringLength(200)]
    public string Direccion { get; set; } = "";

    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(1000)]
    public string Notas { get; set; } = "";
}
