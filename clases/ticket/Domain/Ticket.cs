namespace Agenda.Domain;

public enum EstadoTicket {
    Abierto,
    EnProceso,
    Cerrado
}

public class Ticket {
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int OriginadoPorId { get; set; }
    public int? ResponsableId { get; set; }
    public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public Usuario OriginadoPor { get; set; } = null!;
    public Usuario? Responsable { get; set; }
    public ICollection<Accion> Acciones { get; set; } = new List<Accion>();
}