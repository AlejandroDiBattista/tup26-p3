namespace tp5.Models;

public class Contacto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Apellido { get; set; } = "";

    public string Telefono { get; set; } = "";

    public string Email { get; set; } = "";

    public string  Empresa { get; set; } = "";

    public string Cargo { get; set; } = "";

    public string Direccion { get; set; } = "";

    public int Legajo { get; set; } = 0;

    public DateOnly? FechaNacimiento { get; set; }

    public string Notas { get; set; } = "";

    public string NombreCompleto => $"{Nombre} {Apellido}";

    public string Iniciales => $"{(Nombre.Length > 0 ? Nombre[0] : ' ')}{(Apellido.Length > 0 ? Apellido[0] : ' ')}".Trim().ToUpper();
}
