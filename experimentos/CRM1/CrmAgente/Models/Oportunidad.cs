namespace CrmAgente.Models;

// Una Oportunidad es una venta posible y concreta para un contacto.
// Tiene un monto estimado y una etapa dentro del pipeline. Es el
// corazon de lo que un CRM permite medir: cuanta plata hay en juego
// y en que punto de la negociacion esta cada negocio.
public class Oportunidad {
    public int Id { get; set; }

    public string Descripcion { get; set; } = "";

    public decimal Monto { get; set; }

    public Etapa Etapa { get; set; } = Etapa.Prospecto;

    public DateTime CreadoEn { get; set; } = DateTime.Now;

    // Clave foranea hacia el contacto duenio de esta oportunidad.
    public int ContactoId { get; set; }

    public Contacto? Contacto { get; set; }
}
