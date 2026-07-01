using System.ComponentModel.DataAnnotations;
namespace tp5.Models;
public class Contacto
{


    public int Id { get; set; }
    [Required(ErrorMessage ="tiene que ser obligatorio el nombre")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage="tiene que ser obligatorio el apellido")]
    public string Apellido { get; set; } = "";
    [Required(ErrorMessage ="El telefono debe ser obligatorio")]
    public string Telefono { get; set; } = "";
    [Required(ErrorMessage ="El correo debe ser obligatorio")]
    [EmailAddress(ErrorMessage ="ingrese un correo valido")]
    public string Email { get; set; } = "";
    
    [Required(ErrorMessage ="La empresa debe ser obligatoria")]
    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}
