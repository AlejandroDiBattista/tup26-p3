using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

/// <summary>Persona o entidad almacenada en la agenda.</summary>
public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre admite hasta 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, ErrorMessage = "El apellido admite hasta 80 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
    [StringLength(40, ErrorMessage = "El teléfono admite hasta 40 caracteres.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
    [StringLength(160, ErrorMessage = "El correo admite hasta 160 caracteres.")]
    public string Email { get; set; } = "";

    [StringLength(120, ErrorMessage = "La empresa admite hasta 120 caracteres.")]
    public string Empresa { get; set; } = "";

    [StringLength(100, ErrorMessage = "El cargo admite hasta 100 caracteres.")]
    public string Cargo { get; set; } = "";

    [StringLength(240, ErrorMessage = "La dirección admite hasta 240 caracteres.")]
    public string Direccion { get; set; } = "";

    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(1000, ErrorMessage = "Las notas admiten hasta 1000 caracteres.")]
    public string Notas { get; set; } = "";
}
