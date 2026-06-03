namespace CrmAgente.Models;

// El Contacto es el centro del CRM: la persona o empresa con la que
// tenemos una relacion comercial. Tiene muchas Oportunidades (posibles
// ventas) y muchas Actividades (historial de interacciones).
public class Contacto {
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Empresa { get; set; } = "";

    public string Email { get; set; } = "";

    public string Telefono { get; set; } = "";

    public DateTime CreadoEn { get; set; } = DateTime.Now;

    // Relaciones 1-N: un contacto tiene muchas oportunidades y actividades.
    public List<Oportunidad> Oportunidades { get; set; } = new();

    public List<Actividad> Actividades { get; set; } = new();
}
