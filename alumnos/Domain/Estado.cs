namespace Tup26.AlumnosApp;

[Flags]
enum Estado : int {  // Usar valor único en el estado pero multiples en los filtros.
    Vacio       = 0b0000,
    Aprobado    = 0b0001,
    Pendiente   = 0b0010,
    Revision    = 0b0100,
    Desaprobado = 0b1000,
}

static class EstadoExtensions {
    public static string ToEmoji(this Estado estado) {
        return estado switch {
            Estado.Desaprobado => "🔴",
            Estado.Revision    => "🟠",
            Estado.Pendiente   => "🟡",
            Estado.Aprobado    => "🟢",
            Estado.Vacio       => "⚪️",
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
            _ => Estado.Vacio,
        };
    }
}
