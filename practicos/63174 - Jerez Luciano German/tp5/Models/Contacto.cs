using System;
using System.ComponentModel.DataAnnotations;

namespace AgendaWeb.Data
{
    public class Contacto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [Phone(ErrorMessage = "Formato de teléfono inválido.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        public string Email { get; set; } = string.Empty;

        public string? Empresa { get; set; }
        public string? Cargo { get; set; }
        public string? Direccion { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Notas { get; set; }

        // Propiedad calculada para mostrar el nombre completo en la lista
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
        
        // Iniciales para el avatar circular
        public string Iniciales => 
            $"{(Nombre.Length > 0 ? Nombre[0].ToString() : "")}{(Apellido.Length > 0 ? Apellido[0].ToString() : "")}".ToUpper();
    }
}