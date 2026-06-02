namespace CrmAgente.Services;

// Canal de comunicacion entre las paginas y el AgentePanel.
// Guarda dos cosas:
//   1. Los IDs del ultimo filtro aplicado por el agente (para destacar en Contactos).
//   2. El contacto que el usuario esta viendo ahora mismo (para que el agente
//      actue sobre el sin necesidad de buscarlo).
public class AgenteFiltroService {
    // Resumen de un contacto candidato para mostrar en el panel del agente.
    public record ContactoResumen(int Id, string Nombre, string Empresa);

    // Resumen de una oportunidad para presentarla como tarjeta en el panel del agente.
    public record OportunidadResumen(int Id, string Contacto, string Descripcion, decimal Monto, string Etapa);

    // ---- Filtro de lista ----
    public IReadOnlyList<int>? ContactoIds { get; private set; }

    // ---- Filtro de oportunidades (pagina Pipeline) ----
    public IReadOnlyList<int>? OportunidadIds { get; private set; }

    // ---- Contacto activo (pagina Ficha) ----
    public int?    ContactoActualId     { get; private set; }
    public string? ContactoActualNombre { get; private set; }
    public string? ContactoActualEmpresa { get; private set; }

    // ---- Candidatos a desambiguar ----
    // Cuando una busqueda devuelve VARIOS contactos, los guardamos aca para que
    // el AgentePanel los muestre como una lista clicable y el usuario elija cual.
    public IReadOnlyList<ContactoResumen>? Candidatos { get; private set; }

    // ---- Oportunidades mostradas ----
    // Las oportunidades que el agente acaba de listar, para presentarlas como
    // tarjetas en el panel en vez de texto plano.
    public IReadOnlyList<OportunidadResumen>? OportunidadesMostradas { get; private set; }

    public event Action? OnChange;
    public event Action<int>? NavegarAFicha;
    public event Action? NavegarAPipeline;

    public void SolicitarNavegacion(int contactoId) =>
        NavegarAFicha?.Invoke(contactoId);

    public void SolicitarNavegacionPipeline() =>
        NavegarAPipeline?.Invoke();

    public void SetFiltro(IEnumerable<int> ids) {
        ContactoIds = ids.ToList();
        OnChange?.Invoke();
    }

    public void LimpiarFiltro() {
        ContactoIds = null;
        OnChange?.Invoke();
    }

    public void SetFiltroOportunidades(IEnumerable<int> ids) {
        OportunidadIds = ids.ToList();
        OnChange?.Invoke();
    }

    public void LimpiarFiltroOportunidades() {
        OportunidadIds = null;
        OnChange?.Invoke();
    }

    public void SetCandidatos(IEnumerable<ContactoResumen> candidatos) {
        Candidatos = candidatos.ToList();
        OnChange?.Invoke();
    }

    public void LimpiarCandidatos() {
        if (Candidatos == null) return;
        Candidatos = null;
        OnChange?.Invoke();
    }

    public void SetOportunidadesMostradas(IEnumerable<OportunidadResumen> oportunidades) {
        OportunidadesMostradas = oportunidades.ToList();
        OnChange?.Invoke();
    }

    public void LimpiarOportunidadesMostradas() {
        if (OportunidadesMostradas == null) return;
        OportunidadesMostradas = null;
        OnChange?.Invoke();
    }

    public void SetContactoActual(int id, string nombre, string empresa) {
        ContactoActualId      = id;
        ContactoActualNombre  = nombre;
        ContactoActualEmpresa = empresa;
        OnChange?.Invoke();
    }

    public void LimpiarContactoActual() {
        ContactoActualId      = null;
        ContactoActualNombre  = null;
        ContactoActualEmpresa = null;
        OnChange?.Invoke();
    }
}
