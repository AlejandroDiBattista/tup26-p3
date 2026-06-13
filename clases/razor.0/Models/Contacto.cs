namespace RazorHolaMundo.Models;

public class Contacto {
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string? Email { get; set; }
    public bool EsFavorito { get; set; }
}
