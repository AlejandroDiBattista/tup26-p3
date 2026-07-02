using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string Telefono { get; set; } = string.Empty;
    public int Legajo {get; set;} = 0;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(200, ErrorMessage = "El correo no puede superar los 200 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "La empresa no puede superar los 100 caracteres.")]
    public string Empresa { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El cargo no puede superar los 100 caracteres.")]
    public string Cargo { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
    public string Notas { get; set; } = string.Empty;

    public bool EsNuevo => Id == 0;

    public Contacto Clonar() => (Contacto)MemberwiseClone();
}
