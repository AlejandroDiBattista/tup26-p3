using System.ComponentModel.DataAnnotations;
namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = "";
    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    public string Telefono { get; set; } = "";
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    public string Email { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public int Legajo {get;set;} =0;
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}
