using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    [Required(ErrorMessage="el nombre es obligatorio")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage="el apellido es obligatorio")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage="el telefono es obligatorio")]
    [Phone(ErrorMessage="el telefono deben ser numero")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage="el email es obligatorio")]
    [EmailAddress(ErrorMessage ="debe ingresar un email valido")]
    public string Email { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}