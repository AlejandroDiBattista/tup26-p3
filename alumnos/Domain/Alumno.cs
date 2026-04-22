namespace Tup26.AlumnosApp;

class Alumno {
    public int Legajo;
    public string Comision = string.Empty;
    public string Nombre = string.Empty;
    public string Apellido = string.Empty;
    public string Telefono = string.Empty;
    public bool TieneFoto;
    public string GitHub = string.Empty;
    public List<Estado> practicos = new();
    public List<Estado> examenes = new();

    public string NombreCompleto => $"{Apellido.Trim()}, {Nombre.Trim()}";
    public string CarpetaNombre => $"{Legajo} - {NombreCompleto}";
    public string TelefonoId => FormatearTelefonoId(Telefono);
    public bool ConGithub => !string.IsNullOrWhiteSpace(GitHub) && GitHub.Length > 3;
    public bool ConFoto => TieneFoto;
    public bool ConTelefono => !string.IsNullOrWhiteSpace(Telefono);

    public static int Comparar(Alumno alumnoA, Alumno alumnoB) {
        int comparacion = string.Compare(FormatearComision(alumnoA), FormatearComision(alumnoB), StringComparison.OrdinalIgnoreCase);
        if (comparacion != 0) { return comparacion; }

        comparacion = string.Compare(FormatearTexto(alumnoA.Apellido), FormatearTexto(alumnoB.Apellido), StringComparison.OrdinalIgnoreCase);
        if (comparacion != 0) { return comparacion; }

        comparacion = string.Compare(FormatearTexto(alumnoA.Nombre), FormatearTexto(alumnoB.Nombre), StringComparison.OrdinalIgnoreCase);
        if (comparacion != 0) { return comparacion; }

        return alumnoA.Legajo.CompareTo(alumnoB.Legajo);
    }

    public void Practico(int numero, Estado estado) {
        while (practicos.Count < numero) {
            practicos.Add(Estado.Vacio);
        }

        practicos[numero - 1] = estado;
    }

    public void Examen(int numero, Estado estado) {
        while (examenes.Count < numero) {
            examenes.Add(Estado.Vacio);
        }
        examenes[numero - 1] = estado;
    }

    static string FormatearTelefonoId(string telefono) {
        string digitos = Regex.Replace(telefono.Trim(), @"\D", string.Empty);

        if (string.IsNullOrWhiteSpace(digitos)) { return string.Empty; }

        if (digitos.StartsWith("549")) { return digitos; }

        if (digitos.StartsWith("54")) { return $"549{digitos.Substring(2)}"; }

        if (digitos.StartsWith("0")) {
            digitos = digitos.Substring(1);
        }

        return $"549{digitos}";
    }

    static string FormatearTexto(string texto) {
        if (string.IsNullOrWhiteSpace(texto)) { return "—"; }

        return texto.Trim();
    }

    static string FormatearComision(Alumno alumno) {
        return FormatearTexto(alumno.Comision);
    }
}
