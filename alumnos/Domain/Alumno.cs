using System.Globalization;
using System.Text.RegularExpressions;

namespace Tup26.AlumnosApp;

public class Alumno {
    public int Legajo;
    public string Comision = "";
    public string Nombre = "";
    public string Apellido = "";
    public string Telefono = "";
    public string GitHub = "";
    public bool TieneFoto = false;
    public bool Presente = false;
    public int Asistencias = 0;
    public int Nota = 0;
    public string Codigo = "";

    public List<Estado> practicos = new();
    public List<Estado> examenes = new();

    public string NombreCompleto => $"{Apellido}, {Nombre}";
    public string CarpetaNombre => $"{Legajo} - {NombreCompleto}";
    public bool ConTelefono => !string.IsNullOrWhiteSpace(Telefono);
    public string TelefonoId => TelefonoID(Telefono);
    public bool ConGithub => EsGitHubValido(GitHub);
    public bool ConFoto => TieneFoto;

    public Alumno(int legajo, string comision, string nombre, string apellido, string telefono, string github, bool tieneFoto, bool presente = false, int asistencias = 0, int nota = 0, string codigo = "") {
        Legajo = legajo;
        Comision = NormalizarComision(comision);
        Nombre = NormalizarNombre(nombre);
        Apellido = NormalizarNombre(apellido);
        Telefono = NormalizarTelefono(telefono);
        GitHub = NormalizarGitHub(github);
        TieneFoto = tieneFoto;
        Presente = presente;
        Asistencias = asistencias;
        Nota = nota;
        Codigo = codigo.Trim();
    }

    public static int Comparar(Alumno a, Alumno b) {
        int comparacion = string.Compare(a.Comision, b.Comision);

        if (comparacion == 0) {
            comparacion = string.Compare(a.NombreCompleto, b.NombreCompleto);
        }

        if (comparacion == 0) {
            comparacion = a.Legajo.CompareTo(b.Legajo);
        }

        return comparacion;
    }

    public void Practico(int numero, Estado estado)
        => AsignarEstado(practicos, numero, estado);

    public void Examen(int numero, Estado estado)
        => AsignarEstado(examenes, numero, estado);

    public Estado EstadoPractico(int numero) => ObtenerEstado(practicos, numero);

    public Estado EstadoExamen(int numero) => ObtenerEstado(examenes, numero);

    static void AsignarEstado(List<Estado> estados, int numero, Estado estado) {
        if (numero <= 0) {
            return;
        }

        while (estados.Count < numero) {
            estados.Add(Estado.Vacio);
        }
        estados[numero - 1] = estado;
    }

    static Estado ObtenerEstado(List<Estado> estados, int numero) {
        if (numero <= 0 || numero > estados.Count) {
            return Estado.Vacio;
        }

        return estados[numero - 1];
    }

    static string NormalizarComision(string comision) {
        comision = Regex.Replace(comision, @"\D", "");
        return comision.Length > 0 ? $"C{comision}" : "(-)";
    }

    static string NormalizarNombre(string nombre) {
        nombre = Regex.Replace(nombre, @"^\s+|\s+$|\s+(?=\s)", "");
        nombre = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nombre);
        return nombre;
    }

    static string NormalizarGitHub(string github) {
        string valor = github.Trim().ToLowerInvariant();
        return EsMarcadorVacio(valor) ? string.Empty : valor;
    }

    static string NormalizarTelefono(string telefono) {
        string digitos = Regex.Replace(telefono, @"\D", "");

        if (string.IsNullOrWhiteSpace(digitos)) {
            return string.Empty;
        }

        if (digitos.StartsWith("549", StringComparison.Ordinal)) {
            digitos = digitos[3..];
        } else if (digitos.StartsWith("54", StringComparison.Ordinal)) {
            digitos = digitos[2..];
        }

        if (digitos.StartsWith("0", StringComparison.Ordinal)) {
            digitos = digitos[1..];
        }

        if (digitos.Length != 10) {
            return digitos;
        }

        if (digitos.StartsWith("11", StringComparison.Ordinal)) {
            return $"({digitos[..2]}){digitos.Substring(2, 4)}-{digitos.Substring(6, 4)}";
        }

        return $"({digitos[..3]}){digitos.Substring(3, 3)}-{digitos.Substring(6, 4)}";
    }

    static string TelefonoID(string telefono) {
        string digitos = Regex.Replace(NormalizarTelefono(telefono), @"\D", "");
        return string.IsNullOrWhiteSpace(digitos) ? string.Empty : $"549{digitos}";
    }

    static bool EsGitHubValido(string github) =>
        !string.IsNullOrWhiteSpace(github) &&
        !EsMarcadorVacio(github) &&
        !string.Equals(github, "(agregar)", StringComparison.OrdinalIgnoreCase);

    static bool EsMarcadorVacio(string valor) =>
        valor is "-" or "—" or "(-)" or "(—)" or "no";
}
