using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

/// <summary>
/// Representa una persona o entidad almacenada en la agenda.
/// </summary>
/// <remarks>
/// Las anotaciones se comparten entre los formularios de Blazor y la capa de
/// servicio. De esta manera, las mismas reglas se aplican antes de persistir
/// datos, incluso si el servicio se invoca desde otro componente.
/// </remarks>
public class Contacto
{
    /// <summary>Identificador generado por SQLite.</summary>
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre no puede superar los 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, ErrorMessage = "El apellido no puede superar los 80 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(40, ErrorMessage = "El teléfono no puede superar los 40 caracteres.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
    [StringLength(160, ErrorMessage = "El correo no puede superar los 160 caracteres.")]
    public string Email { get; set; } = "";

    [StringLength(120, ErrorMessage = "La empresa no puede superar los 120 caracteres.")]
    public string Empresa { get; set; } = "";

    [StringLength(100, ErrorMessage = "El cargo no puede superar los 100 caracteres.")]
    public string Cargo { get; set; } = "";

    [StringLength(240, ErrorMessage = "La dirección no puede superar los 240 caracteres.")]
    public string Direccion { get; set; } = "";

    [Display(Name = "Fecha de nacimiento")]
    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(1000, ErrorMessage = "Las notas no pueden superar los 1000 caracteres.")]
    public string Notas { get; set; } = "";
}
