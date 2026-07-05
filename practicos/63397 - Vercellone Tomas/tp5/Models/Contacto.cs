using System.ComponentModel.DataAnnotations;

namespace tp5.Models
{
    public class Contacto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string Apellido { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public string Telefono { get; set; } = "";

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        public string Email { get; set; } = "";

        public string? Legajo { get; set; }
        public string? Empresa { get; set; }

        public string? Cargo { get; set; }

        public string? Direccion { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        public string? Notas { get; set; }

        
        public string ObtenerIniciales()
        {
            string inicialNombre = Nombre.Length > 0 ? Nombre[0].ToString() : "";
            string inicialApellido = Apellido.Length > 0 ? Apellido[0].ToString() : "";
            return (inicialNombre + inicialApellido).ToUpper();
        }

        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}