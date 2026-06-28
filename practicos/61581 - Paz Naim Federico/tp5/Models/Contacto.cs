using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre no puede superar los 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, ErrorMessage = "El apellido no puede superar los 80 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    [StringLength(40, ErrorMessage = "El telefono no puede superar los 40 caracteres.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electronico valido.")]
    [StringLength(120, ErrorMessage = "El correo electronico no puede superar los 120 caracteres.")]
    public string Email { get; set; } = "";

    [StringLength(120, ErrorMessage = "La empresa no puede superar los 120 caracteres.")]
    public string Empresa { get; set; } = "";

    [StringLength(120, ErrorMessage = "El cargo no puede superar los 120 caracteres.")]
    public string Cargo { get; set; } = "";

    [StringLength(180, ErrorMessage = "La direccion no puede superar los 180 caracteres.")]
    public string Direccion { get; set; } = "";

    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
    public string Notas { get; set; } = "";
}
