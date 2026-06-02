namespace Agenda.Domain;

public class Accion {
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int RegistradaPorId { get; set; }
    public DateTime Fecha { get; set; }
    public bool Realizada { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public Usuario RegistradaPor { get; set; } = null!;
}