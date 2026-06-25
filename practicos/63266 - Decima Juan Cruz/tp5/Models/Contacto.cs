namespace tp5.Models;

using System.ComponentModel.DataAnnotations;
public class Contacto {
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
    public string Email { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}
