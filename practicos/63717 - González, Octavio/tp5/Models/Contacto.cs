namespace tp5.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Entidad persistida en SQLite para cada contacto de la agenda.
/// El identificador se genera automáticamente; el resto de los campos
/// modela la información solicitada en el enunciado.
/// </summary>
public class Contacto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 80 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email debe tener un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "La empresa no puede superar los 80 caracteres.")]
    public string Empresa { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "El cargo no puede superar los 80 caracteres.")]
    public string Cargo { get; set; } = string.Empty;

    [StringLength(120, ErrorMessage = "La dirección no puede superar los 120 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
    public string Notas { get; set; } = string.Empty;
}
