namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string CorreoElectronico { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Notas { get; set; } = "";
}
