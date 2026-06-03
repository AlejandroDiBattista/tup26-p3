namespace Tup26.AlumnosApp;

[Flags]
public enum Estado : int {  // Usar valor único en el estado pero multiples en los filtros.
    Vacio = 0b00000,
    Aprobado = 0b00001,
    Pendiente = 0b00010,
    Revision = 0b00100,
    Desaprobado = 0b01000,
    Mal = 0b10000
}

static class EstadoExtensions {
    public static string ToEmoji(this Estado estado) {
        return estado switch {
            Estado.Desaprobado => "🔴",
            Estado.Revision => "🟠",
            Estado.Pendiente => "🟡",
            Estado.Aprobado => "🟢",
            Estado.Mal => "🟤",
            Estado.Vacio => "⚪️",
            _ => string.Empty
        };
    }

    public static Estado Parse(string? valor) {
        string? v = valor?.Trim().ToUpperInvariant();

        return v switch {
            "🔴" or "D" => Estado.Desaprobado,
            "🟠" or "R" => Estado.Revision,
            "🟡" or "P" => Estado.Pendiente,
            "🟢" or "A" => Estado.Aprobado,
            "🟤" or "M" => Estado.Mal,
            _ => Estado.Vacio,
        };
    }
}
