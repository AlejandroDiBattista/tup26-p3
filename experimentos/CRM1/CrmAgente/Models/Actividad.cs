namespace CrmAgente.Models;

// Una Actividad es cada interaccion con un contacto: una llamada, un
// mail, una reunion, una nota. Forman el historial cronologico que
// permite saber que paso y cuando, sin depender de la memoria de nadie.
public class Actividad {
    public int Id { get; set; }

    // Tipo libre pero pensado para: Llamada, Mail, Reunion, Nota.
    public string Tipo { get; set; } = "Nota";

    public string Detalle { get; set; } = "";

    public DateTime Fecha { get; set; } = DateTime.Now;

    // Clave foranea obligatoria hacia el contacto.
    public int ContactoId { get; set; }

    public Contacto? Contacto { get; set; }
}
