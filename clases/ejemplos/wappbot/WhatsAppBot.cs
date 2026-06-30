#!/usr/bin/env dotnet run
#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property Nullable=enable
#:package Microsoft.Extensions.AI@10.3.0
#:package Microsoft.Extensions.AI.OpenAI@10.3.0
#:package OpenAI@2.8.0

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using OpenAI;

const string ContactoPorDefecto = "+5493815343458";
const string Modelo            = "gpt-5.5";
const string ArchivoAgentes    = "AGENTS.md";
const string ComandoFinalizar  = "/finalizar";
const string PromptPorDefecto  = "Sos un asistente claro, breve y util. Responde en el mismo idioma del usuario.";
const int IntervaloSondeoMs    = 2_000;

string contacto = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0].Trim()
    : ContactoPorDefecto;

string? claveApi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(claveApi)) {
    Console.Error.WriteLine("Falta OPENAI_API_KEY. Defini la variable de entorno antes de iniciar el bot.");
    return 1;
}

Process? procesoSincronizacion = null;
try {
    procesoSincronizacion = ClienteWhatsApp.IniciarSincronizacion();

    string promptSistema = await LeerPromptSistema(ArchivoAgentes, PromptPorDefecto);
    AgenteSecretaria asistente = new(claveApi, Modelo, promptSistema);
    DateTimeOffset iniciadoEn = DateTimeOffset.Now;

    Console.WriteLine($"Bot activo para {contacto}. Envia {ComandoFinalizar} por WhatsApp para detenerlo.");

    while (true) {
        await Task.Delay(IntervaloSondeoMs);

        string? textoEntrante = await ClienteWhatsApp.RecibirTexto(contacto, iniciadoEn);
        if (string.IsNullOrWhiteSpace(textoEntrante)) { continue; }
        if (EsComandoFinalizar(textoEntrante)) { break; }

        string respuesta = await asistente.ProcesarMensaje(textoEntrante);
        if (string.IsNullOrWhiteSpace(respuesta)) { continue; }

        await ClienteWhatsApp.EnviarTexto(contacto, respuesta);
    }
} finally {
    ClienteWhatsApp.DetenerSincronizacion(procesoSincronizacion);
}

return 0;

static bool EsComandoFinalizar(string texto) => texto.Contains(ComandoFinalizar, StringComparison.OrdinalIgnoreCase);

static async Task<string> LeerPromptSistema(string archivoAgentes, string promptPorDefecto) {
    if (!File.Exists(archivoAgentes)) { return promptPorDefecto; }
    string prompt = await File.ReadAllTextAsync(archivoAgentes);
    return string.IsNullOrWhiteSpace(prompt) ? promptPorDefecto : prompt;
}

sealed class AgenteSecretaria {
    readonly IChatClient cliente;
    readonly ChatOptions opciones;
    readonly List<ChatMessage> conversacion;

    public AgenteSecretaria(string claveApi, string modelo, string promptSistema) {
        cliente = CrearCliente(claveApi, modelo);
        opciones = CrearOpciones();
        conversacion = [new(ChatRole.System, promptSistema)];
    }

    public async Task<string> ProcesarMensaje(string textoWhatsapp) {
        Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] usuario: {textoWhatsapp}");

        conversacion.Add(new(ChatRole.User, textoWhatsapp));

        ChatResponse respuesta = await cliente.GetResponseAsync(conversacion, opciones);
        conversacion.AddMessages(respuesta);

        return respuesta.Text.Trim();
    }

    static IChatClient CrearCliente(string claveApi, string modelo) => new ChatClientBuilder(
            new OpenAIClient(claveApi).GetChatClient(modelo).AsIChatClient())
        .UseFunctionInvocation()
        .Build();

    static ChatOptions CrearOpciones() => new() {
        Tools =
        [
            AIFunctionFactory.Create(ArchivosAgente.LeerArchivo),
            AIFunctionFactory.Create(ArchivosAgente.EscribirArchivo),
            AIFunctionFactory.Create(ArchivosAgente.ListarArchivos)
        ]
    };
}

static class ArchivosAgente {
    const string RaizTrabajo = ".";

    [Description("Lee un archivo de texto dentro de la carpeta de trabajo.")]
    public static async Task<string> LeerArchivo([Description("Ruta relativa del archivo a leer.")] string rutaRelativa) {
        string ruta = ObtenerRutaSegura(rutaRelativa);
        if (!File.Exists(ruta)) { return $"No encontre el archivo: {rutaRelativa}"; }
        return await File.ReadAllTextAsync(ruta);
    }

    [Description("Escribe o reemplaza un archivo de texto dentro de la carpeta de trabajo.")]
    public static async Task<string> EscribirArchivo(
        [Description("Ruta relativa del archivo a escribir.")] string rutaRelativa,
        [Description("Contenido completo del archivo.")] string contenido) {
        string ruta = ObtenerRutaSegura(rutaRelativa);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta) ?? Environment.CurrentDirectory);
        await File.WriteAllTextAsync(ruta, contenido, Encoding.UTF8);
        return $"Archivo actualizado: {rutaRelativa}";
    }

    [Description("Lista archivos de la carpeta de trabajo que coinciden con un patron simple, por ejemplo *.txt o notas/*.md.")]
    public static string ListarArchivos([Description("Patron relativo para buscar archivos.")] string patron = "*") {
        string raiz = Path.GetFullPath(RaizTrabajo);
        return string.Join('\n', Directory.EnumerateFiles(raiz, patron, SearchOption.AllDirectories)
            .Select(ruta => Path.GetRelativePath(raiz, ruta))
            .Take(200));
    }

    static string ObtenerRutaSegura(string rutaRelativa) {
        string raiz = Path.GetFullPath(RaizTrabajo);
        string rutaCompleta = Path.GetFullPath(Path.Combine(raiz, rutaRelativa));

        if (!rutaCompleta.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal) && rutaCompleta != raiz) {
            throw new InvalidOperationException("La ruta debe estar dentro de la carpeta de trabajo.");
        }

        return rutaCompleta;
    }
}

static class ClienteWhatsApp {
    const string EjecutableWacli = "wacli";

    public static Process IniciarSincronizacion() {
        ProcessStartInfo infoInicio = CrearInfoInicioWacli(redirigir: false, "sync", "--follow", "--refresh-contacts");
        Process proceso = Process.Start(infoInicio) ?? throw new InvalidOperationException("No se pudo iniciar la sincronizacion de WhatsApp con wacli.");
        Console.WriteLine($"Sincronizacion de WhatsApp iniciada con wacli. PID: {proceso.Id}");
        return proceso;
    }

    public static void DetenerSincronizacion(Process? procesoSincronizacion) {
        if (procesoSincronizacion is null) { return; }
        if (!procesoSincronizacion.HasExited) { procesoSincronizacion.Kill(entireProcessTree: true); }
        procesoSincronizacion.Dispose();
    }

    public static async Task<string?> RecibirTexto(string contacto, DateTimeOffset? desde = null) {
        CommandResult resultado = await EjecutarWacli("--json", "messages", "list", "--chat", contacto, "--limit", "50");
        if (resultado.ExitCode != 0) {
            Console.Error.WriteLine($"No se pudieron leer mensajes con wacli: {resultado.StdErr}");
            return null;
        }

        IReadOnlyList<InboundMessage> mensajes = ParsearMensajes(resultado.StdOut, contacto);
        List<string> mensajesFiltrados = FiltrarMensajesNuevos(mensajes, desde)
            .Select(mensaje => mensaje.Text.Trim())
            .Where(texto => texto.Length > 0)
            .ToList();
        if (mensajesFiltrados.Count == 0) { return null; }

        return string.Join(Environment.NewLine, mensajesFiltrados);
    }

    public static async Task EnviarTexto(string contacto, string mensaje) {
        CommandResult resultado = await EjecutarWacli("send", "text", "--to", contacto, "--message", mensaje);
        if (resultado.ExitCode != 0) { throw new InvalidOperationException($"No se pudo enviar el mensaje con wacli: {resultado.StdErr}"); }
    }

    static IReadOnlyList<InboundMessage> ParsearMensajes(string salida, string contactoPorDefecto) {
        try {
            WacliMessageList? respuesta = JsonSerializer.Deserialize(salida, WhatsAppJsonContext.Default.WacliMessageList);
            List<WacliStoredMessage> mensajes = respuesta?.Data?.Messages ?? [];

            return mensajes.Select(AMensajeEntrante).ToList();
        } catch (JsonException) {
            return salida
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select((linea, indice) => new InboundMessage($"line:{indice}:{linea.GetHashCode()}", contactoPorDefecto, linea, false, DateTimeOffset.UtcNow))
                .ToList();
        }
    }

    static List<InboundMessage> FiltrarMensajesNuevos(IReadOnlyList<InboundMessage> mensajes, DateTimeOffset? desde) {
        List<InboundMessage> mensajesCronologicos = mensajes
            .OrderBy(mensaje => mensaje.Timestamp ?? DateTimeOffset.MinValue)
            .ThenBy(mensaje  => mensaje.Id, StringComparer.Ordinal)
            .ToList();

        int indiceUltimoMensajePropio = mensajesCronologicos.FindLastIndex(mensaje => mensaje.FromMe);
        int indiceUltimoMensajeAntesDeInicio = desde is null
            ? -1
            : mensajesCronologicos.FindLastIndex(mensaje => mensaje.Timestamp is not null && mensaje.Timestamp < desde);
        int indiceInicio = Math.Max(indiceUltimoMensajePropio, indiceUltimoMensajeAntesDeInicio);

        IEnumerable<InboundMessage> mensajesDespuesDeInicio = indiceInicio >= 0
            ? mensajesCronologicos.Skip(indiceInicio + 1)
            : mensajesCronologicos;

        return mensajesDespuesDeInicio
            .Where(EsTextoEntrante)
            .ToList();
    }

    static bool EsTextoEntrante(InboundMessage mensaje) => !mensaje.FromMe && !string.IsNullOrWhiteSpace(mensaje.Text);

    static InboundMessage AMensajeEntrante(WacliStoredMessage mensaje) => new(
        Id: mensaje.MsgID ?? Guid.NewGuid().ToString("N"),
        Contact: mensaje.ChatJID ?? mensaje.SenderJID ?? mensaje.ChatName ?? mensaje.SenderName ?? string.Empty,
        Text: PrimerNoVacio(mensaje.Text, mensaje.DisplayText, mensaje.Snippet) ?? string.Empty,
        FromMe: mensaje.FromMe,
        Timestamp: mensaje.Timestamp);

    static async Task<CommandResult> EjecutarWacli(params string[] argumentos) {
        ProcessStartInfo infoInicio = CrearInfoInicioWacli(redirigir: true, argumentos);
        using Process proceso = Process.Start(infoInicio) ?? throw new InvalidOperationException("No se pudo ejecutar wacli.");

        Task<string> salidaEstandar = proceso.StandardOutput.ReadToEndAsync();
        Task<string> errorEstandar  = proceso.StandardError.ReadToEndAsync();
        await proceso.WaitForExitAsync();

        return new(proceso.ExitCode, await salidaEstandar, await errorEstandar);
    }

    static ProcessStartInfo CrearInfoInicioWacli(bool redirigir, params string[] argumentos) {
        ProcessStartInfo infoInicio = new(EjecutableWacli) {
            UseShellExecute  = false,
            RedirectStandardOutput = redirigir,
            RedirectStandardError  = redirigir,
            CreateNoWindow   = true,
            WorkingDirectory = Environment.CurrentDirectory
        };
        foreach (string argumento in argumentos) { infoInicio.ArgumentList.Add(argumento); }
        return infoInicio;
    }

    static string? PrimerNoVacio(params string?[] valores) => valores.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor));
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(WacliMessageList))]
partial class WhatsAppJsonContext : JsonSerializerContext;

sealed class WacliMessageList {
    public WacliMessageData? Data { get; set; }
}

sealed class WacliMessageData {
    public List<WacliStoredMessage> Messages { get; set; } = [];
}

sealed class WacliStoredMessage {
    public string? MsgID { get; set; }
    public string? ChatJID { get; set; }
    public string? ChatName { get; set; }
    public string? SenderJID { get; set; }
    public string? SenderName { get; set; }
    public string? Text { get; set; }
    public string? DisplayText { get; set; }
    public string? Snippet { get; set; }
    public bool FromMe { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

sealed record InboundMessage(string Id, string Contact, string Text, bool FromMe, DateTimeOffset? Timestamp);
sealed record CommandResult(int ExitCode, string StdOut, string StdErr);
