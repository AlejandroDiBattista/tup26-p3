using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido")]
    public string Email { get; set; } = "";

    public string Empresa { get; set; } = "";

    public string Cargo { get; set; } = "";

    public string Direccion { get; set; } = "";

    public DateOnly? FechaNacimiento { get; set; }

    public string Notas { get; set; } = "";

    public int? Legajo { get; set; }


    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

    public string Iniciales
    {
        get
        {
            var n = string.IsNullOrWhiteSpace(Nombre) ? "" : Nombre.Trim()[..1];
            var a = string.IsNullOrWhiteSpace(Apellido) ? "" : Apellido.Trim()[..1];
            var iniciales = $"{n}{a}".ToUpper();
            return iniciales == "" ? "?" : iniciales;
        }
    }


    public Contacto Clonar() => (Contacto)MemberwiseClone();
}
