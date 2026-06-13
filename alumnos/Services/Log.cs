using System.Text.RegularExpressions;

namespace Tup26.AlumnosApp;

static class Log {
    static readonly Regex formatoColorRegex = new(@"\[(?<frente>[^:\]\r\n]+)(?::(?<fondo>[^\]\r\n]+))?\]", RegexOptions.Compiled);

    public static void Escribir(string mensaje, ConsoleColor color = ConsoleColor.Black) {
        ConsoleColor colorAnterior = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(mensaje);
        Console.ForegroundColor = colorAnterior;
    }


    static public void WriteLine(string mensaje = "") {

        ConsoleColor calcular(string color) {
            return color.Trim().ToLowerInvariant() switch {
                "error" or "red" => ConsoleColor.Red,
                "warning" or "yellow" => ConsoleColor.Yellow,
                "debug" or "gray" => ConsoleColor.Gray,
                "info" or "cyan" => ConsoleColor.Cyan,
                "blue" => ConsoleColor.Blue,
                "green" => ConsoleColor.Green,
                "darkred" => ConsoleColor.DarkRed,
                "darkblue" => ConsoleColor.DarkBlue,
                "darkgreen" => ConsoleColor.DarkGreen,
                _ => ConsoleColor.White
            };
        }

        void EscribirTexto(string texto) {
            foreach (char caracter in texto) {
                if (caracter == '\r') { continue; }
                if (caracter == '\n') { Console.WriteLine(); continue; }
                Console.Write(caracter);
            }
        }

        int posicionActual = 0;

        foreach (Match coincidencia in formatoColorRegex.Matches(mensaje)) {
            if (coincidencia.Index > posicionActual) {
                string textoAnterior = mensaje.Substring(posicionActual, coincidencia.Index - posicionActual);
                EscribirTexto(textoAnterior);
            }

            string? frente = coincidencia.Groups?["frente"].Value;
            string? fondo = coincidencia.Groups?["fondo"].Value;

            if (!string.IsNullOrWhiteSpace(frente)) {
                Console.ForegroundColor = calcular(frente);
            }
            if (!string.IsNullOrWhiteSpace(fondo)) {
                Console.BackgroundColor = calcular(fondo);
            }

            posicionActual = coincidencia.Index + coincidencia.Length;
        }

        if (posicionActual < mensaje.Length) {
            string textoFinal = mensaje.Substring(posicionActual);
            EscribirTexto(textoFinal);
        }

        Console.WriteLine();
        Console.ResetColor();
    }

    public static void Debug(string mensaje)   => WriteLine($"[debug]{mensaje}");
    public static void Error(string mensaje)   => WriteLine($"[error]{mensaje}");
    public static void Info(string mensaje)    => WriteLine($"[info]{mensaje}");
    public static void Warning(string mensaje) => WriteLine($"[warning]{mensaje}");
    public static void Success(string mensaje) => WriteLine($"[green]{mensaje}");
    public static void Print(string mensaje)   => WriteLine($"[white]{mensaje}");
}
