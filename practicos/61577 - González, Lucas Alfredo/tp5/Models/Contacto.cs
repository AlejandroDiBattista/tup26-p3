using System.ComponentModel.DataAnnotations;

namespace tp5.Models;

/// <summary>
/// Representa una persona o entidad almacenada en la agenda.
/// </summary>
/// <remarks>
/// Las anotaciones cumplen dos funciones: describen las restricciones que
/// Entity Framework aplica al modelo y permiten que los formularios de Blazor
/// muestren mensajes de validación antes de intentar persistir datos inválidos.
/// Los campos opcionales se guardan como cadenas vacías porque la base provista
/// los declaró como <c>NOT NULL</c>.
/// </remarks>
public class Contacto
{
    /// <summary>Identificador autoincremental administrado por SQLite.</summary>
    public int Id { get; set; }

    /// <summary>Nombre de pila o denominación principal del contacto.</summary>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre no puede superar los 80 caracteres.")]
    public string Nombre { get; set; } = "";

    /// <summary>Apellido o denominación secundaria del contacto.</summary>
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80, ErrorMessage = "El apellido no puede superar los 80 caracteres.")]
    public string Apellido { get; set; } = "";

    /// <summary>Número telefónico conservado como texto para admitir prefijos y separadores.</summary>
    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "Ingresá un número de teléfono válido.")]
    [StringLength(40, ErrorMessage = "El teléfono no puede superar los 40 caracteres.")]
    public string Telefono { get; set; } = "";

    /// <summary>Dirección de correo electrónico principal.</summary>
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresá una dirección de correo válida.")]
    [StringLength(160, ErrorMessage = "El correo no puede superar los 160 caracteres.")]
    public string Email { get; set; } = "";

    /// <summary>Empresa u organización a la que pertenece; puede quedar vacía.</summary>
    [StringLength(120, ErrorMessage = "La empresa no puede superar los 120 caracteres.")]
    public string Empresa { get; set; } = "";

    /// <summary>Puesto o función laboral; puede quedar vacío.</summary>
    [StringLength(120, ErrorMessage = "El cargo no puede superar los 120 caracteres.")]
    public string Cargo { get; set; } = "";

    /// <summary>Domicilio o dirección postal; puede quedar vacío.</summary>
    [StringLength(240, ErrorMessage = "La dirección no puede superar los 240 caracteres.")]
    public string Direccion { get; set; } = "";

    /// <summary>Fecha de nacimiento cuando el contacto decide informarla.</summary>
    public DateOnly? FechaNacimiento { get; set; }

    /// <summary>Observaciones libres útiles para recordar el vínculo con el contacto.</summary>
    [StringLength(1000, ErrorMessage = "Las notas no pueden superar los 1000 caracteres.")]
    public string Notas { get; set; } = "";
}
