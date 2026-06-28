using Microsoft.Playwright;
using System.Net;

namespace Tup26.AlumnosApp;

static class CapturaTerminalPracticos {
    const int Columnas = 100;
    const int Filas = 30;
    const int TiempoMaximoCapturaMs = 12_000;
    const string NombreCapturaTp6 = "captura-tp6.png";

    public static CapturaPantallaResultado CapturarTp6(string rutaPractico, bool forzar) {
        if (!OperatingSystem.IsMacOS()) {
            return new(
                EstadoCapturaPantalla.Omitida,
                null,
                null,
                ["La captura de TUI automatizada está disponible solo en macOS."]);
        }

        string? asistente = SeleccionarAsistenteCs(rutaPractico);
        if (asistente is null) {
            return new(
                EstadoCapturaPantalla.Omitida,
                null,
                null,
                ["No se encontró asistente.cs para capturar."]);
        }

        string rutaCaptura = Path.Combine(rutaPractico, NombreCapturaTp6);
        if (File.Exists(rutaCaptura) && !forzar) {
            File.Delete(rutaCaptura);
        }

        string? transcripcion = null;
        try {
            transcripcion = CapturarSalidaPty(rutaPractico);
            string pantalla = TerminalBuffer.Renderizar(transcripcion, Columnas, Filas);
            RenderizarPngAsync(pantalla, rutaCaptura).GetAwaiter().GetResult();

            return new(
                EstadoCapturaPantalla.Capturada,
                Path.GetFileName(asistente),
                rutaCaptura,
                []);
        } catch (Exception ex) {
            return new(
                EstadoCapturaPantalla.Error,
                Path.GetFileName(asistente),
                null,
                ResumirError(ex, transcripcion));
        }
    }

    static string? SeleccionarAsistenteCs(string rutaPractico) {
        if (!Directory.Exists(rutaPractico)) {
            return null;
        }

        return Directory
            .EnumerateFiles(rutaPractico, "asistente.cs", SearchOption.AllDirectories)
            .Where(ObjetivosPracticos.EsArchivoFuente)
            .OrderBy(ruta => EstaEnRaizPractico(rutaPractico, ruta) ? 0 : 1)
            .ThenBy(ruta => ruta, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    static bool EstaEnRaizPractico(string rutaPractico, string rutaArchivo) {
        string? directorioArchivo = Path.GetDirectoryName(Path.GetFullPath(rutaArchivo));
        return string.Equals(
            directorioArchivo,
            Path.GetFullPath(rutaPractico).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            AppPaths.ComparacionRutas);
    }

    static string CapturarSalidaPty(string rutaPractico) {
        string rutaTranscripcion = Path.Combine(Path.GetTempPath(), $"tp6-captura-{Guid.NewGuid():N}.ansi");
        ProcessStartInfo startInfo = new() {
            FileName = "script",
            WorkingDirectory = rutaPractico,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.Environment["TERM"] = "xterm-256color";
        startInfo.Environment["COLUMNS"] = Columnas.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["LINES"] = Filas.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["OPENAI_API_URL"] = ObtenerVariable("OPENAI_API_URL", "http://127.0.0.1:9/v1");
        startInfo.Environment["OPENAI_API_KEY"] = ObtenerVariable("OPENAI_API_KEY", "no-requiere-key");
        startInfo.Environment["OPENAI_MODEL"] = ObtenerVariable("OPENAI_MODEL", "gpt-5.4-mini");
        startInfo.ArgumentList.Add("-q");
        startInfo.ArgumentList.Add(rutaTranscripcion);
        startInfo.ArgumentList.Add("dotnet");
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("asistente.cs");

        try {
            using Process proceso = Process.Start(startInfo)
                ?? throw new InvalidOperationException("No se pudo iniciar script.");

            Task<string> stdout = proceso.StandardOutput.ReadToEndAsync();
            Task<string> stderr = proceso.StandardError.ReadToEndAsync();

            if (!proceso.WaitForExit(TiempoMaximoCapturaMs)) {
                MatarProceso(proceso);
            }

            string salidaProceso = stdout.GetAwaiter().GetResult();
            string errorProceso = stderr.GetAwaiter().GetResult();
            string transcripcion = File.Exists(rutaTranscripcion)
                ? File.ReadAllText(rutaTranscripcion)
                : string.Empty;

            if (string.IsNullOrWhiteSpace(transcripcion) && !string.IsNullOrWhiteSpace(salidaProceso + errorProceso)) {
                transcripcion = salidaProceso + "\n" + errorProceso;
            }

            if (string.IsNullOrWhiteSpace(transcripcion)) {
                throw new InvalidOperationException("La pseudo-terminal no produjo salida para capturar.");
            }

            return transcripcion;
        } finally {
            try {
                if (File.Exists(rutaTranscripcion)) {
                    File.Delete(rutaTranscripcion);
                }
            } catch {
            }
        }
    }

    static string ObtenerVariable(string nombre, string valorPorDefecto) =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(nombre))
            ? valorPorDefecto
            : Environment.GetEnvironmentVariable(nombre)!;

    static void MatarProceso(Process proceso) {
        try {
            if (!proceso.HasExited) {
                proceso.Kill(entireProcessTree: true);
                proceso.WaitForExit();
            }
        } catch (InvalidOperationException) {
        }
    }

    static async Task RenderizarPngAsync(string pantalla, string rutaCaptura) {
        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        IPage page = await browser.NewPageAsync(new() {
            ViewportSize = new ViewportSize { Width = 1100, Height = 720 },
            DeviceScaleFactor = 1
        });

        await page.SetContentAsync(HtmlTerminal(pantalla), new() { WaitUntil = WaitUntilState.Load });
        await page.ScreenshotAsync(new() {
            Path = rutaCaptura,
            FullPage = false
        });
    }

    static string HtmlTerminal(string pantalla) {
        string contenido = WebUtility.HtmlEncode(pantalla);
        return $$"""
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<style>
html, body {
  margin: 0;
  width: 1100px;
  height: 720px;
  background: #151515;
}
body {
  display: grid;
  place-items: center;
}
.terminal {
  width: 1040px;
  height: 660px;
  box-sizing: border-box;
  overflow: hidden;
  background: #1f1f1f;
  color: #eeeeee;
  border: 1px solid #3a3a3a;
  border-radius: 6px;
  padding: 18px 20px;
  font: 20px/1.25 "SFMono-Regular", "Menlo", "Consolas", monospace;
  white-space: pre;
}
</style>
</head>
<body>
<pre class="terminal">{{contenido}}</pre>
</body>
</html>
""";
    }

    static IReadOnlyList<string> ResumirError(Exception ex, string? transcripcion) {
        List<string> mensajes = [ex.Message];
        if (!string.IsNullOrWhiteSpace(transcripcion)) {
            mensajes.AddRange(
                TerminalBuffer
                    .Renderizar(transcripcion, Columnas, Filas)
                    .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .TakeLast(3));
        }

        return mensajes;
    }

    sealed class TerminalBuffer {
        readonly char[,] celdas;
        readonly int columnas;
        readonly int filas;
        int x;
        int y;
        int guardadoX;
        int guardadoY;

        TerminalBuffer(int columnas, int filas) {
            this.columnas = columnas;
            this.filas = filas;
            celdas = new char[filas, columnas];
            LimpiarTodo();
        }

        public static string Renderizar(string entrada, int columnas, int filas) {
            TerminalBuffer buffer = new(columnas, filas);
            buffer.Procesar(entrada);
            return buffer.ComoTexto();
        }

        void Procesar(string entrada) {
            for (int i = 0; i < entrada.Length; i++) {
                char ch = entrada[i];
                if (ch == '\u001b') {
                    i = ProcesarEscape(entrada, i);
                    continue;
                }

                ProcesarCaracter(ch);
            }
        }

        int ProcesarEscape(string entrada, int indiceEscape) {
            int indice = indiceEscape + 1;
            if (indice >= entrada.Length) {
                return indiceEscape;
            }

            char tipo = entrada[indice];
            if (tipo == '[') {
                int inicio = indice + 1;
                int fin = inicio;
                while (fin < entrada.Length && !EsFinalCsi(entrada[fin])) {
                    fin++;
                }

                if (fin >= entrada.Length) {
                    return entrada.Length - 1;
                }

                ProcesarCsi(entrada.Substring(inicio, fin - inicio), entrada[fin]);
                return fin;
            }

            if (tipo == 'c') {
                LimpiarTodo();
            } else if (tipo == '7') {
                guardadoX = x;
                guardadoY = y;
            } else if (tipo == '8') {
                x = guardadoX;
                y = guardadoY;
            }

            return indice;
        }

        static bool EsFinalCsi(char ch) => ch is >= '@' and <= '~';

        void ProcesarCsi(string parametros, char final) {
            int[] valores = ParsearParametros(parametros);
            switch (final) {
                case 'A':
                    y = Math.Max(0, y - Valor(valores, 0, 1));
                    break;
                case 'B':
                    y = Math.Min(filas - 1, y + Valor(valores, 0, 1));
                    break;
                case 'C':
                    x = Math.Min(columnas - 1, x + Valor(valores, 0, 1));
                    break;
                case 'D':
                    x = Math.Max(0, x - Valor(valores, 0, 1));
                    break;
                case 'H':
                case 'f':
                    y = Math.Clamp(Valor(valores, 0, 1) - 1, 0, filas - 1);
                    x = Math.Clamp(Valor(valores, 1, 1) - 1, 0, columnas - 1);
                    break;
                case 'J':
                    if (Valor(valores, 0, 0) == 2) {
                        LimpiarTodo();
                    }
                    break;
                case 'K':
                    LimpiarLineaDesdeCursor();
                    break;
                case 's':
                    guardadoX = x;
                    guardadoY = y;
                    break;
                case 'u':
                    x = guardadoX;
                    y = guardadoY;
                    break;
            }
        }

        static int[] ParsearParametros(string parametros) {
            string limpio = parametros.TrimStart('?');
            if (string.IsNullOrWhiteSpace(limpio)) {
                return [];
            }

            return limpio
                .Split(';')
                .Select(parte => int.TryParse(parte, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor) ? valor : 0)
                .ToArray();
        }

        static int Valor(int[] valores, int indice, int valorPorDefecto) =>
            indice < valores.Length && valores[indice] != 0 ? valores[indice] : valorPorDefecto;

        void ProcesarCaracter(char ch) {
            switch (ch) {
                case '\r':
                    x = 0;
                    return;
                case '\n':
                    NuevaLinea();
                    return;
                case '\b':
                    x = Math.Max(0, x - 1);
                    return;
            }

            if (char.IsControl(ch)) {
                return;
            }

            celdas[y, x] = ch;
            x++;
            if (x >= columnas) {
                NuevaLinea();
            }
        }

        void NuevaLinea() {
            x = 0;
            y++;
            if (y < filas) {
                return;
            }

            for (int fila = 1; fila < filas; fila++) {
                for (int columna = 0; columna < columnas; columna++) {
                    celdas[fila - 1, columna] = celdas[fila, columna];
                }
            }

            for (int columna = 0; columna < columnas; columna++) {
                celdas[filas - 1, columna] = ' ';
            }

            y = filas - 1;
        }

        void LimpiarTodo() {
            for (int fila = 0; fila < filas; fila++) {
                for (int columna = 0; columna < columnas; columna++) {
                    celdas[fila, columna] = ' ';
                }
            }

            x = 0;
            y = 0;
        }

        void LimpiarLineaDesdeCursor() {
            for (int columna = x; columna < columnas; columna++) {
                celdas[y, columna] = ' ';
            }
        }

        string ComoTexto() {
            StringBuilder salida = new();
            for (int fila = 0; fila < filas; fila++) {
                for (int columna = 0; columna < columnas; columna++) {
                    salida.Append(celdas[fila, columna]);
                }

                if (fila + 1 < filas) {
                    salida.AppendLine();
                }
            }

            return salida.ToString();
        }
    }
}
