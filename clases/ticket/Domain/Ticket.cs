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
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;
    
    public int OriginadoPorId { get; set; }
    public Usuario OriginadoPor { get; set; } = null!;
    
    public int? ResponsableId { get; set; }
    public Usuario? Responsable { get; set; }
    
    public ICollection<Accion> Acciones { get; set; } = new List<Accion>();
}