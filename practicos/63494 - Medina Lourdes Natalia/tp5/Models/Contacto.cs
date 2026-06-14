using System.ComponentModel.DataAnnotations;

namespace tp5.Models;
public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80)]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    [StringLength(40)]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electronico valido.")]
    [StringLength(120)]
    public string Email { get; set; } = "";

    [StringLength(120)]
    public string Empresa { get; set; } = "";

    [StringLength(120)]
    public string Cargo { get; set; } = "";

    [StringLength(180)]
    public string Direccion { get; set; } = "";

    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(1000)]
    public string Notas { get; set; } = "";
}
