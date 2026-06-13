namespace AgendaRazorHtmx.Models;

public class Contact {
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
}
