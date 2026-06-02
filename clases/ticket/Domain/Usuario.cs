namespace Agenda.Domain;

public enum TipoUsuario {
    Cliente,
    Interno
}

public class Usuario {
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TipoUsuario Tipo { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string? Token { get; set; }
    public DateTime? TokenExpira { get; set; }
    public ICollection<Ticket> TicketsOriginados { get; set; } = new List<Ticket>();
    public ICollection<Ticket> TicketsAsignados { get; set; } = new List<Ticket>();
}