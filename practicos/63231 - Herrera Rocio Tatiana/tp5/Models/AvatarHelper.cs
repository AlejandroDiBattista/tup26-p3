namespace tp5.Models;

public static class AvatarHelper
{
    private static readonly string[] paleta =
    {
        "#cfe2ff", "#d1e7dd", "#e2d9f3", "#fff3cd",
        "#f8d7da", "#cff4fc", "#e8d5c4"
    };

    public static string Iniciales(string nombre, string apellido)
    {
        var letra1 = nombre.Length > 0 ? nombre[0].ToString() : "";
        var letra2 = apellido.Length > 0 ? apellido[0].ToString() : "";
        return $"{letra1}{letra2}".ToUpperInvariant();
    }

    public static string Color(string nombre, string apellido)
    {
        var clave = nombre + apellido;
        var hash = Math.Abs(clave.GetHashCode());
        return paleta[hash % paleta.Length];
    }
}