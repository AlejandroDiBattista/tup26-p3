using System.Xml.Linq;
using Microsoft.Playwright;

namespace Tup26.AlumnosApp;

enum EstadoCapturaPantalla {
    Capturada,
    Error,
    Omitida
}

readonly record struct CapturaPantallaResultado(
    EstadoCapturaPantalla Estado,
    string? Proyecto,
    string? Archivo,
    IReadOnlyList<string> Mensajes) {

    public bool Capturada => Estado == EstadoCapturaPantalla.Capturada;
    public bool Error => Estado == EstadoCapturaPantalla.Error;
    public bool Omitida => Estado == EstadoCapturaPantalla.Omitida;
}

static class CapturaPantallasPracticos {
    const int TiempoMaximoArranqueMs = 60_000;
    const int TiempoMaximoNavegacionMs = 10_000;
    const string NombreCapturaTp5 = "captura-tp5.png";

    static readonly Regex regexUrlEscucha = new(@"Now listening on:\s+(https?://\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly string[] patronesErrorArranque = [
        "Unhandled exception",
        "Excepción no controlada",
        "exception has been thrown",
        ": error ",
        "fail:",
        "crit:",
        "fatal"
    ];

    static readonly string[] sufijosTemporalesSqlite = ["-wal", "-shm", "-journal"];

    public static async Task<CapturaPantallaResultado> Capturar(
        string rutaPractico,
        int numeroTp,
        string rutaInicial,
        bool headed,
        bool forzar) {

        string? proyecto = SeleccionarProyectoWeb(rutaPractico, numeroTp);
        if (proyecto is null) {
            return new(
                EstadoCapturaPantalla.Omitida,
                null,
                null,
                ["No se encontró un proyecto web para capturar."]);
        }

        string rutaCaptura = Path.Combine(rutaPractico, NombreCapturaTp5);
        if (File.Exists(rutaCaptura) && !forzar) {
            // El comando regenera por defecto la captura estable del TP.
            File.Delete(rutaCaptura);
        }

        Process? proceso = null;
        try {
            proceso = IniciarAplicacion(rutaPractico, proyecto);
            (bool inicio, string? urlBase, string salida) = EsperarUrlAplicacion(proceso, rutaPractico);
            if (!inicio || string.IsNullOrWhiteSpace(urlBase)) {
                return new(
                    EstadoCapturaPantalla.Error,
                    Path.GetFileName(proyecto),
                    null,
                    ResumirSalida(salida, "La aplicación no informó una URL de escucha."));
            }

            await CapturarBrowserAsync(urlBase, rutaInicial, rutaCaptura, headed);
            return new(
                EstadoCapturaPantalla.Capturada,
                Path.GetFileName(proyecto),
                rutaCaptura,
                []);
        } catch (Exception ex) {
            return new(
                EstadoCapturaPantalla.Error,
                Path.GetFileName(proyecto),
                null,
                [ex.Message]);
        } finally {
            if (proceso is not null) {
                MatarProceso(proceso);
                proceso.Dispose();
            }

            LimpiarTemporalesSqlite(rutaPractico);
        }
    }

    static string? SeleccionarProyectoWeb(string rutaPractico, int numeroTp) {
        List<string> proyectosWeb = ObjetivosPracticos.Obtener(rutaPractico, numeroTp)
            .Where(ruta => string.Equals(Path.GetExtension(ruta), ".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(EsProyectoWeb)
            .OrderBy(ruta => EstaEnRaizPractico(rutaPractico, ruta) ? 0 : 1)
            .ThenBy(ruta => ruta, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return proyectosWeb.FirstOrDefault();
    }

    static bool EsProyectoWeb(string rutaProyecto) {
        try {
            XDocument documento = XDocument.Load(rutaProyecto);
            string? sdk = documento.Root?.Attribute("Sdk")?.Value;
            return sdk?.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) == true;
        } catch {
            return File.ReadLines(rutaProyecto)
                .Take(5)
                .Any(linea => linea.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase));
        }
    }

    static bool EstaEnRaizPractico(string rutaPractico, string rutaProyecto) {
        string? directorioProyecto = Path.GetDirectoryName(Path.GetFullPath(rutaProyecto));
        return string.Equals(
            directorioProyecto,
            Path.GetFullPath(rutaPractico).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    static Process IniciarAplicacion(string rutaPractico, string proyecto) {
        string proyectoRelativo = Path.GetRelativePath(rutaPractico, proyecto);
        ProcessStartInfo startInfo = new() {
            FileName = "dotnet",
            WorkingDirectory = rutaPractico,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(proyectoRelativo);
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("-p:EnableSourceControlManagerQueries=false");

        Process proceso = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar dotnet run.");
        try {
            proceso.StandardInput.Close();
        } catch (InvalidOperationException) {
        }

        return proceso;
    }

    static (bool Inicio, string? UrlBase, string Salida) EsperarUrlAplicacion(Process proceso, string rutaPractico) {
        StringBuilder salida = new();
        object salidaLock = new();
        proceso.OutputDataReceived += (_, e) => AgregarLinea(salida, salidaLock, e.Data);
        proceso.ErrorDataReceived += (_, e) => AgregarLinea(salida, salidaLock, e.Data);
        proceso.BeginOutputReadLine();
        proceso.BeginErrorReadLine();

        DateTime limite = DateTime.UtcNow.AddMilliseconds(TiempoMaximoArranqueMs);
        while (!proceso.HasExited && DateTime.UtcNow < limite) {
            string salidaActual = LimpiarSalida(rutaPractico, ObtenerSalida(salida, salidaLock));
            Match match = regexUrlEscucha.Match(salidaActual);
            if (match.Success) {
                return (true, match.Groups[1].Value.TrimEnd('/'), salidaActual);
            }

            if (ContieneErrorArranque(salidaActual)) {
                return (false, null, salidaActual);
            }

            Thread.Sleep(100);
        }

        return (false, null, LimpiarSalida(rutaPractico, ObtenerSalida(salida, salidaLock)));
    }

    static void AgregarLinea(StringBuilder salida, object salidaLock, string? linea) {
        if (linea is null) {
            return;
        }

        lock (salidaLock) {
            salida.AppendLine(linea);
        }
    }

    static string ObtenerSalida(StringBuilder salida, object salidaLock) {
        lock (salidaLock) {
            return salida.ToString();
        }
    }

    static async Task CapturarBrowserAsync(string urlBase, string rutaInicial, string rutaCaptura, bool headed) {
        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await LanzarBrowserAsync(playwright, headed);
        IBrowserContext context = await browser.NewContextAsync(new() {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        IPage page = await context.NewPageAsync();

        string rutaNormalizada = NormalizarRutaInicial(rutaInicial);
        await page.WaitForTimeoutAsync(500);

        (bool cargado, string? errorNavegacion) = await IntentarNavegar(page, CombinarUrl(urlBase, rutaNormalizada), aceptarErrorHttp: false);
        if (!cargado && rutaNormalizada != "/") {
            (cargado, errorNavegacion) = await IntentarNavegar(page, CombinarUrl(urlBase, "/"), aceptarErrorHttp: true);
        }

        if (!cargado) {
            throw new InvalidOperationException($"No se pudo cargar /contactos ni / para capturar la pantalla. {errorNavegacion}");
        }

        await page.WaitForTimeoutAsync(1500);
        await page.ScreenshotAsync(new() {
            Path = rutaCaptura,
            FullPage = false
        });
    }

    static async Task<IBrowser> LanzarBrowserAsync(IPlaywright playwright, bool headed) {
        try {
            return await playwright.Chromium.LaunchAsync(new() {
                Channel = "chrome",
                Headless = !headed
            });
        } catch {
            return await playwright.Chromium.LaunchAsync(new() {
                Headless = !headed
            });
        }
    }

    static async Task<(bool Cargado, string? Error)> IntentarNavegar(IPage page, string url, bool aceptarErrorHttp) {
        DateTime limite = DateTime.UtcNow.AddMilliseconds(TiempoMaximoNavegacionMs);
        string? ultimoError = null;
        while (DateTime.UtcNow < limite) {
            try {
                IResponse? response = await page.GotoAsync(url, new() {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 3_000
                });

                bool cargado = response is null || response.Status < 400 || aceptarErrorHttp;
                string? error = response is null || response.Status < 400
                    ? null
                    : $"HTTP {response.Status} en {url}.";
                return (cargado, error);
            } catch (Exception ex) {
                ultimoError = ex.Message;
                await page.WaitForTimeoutAsync(750);
            }
        }

        return (false, ultimoError ?? $"No se pudo navegar a {url}.");
    }

    static string NormalizarRutaInicial(string ruta) {
        if (string.IsNullOrWhiteSpace(ruta)) {
            return "/contactos";
        }

        string valor = ruta.Trim();
        return valor.StartsWith("/", StringComparison.Ordinal) ? valor : "/" + valor;
    }

    static string CombinarUrl(string urlBase, string ruta) =>
        $"{urlBase.TrimEnd('/')}/{ruta.TrimStart('/')}";

    static bool ContieneErrorArranque(string salida) =>
        patronesErrorArranque.Any(patron => salida.Contains(patron, StringComparison.OrdinalIgnoreCase));

    static string LimpiarSalida(string rutaPractico, string salida) {
        string prefijoPractico = Path.GetFullPath(rutaPractico).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (prefijoPractico.StartsWith("/var/", StringComparison.Ordinal)) {
            salida = salida.Replace(
                "/private" + prefijoPractico,
                string.Empty,
                StringComparison.Ordinal);
        }

        return salida.Replace(prefijoPractico, string.Empty, StringComparison.Ordinal);
    }

    static IReadOnlyList<string> ResumirSalida(string salida, string mensajePorDefecto) {
        string[] lineas = salida
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> errores = lineas
            .Where(linea =>
                linea.Contains(": error ", StringComparison.OrdinalIgnoreCase) ||
                linea.StartsWith("fail:", StringComparison.OrdinalIgnoreCase) ||
                linea.StartsWith("crit:", StringComparison.OrdinalIgnoreCase) ||
                linea.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        if (errores.Count > 0) {
            return errores;
        }

        return lineas
            .Where(linea => !string.IsNullOrWhiteSpace(linea))
            .TakeLast(3)
            .DefaultIfEmpty(mensajePorDefecto)
            .ToList();
    }

    static void MatarProceso(Process proceso) {
        try {
            if (!proceso.HasExited) {
                proceso.Kill(entireProcessTree: true);
                proceso.WaitForExit();
            }
        } catch (InvalidOperationException) {
        }
    }

    static void LimpiarTemporalesSqlite(string rutaPractico) {
        if (!Directory.Exists(rutaPractico)) {
            return;
        }

        foreach (string archivo in Directory.EnumerateFiles(rutaPractico, "*", SearchOption.AllDirectories)) {
            if (!sufijosTemporalesSqlite.Any(sufijo => archivo.EndsWith(sufijo, StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            try {
                File.Delete(archivo);
            } catch {
            }
        }
    }
}
