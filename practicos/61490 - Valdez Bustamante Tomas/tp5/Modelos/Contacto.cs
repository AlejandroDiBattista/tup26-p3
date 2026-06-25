using System.ComponentModel.DataAnnotations;

namespace AgendaWeb.Modelos;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "El teléfono no es válido.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    public string Email { get; set; } = "";

    // Datos opcionales
    public string? Empresa { get; set; }
    public string? Cargo { get; set; }
    public string? Direccion { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? FechaNacimiento { get; set; }

    public string? Notas { get; set; }

    // Se calcula al momento, no se guarda en la base.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string NombreCompleto => $"{this.Apellido}, {this.Nombre}";
}
