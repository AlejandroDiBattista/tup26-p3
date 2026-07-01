using Microsoft.Playwright;
using System.Net;

namespace Tup26.AlumnosApp;

static class CapturaTerminalPracticos {
    const int Columnas = 100;
    const int Filas = 30;
    const int TiempoMaximoArranqueMs = 8_000;
    const int TiempoMaximoRespuestaMs = 20_000;
    const int IntervaloSondeoMs = 500;
    const int PausaEntreTeclasMs = 120;
    const int MaximoIntentosFoco = 3;
    const string MensajePrueba = "generar quicksort";
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
            CapturaTerminalInteractivaResultado resultado = CapturarSalidaPty(rutaPractico);
            transcripcion = resultado.Transcripcion;
            string pantalla = TerminalBuffer.Renderizar(transcripcion, Columnas, Filas);
            RenderizarPngAsync(pantalla, rutaCaptura).GetAwaiter().GetResult();

            if (!resultado.RespuestaDetectada) {
                return new(
                    EstadoCapturaPantalla.Error,
                    Path.GetFileName(asistente),
                    rutaCaptura,
                    ["No se detectó respuesta luego de 20 segundos."]);
            }

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

    static CapturaTerminalInteractivaResultado CapturarSalidaPty(string rutaPractico) {
        string rutaTranscripcion = Path.Combine(Path.GetTempPath(), $"tp6-captura-{Guid.NewGuid():N}.ansi");
        ProcessStartInfo startInfo = new() {
            FileName = "script",
            WorkingDirectory = rutaPractico,
            RedirectStandardInput = true,
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
        startInfo.ArgumentList.Add("-F");
        startInfo.ArgumentList.Add(rutaTranscripcion);
        startInfo.ArgumentList.Add("dotnet");
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("asistente.cs");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("openai");

        try {
            using Process proceso = Process.Start(startInfo)
                ?? throw new InvalidOperationException("No se pudo iniciar script.");

            Task<string> stdout = proceso.StandardOutput.ReadToEndAsync();
            Task<string> stderr = proceso.StandardError.ReadToEndAsync();

            bool mensajeEnviado = EsperarArranqueYEnviarMensaje(proceso, rutaTranscripcion);
            bool respuestaDetectada = mensajeEnviado && EsperarRespuesta(proceso, rutaTranscripcion);
            MatarProceso(proceso);

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

            return new(transcripcion, respuestaDetectada);
        } finally {
            try {
                if (File.Exists(rutaTranscripcion)) {
                    File.Delete(rutaTranscripcion);
                }
            } catch {
            }
        }
    }

    static bool EsperarArranqueYEnviarMensaje(Process proceso, string rutaTranscripcion) {
        DateTime limite = DateTime.UtcNow.AddMilliseconds(TiempoMaximoArranqueMs);

        while (!proceso.HasExited && DateTime.UtcNow < limite) {
            string pantalla = PantallaActual(rutaTranscripcion);
            if (PantallaListaParaInteractuar(pantalla)) {
                EnviarMensaje(proceso, pantalla, prefijoTeclas: string.Empty, escribirTexto: true);
                return true;
            }

            Thread.Sleep(IntervaloSondeoMs);
        }

        return false;
    }

    static bool EsperarRespuesta(Process proceso, string rutaTranscripcion) {
        DateTime limite = DateTime.UtcNow.AddMilliseconds(TiempoMaximoRespuestaMs);
        DateTime proximoFallback = DateTime.UtcNow.AddSeconds(3);
        int intentosFallback = 0;
        while (!proceso.HasExited && DateTime.UtcNow < limite) {
            string pantalla = PantallaActual(rutaTranscripcion);
            if (RespuestaDetectada(pantalla)) {
                return true;
            }

            if (DateTime.UtcNow >= proximoFallback && intentosFallback < MaximoIntentosFoco) {
                EnviarMensaje(
                    proceso,
                    pantalla,
                    prefijoTeclas: PrefijoFocoFallback(intentosFallback),
                    escribirTexto: true);
                intentosFallback++;
                proximoFallback = DateTime.UtcNow.AddSeconds(4);
            }

            Thread.Sleep(IntervaloSondeoMs);
        }

        return RespuestaDetectada(PantallaActual(rutaTranscripcion));
    }

    static string PrefijoFocoFallback(int intento) =>
        intento switch {
            0 => "\u001b[Z",
            1 => "\t",
            _ => "\u001b[Z\u001b[Z"
        };

    static void EnviarMensaje(Process proceso, string pantalla, string prefijoTeclas, bool escribirTexto) {
        if (proceso.HasExited) {
            return;
        }

        try {
            EnviarClickEntrada(proceso, pantalla);
            EnviarTeclas(proceso, prefijoTeclas);

            if (escribirTexto) {
                foreach (char ch in MensajePrueba) {
                    proceso.StandardInput.Write(ch);
                    proceso.StandardInput.Flush();
                    Thread.Sleep(10);
                }

                Thread.Sleep(PausaEntreTeclasMs);
            }

            proceso.StandardInput.Write("\r");
            proceso.StandardInput.Flush();
        } catch (InvalidOperationException) {
        } catch (IOException) {
        }
    }

    static void EnviarClickEntrada(Process proceso, string pantalla) {
        (int x, int y) = CoordenadasEntrada(pantalla);
        proceso.StandardInput.Write($"\u001b[<0;{x};{y}M");
        proceso.StandardInput.Flush();
        Thread.Sleep(PausaEntreTeclasMs);
        proceso.StandardInput.Write($"\u001b[<0;{x};{y}m");
        proceso.StandardInput.Flush();
        Thread.Sleep(PausaEntreTeclasMs);
    }

    static (int X, int Y) CoordenadasEntrada(string pantalla) {
        string[] lineas = pantalla.Split('\n');
        for (int indice = lineas.Length - 1; indice >= 0; indice--) {
            if (indice < Filas / 2) {
                break;
            }

            string linea = lineas[indice];
            if (linea.Contains("Mensaje", StringComparison.OrdinalIgnoreCase) ||
                linea.Contains("Entrada", StringComparison.OrdinalIgnoreCase) ||
                linea.Contains("consulta", StringComparison.OrdinalIgnoreCase)) {
                return (4, Math.Clamp(indice + 2, 1, Filas));
            }
        }

        return (4, Math.Max(1, Filas - 5));
    }

    static void EnviarTeclas(Process proceso, string teclas) {
        for (int indice = 0; indice < teclas.Length; indice++) {
            if (teclas[indice] == '\u001b' && indice + 2 < teclas.Length && teclas[indice + 1] == '[') {
                proceso.StandardInput.Write(teclas.Substring(indice, 3));
                indice += 2;
            } else {
                proceso.StandardInput.Write(teclas[indice]);
            }

            proceso.StandardInput.Flush();
            Thread.Sleep(PausaEntreTeclasMs);
        }
    }

    static string PantallaActual(string rutaTranscripcion) {
        string transcripcion = LeerTranscripcion(rutaTranscripcion);
        return string.IsNullOrWhiteSpace(transcripcion)
            ? string.Empty
            : TerminalBuffer.Renderizar(transcripcion, Columnas, Filas);
    }

    static string LeerTranscripcion(string rutaTranscripcion) {
        try {
            return File.Exists(rutaTranscripcion)
                ? File.ReadAllText(rutaTranscripcion)
                : string.Empty;
        } catch (IOException) {
            return string.Empty;
        }
    }

    static bool PantallaListaParaInteractuar(string pantalla) =>
        !string.IsNullOrWhiteSpace(pantalla) &&
        (
            pantalla.Contains("Enviar", StringComparison.OrdinalIgnoreCase) ||
            pantalla.Contains("mensaje", StringComparison.OrdinalIgnoreCase) ||
            pantalla.Contains("consulta", StringComparison.OrdinalIgnoreCase) ||
            pantalla.Contains("Enter", StringComparison.OrdinalIgnoreCase)
        );

    static bool RespuestaDetectada(string pantalla) {
        int indiceMensaje = pantalla.IndexOf(MensajePrueba, StringComparison.OrdinalIgnoreCase);
        if (indiceMensaje < 0) {
            return false;
        }

        string despuesDelMensaje = pantalla[(indiceMensaje + MensajePrueba.Length)..];
        if (despuesDelMensaje.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
            despuesDelMensaje.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
            despuesDelMensaje.Contains("No se pudo", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        string normalizado = NormalizarContenidoRespuesta(despuesDelMensaje);
        return normalizado.Count(char.IsLetterOrDigit) >= 30;
    }

    static string NormalizarContenidoRespuesta(string texto) {
        string normalizado = texto;
        foreach (string etiqueta in new[] {
            "Asistente", "Assistant", "Vos", "Usuario", "User", "Enviar", "Mensaje", "Tu Mensaje"
        }) {
            normalizado = normalizado.Replace(etiqueta, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return new string(normalizado
            .Where(ch =>
                char.IsLetterOrDigit(ch) ||
                char.IsWhiteSpace(ch) ||
                ch is '_' or '-' or '+' or '*' or '/' or '=' or '<' or '>' or '(' or ')' or '{' or '}' or '[' or ']')
            .ToArray());
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
        await using IBrowser browser = await LanzarBrowserAsync(playwright);
        IPage page = await browser.NewPageAsync(new() {
            ViewportSize = new ViewportSize { Width = 1320, Height = 860 },
            DeviceScaleFactor = 1
        });

        await page.SetContentAsync(HtmlTerminal(pantalla), new() { WaitUntil = WaitUntilState.Load });
        await page.ScreenshotAsync(new() {
            Path = rutaCaptura,
            FullPage = false
        });
    }

    static async Task<IBrowser> LanzarBrowserAsync(IPlaywright playwright) {
        try {
            return await playwright.Chromium.LaunchAsync(new() {
                Channel = "chrome",
                Headless = true
            });
        } catch {
            return await playwright.Chromium.LaunchAsync(new() { Headless = true });
        }
    }

    static string HtmlTerminal(string pantalla) {
        string contenido = HtmlCeldas(pantalla);
        return $$"""
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<style>
html, body {
  margin: 0;
  width: 1320px;
  height: 860px;
  background: #151515;
}
body {
  display: grid;
  place-items: center;
}
.terminal {
  width: 1240px;
  height: 800px;
  box-sizing: border-box;
  overflow: hidden;
  background: #1f1f1f;
  color: #eeeeee;
  border: 1px solid #3a3a3a;
  border-radius: 6px;
  padding: 18px 20px;
  font: 20px/1.25 "Menlo", "Monaco", "Consolas", monospace;
  white-space: normal;
}
.line {
  height: 25px;
  white-space: nowrap;
}
.cell {
  display: inline-block;
  width: 12px;
  height: 25px;
  overflow: visible;
  text-align: left;
}
</style>
</head>
<body>
<div class="terminal">{{contenido}}</div>
</body>
</html>
""";
    }

    static string HtmlCeldas(string pantalla) {
        StringBuilder html = new();
        string[] lineas = pantalla.Split('\n');
        foreach (string linea in lineas) {
            html.Append("<div class=\"line\">");
            foreach (char ch in linea.TrimEnd('\r')) {
                string contenido = ch == ' ' ? "&nbsp;" : WebUtility.HtmlEncode(ch.ToString());
                html.Append("<span class=\"cell\">");
                html.Append(contenido);
                html.Append("</span>");
            }

            html.AppendLine("</div>");
        }

        return html.ToString();
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

    readonly record struct CapturaTerminalInteractivaResultado(string Transcripcion, bool RespuestaDetectada);

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

                if (char.IsHighSurrogate(ch) && i + 1 < entrada.Length && char.IsLowSurrogate(entrada[i + 1])) {
                    ProcesarCaracter(' ');
                    i++;
                    continue;
                }

                if (char.IsSurrogate(ch)) {
                    ProcesarCaracter(' ');
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

            if (tipo == ']') {
                return ConsumirSecuenciaTexto(entrada, indice + 1);
            }

            if (tipo is 'P' or '_' or '^') {
                return ConsumirSecuenciaTexto(entrada, indice + 1);
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

        static int ConsumirSecuenciaTexto(string entrada, int inicio) {
            for (int indice = inicio; indice < entrada.Length; indice++) {
                if (entrada[indice] == '\a') {
                    return indice;
                }

                if (entrada[indice] == '\u001b' && indice + 1 < entrada.Length && entrada[indice + 1] == '\\') {
                    return indice + 1;
                }
            }

            return entrada.Length - 1;
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
