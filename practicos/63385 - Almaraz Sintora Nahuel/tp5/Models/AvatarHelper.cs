namespace tp5.Models;

public static class AvatarHelper
{
    static readonly string[] Colores = new[]
    {
        "#cfe2ff", "#d1e7dd", "#e2d9f3", "#fff3cd", "#f8d7da", "#cff4fc", "#e8d5c4"
    };

    public static string Iniciales(string nombre, string apellido)
    {
        var n = nombre.Length > 0 ? nombre[0].ToString() : "";
        var a = apellido.Length > 0 ? apellido[0].ToString() : "";
        return (n + a).ToUpper();
    }

    public static string Color(string nombre, string apellido)
    {
        var hash = (nombre + apellido).GetHashCode();
        var index = Math.Abs(hash) % Colores.Length;
        return Colores[index];
    }
}