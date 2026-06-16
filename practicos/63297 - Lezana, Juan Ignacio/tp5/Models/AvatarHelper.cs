namespace tp5.Models;

public static class AvatarHelper
{
    static string[] colores = {
        "#cfe2ff", "#d1e7dd", "#e2d9f3", "#fff3cd", "#f8d7da", "#cff4fc", "#e8d5c4"
    };

    public static string Iniciales(string nom, string ape)
    {
        string i1 = nom.Length > 0 ? nom.Substring(0, 1) : "";
        string i2 = ape.Length > 0 ? ape.Substring(0, 1) : "";
        return (i1 + i2).ToUpper();
    }

    public static string Color(string nom, string ape)
    {
        int h = (nom + ape).GetHashCode();
        if (h < 0) h = -h;
        return colores[h % colores.Length];
    }
}