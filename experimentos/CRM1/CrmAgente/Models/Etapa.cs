namespace CrmAgente.Models;

// Las etapas del pipeline (embudo de ventas). Una oportunidad avanza
// por estas etapas hasta cerrarse como Ganada o Perdida.
public enum Etapa {
    Prospecto,
    Contactado,
    Propuesta,
    Negociacion,
    Ganada,
    Perdida
}
