using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <-- ESTO ES NUEVO

namespace tp5.Models
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
        [Column("Email")] // <-- AGREGÁ ESTA LÍNEA AQUÍ
        public string CorreoElectronico { get; set; } = string.Empty;

        public string? Empresa { get; set; }
        public string? Cargo { get; set; }
        public string? Direccion { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }
        
        public string? Notas { get; set; }
    }
}