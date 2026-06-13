using Spectre.Console;
using Spectre.Console.Cli;

namespace Tup26.AlumnosApp;

static class AlumnosCliApp {
    public static CommandApp Crear() {
        CommandApp app = new();
        app.Configure(config => {
            config.SetApplicationName("dotnet run --");

            config.AddCommand<ListarAlumnosCommand>("listar-alumnos")
                .WithDescription("Muestra todos los alumnos.");
            config.AddCommand<ContarAsistenciasCommand>("contar-asistencias")
                .WithDescription("Reconstruye las asistencias hasta hoy y marca los presentes del día desde WhatsApp.");
            config.AddCommand<RevisarPrsCommand>("revisar-prs")
                .WithDescription("Revisa pull requests de los alumnos.");
            config.AddCommand<BajarPrsCommand>("bajar-prs")
                .WithDescription("Descarga y sobrescribe todos los prácticos detectados en los PRs.");
            config.AddCommand<CerrarPrsCommand>("cerrar-prs")
                .WithDescription("Cierra todos los PRs abiertos.");
            config.AddCommand<PublicarPracticoCommand>("publicar-practico")
                .WithDescription("Publica el enunciado de un trabajo práctico en la carpeta de cada alumno.");
            config.AddCommand<PublicarApuntesCommand>("publicar-apuntes")
                .WithDescription("Ejecuta publicar.py dentro de la carpeta de apuntes.");
            config.AddCommand<ListarPracticosFaltantesCommand>("listar-practicos-faltantes")
                .WithDescription("Lista alumnos a quienes les falta el trabajo práctico indicado.");
            config.AddCommand<ExportarEstadoCommand>("exportar-estado")
                .WithDescription("Exporta el estado resumido a ESTADO.md.");
            config.AddCommand<ExportarMarkdownCommand>("exportar-markdown")
                .WithDescription("Exporta la lista a Markdown.");
            config.AddCommand<ExportarJsonCommand>("exportar-json")
                .WithDescription("Exporta la lista a JSON.");
            config.AddCommand<ExportarVCardCommand>("exportar-vcard")
                .WithDescription("Exporta la lista a vCard.");
            config.AddCommand<ListarGruposWhatsAppCommand>("listar-grupos-whatsapp")
                .WithDescription("Lista grupos y participantes de WhatsApp.");
            config.AddCommand<RevisarPresentacionesCommand>("revisar-presentaciones")
                .WithDescription("Marca TPs presentados a partir del código local.");
            config.AddCommand<LimpiarArchivosTemporalesCommand>("limpiar-archivos-temporales")
                .WithDescription("Elimina bin, obj, .vs y cachés de compilación dentro de prácticos.");
        });

        return app;
    }

    public static string[] NormalizarArgumentosAyuda(string[] args) {
        if (args.Length == 0 || !EsAliasAyuda(args[0])) { return args; }

        if (args.Length == 1) { return ["--help"]; }

        return [.. args[1..], "--help"];
    }

    public static int EjecutarModoInteractivo(CommandApp app) {
        while (true) {
            string[]? argumentosInteractivos = SolicitarComandoInteractivo();
            if (argumentosInteractivos is null) {
                AnsiConsole.MarkupLine("[grey]Saliendo del modo interactivo...[/]");
                return 0;
            }

            if (argumentosInteractivos.Length == 0) {
                AnsiConsole.Clear();
                continue;
            }

            string[] args = NormalizarArgumentosAyuda(argumentosInteractivos);
            if (args.Length == 0) { continue; }

            AnsiConsole.WriteLine();
            MostrarComandoEnEjecucion(args);
            int codigo = app.Run(args);
            AnsiConsole.WriteLine();

            if (codigo != 0) { AnsiConsole.MarkupLine($"[red]El comando terminó con código {codigo}.[/]"); }

            AnsiConsole.MarkupLine("[grey]Presioná una tecla para volver al menú...[/]");
            Console.ReadKey(intercept: true);
            AnsiConsole.Clear();
        }
    }

    static void MostrarComandoEnEjecucion(string[] args) {
        string descripcion = DescribirComando(args);
        AnsiConsole.MarkupLine($"[bold cyan]Ejecutando:[/] {Markup.Escape(descripcion)}");
        AnsiConsole.MarkupLine($"[grey]Comando:[/] {Markup.Escape(string.Join(" ", args))}");
        AnsiConsole.WriteLine();
    }

    static string DescribirComando(string[] args) {
        if (args.Length == 0) { return "acción interactiva"; }

        string detalle = args.Length > 1 ? $" ({string.Join(" ", args[1..])})" : string.Empty;
        return args[0] switch {
            "listar-alumnos" => "Listar alumnos",
            "contar-asistencias" => "Contar asistencias desde WhatsApp",
            "publicar-practico" => $"Publicar práctico{detalle}",
            "publicar-apuntes" => "Publicar apuntes",
            "revisar-prs" => "Revisar pull requests",
            "bajar-prs" => "Bajar PRs",
            "cerrar-prs" => "Cerrar todos los PRs",
            "listar-practicos-faltantes" => $"Listar prácticos faltantes{detalle}",
            "exportar-estado" => "Exportar Estado",
            "exportar-markdown" => "Exportar alumnos a Markdown",
            "exportar-json" => "Exportar alumnos a JSON",
            "exportar-vcard" => "Exportar alumnos a vCard",
            "revisar-presentaciones" => $"Revisar presentaciones{detalle}",
            "limpiar-archivos-temporales" => "Limpiar archivos temporales",
            "listar-grupos-whatsapp" => "Listar grupos y participantes de WhatsApp",
            _ => args[0]
        };
    }

    public static string[]? SolicitarComandoInteractivo() {
        Alumnos alumnos = AlumnosManager.Leer();

        AnsiConsole.Write(
            new Panel(new Markup($"[bold]Modo interactivo[/]\n[grey]Elegí una acción y completá sus parámetros.[/]\n[green]Alumnos detectados:[/] {alumnos.Count()}")) {
                Header = new PanelHeader("[yellow]TUP 2026 · Alumnos[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1, 1, 1)
            });

        InteractiveChoice opcion = PedirOpcion(
            "[bold cyan]Menú principal[/] · ¿Qué querés hacer?",
            ObtenerOpcionesPrincipales());

        return opcion.Command switch {
            "listar-alumnos" => ["listar-alumnos"],
            "contar-asistencias" => ["contar-asistencias"],
            "revisar-prs" => ["revisar-prs"],
            "bajar-prs" => ["bajar-prs"],
            "cerrar-prs" => ["cerrar-prs"],
            "publicar-practico" => ConstruirArgumentosPublicarPractico(),
            "publicar-apuntes" => ["publicar-apuntes"],
            "revisar-presentaciones" => ConstruirArgumentosRevisarPresentaciones(),
            "listar-practicos-faltantes" => ConstruirArgumentosPracticosFaltantes(),
            "exportar-estado" => ["exportar-estado"],
            "exportar-markdown" => ["exportar-markdown"],
            "exportar-json" => ["exportar-json"],
            "exportar-vcard" => ["exportar-vcard"],
            "listar-grupos-whatsapp" => ["listar-grupos-whatsapp"],
            "limpiar-archivos-temporales" => ["limpiar-archivos-temporales"],
            "salir" => null,
            _ => Array.Empty<string>()
        };
    }

    static InteractiveChoice PedirOpcion(string titulo, IReadOnlyList<InteractiveChoice> opciones) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<InteractiveChoice>()
                .Title(titulo)
                .PageSize(16)
                .UseConverter(choice => $"[green]{choice.Label,-30}[/] [grey] {choice.Description}[/]")
                .AddChoices(opciones));

    static IReadOnlyList<InteractiveChoice> ObtenerOpcionesPrincipales() => [
            new("listar-alumnos",                 "Listar alumnos",                 "Mostrar todos los alumnos"),
            new("contar-asistencias",             "Contar asistencias",             "Reconstruir asistencias y marcar presentes de hoy"),
            new("revisar-prs",                    "Revisar PRs",                    "Mostrar el estado de los pull requests"),
            new("bajar-prs",                      "Bajar PRs",                      "Descargar y sobrescribir todos los prácticos"),
            new("cerrar-prs",                     "Cerrar PRs",                     "Cerrar pull requests abiertos"),
            new("revisar-presentaciones",         "Revisar presentaciones",         "Marcar TP presentados desde el código local"),
            new("publicar-practico",              "Publicar práctico",              "Copiar el enunciado de un TP a cada alumno"),
            new("publicar-apuntes",               "Publicar apuntes",               "Ejecutar apuntes/publicar.py"),
            new("listar-practicos-faltantes",     "Listar prácticos faltantes",      "Listar alumnos que adeudan un práctico"),
            new("exportar-estado",                "Exportar Estado",                "Exportar el resumen a ESTADO.md"),
            new("exportar-markdown",              "Exportar como Markdown",         "Exportar alumnos a alumnos.md"),
            new("exportar-json",                  "Exportar como JSON",             "Exportar alumnos a alumnos.json"),
            new("exportar-vcard",                 "Exportar como vCard",            "Exportar contactos a alumnos.vcf"),
            new("listar-grupos-whatsapp",         "Listar grupos de WhatsApp",      "Listar grupos y participantes"),
            new("limpiar-archivos-temporales",    "Limpiar archivos temporales",    "Eliminar bin, obj, .vs y cachés"),
            new("salir",                          "Salir",                          "Cerrar la aplicación")
        ];

    static string[] ConstruirArgumentosPracticosFaltantes() {
        string? trabajoPractico = PedirTrabajoPractico("Listar prácticos faltantes");

        return trabajoPractico is null
            ? Array.Empty<string>()
            : ["listar-practicos-faltantes", trabajoPractico];
    }

    static string[] ConstruirArgumentosRevisarPresentaciones() {
        string? trabajoPractico = PedirTrabajoPractico("Revisar presentaciones", permitirTodos: true, soloConfigurados: true);

        return trabajoPractico is null
            ? Array.Empty<string>()
            : string.IsNullOrWhiteSpace(trabajoPractico)
                ? ["revisar-presentaciones"]
                : ["revisar-presentaciones", trabajoPractico];
    }

    static string[] ConstruirArgumentosPublicarPractico() {
        string? trabajoPractico = PedirTrabajoPractico("Publicar práctico");
        if (trabajoPractico is null) { return Array.Empty<string>(); }

        string sobrescritura = PedirModoSobrescritura("Publicar práctico");

        return sobrescritura switch {
            "conservar" => ["publicar-practico", trabajoPractico],
            "sobrescribir" => ["publicar-practico", trabajoPractico, "--forzar"],
            _ => Array.Empty<string>()
        };
    }

    static string PedirModoSobrescritura(string accion) {
        InteractiveChoice opcion = PedirOpcion(
            $"[bold cyan]{accion}[/] · Elegí cómo manejar archivos existentes", [
                new("conservar",     "Conservar",     "No reemplazar archivos ya existentes"),
                new("sobrescribir",  "Sobrescribir",  "Reemplazar archivos ya existentes"),
                new("volver",      "Volver",      "Volver al menú sin ejecutar")
            ]);

        return opcion.Command;
    }

    static string? PedirTrabajoPractico(string accion, bool permitirTodos = false, bool soloConfigurados = false) {
        IReadOnlyList<EnunciadoPracticoDisponible> practicos = soloConfigurados
            ? PracticosConfig.FiltrarConfigurados(AppPaths.ListarEnunciadosPracticos())
            : AppPaths.ListarEnunciadosPracticos();
        if (practicos.Count == 0) {
            string textoVacio = permitirTodos ? "todos" : "cancelar";
            string valor = AnsiConsole.Prompt(
                new TextPrompt<string>($"[bold cyan]{accion}[/] · Trabajo práctico ([green]TP1[/] o [green]1[/], [grey]vacío = {textoVacio}[/]):")
                    .PromptStyle("cyan")
                    .AllowEmpty()
                    .Validate(valor =>
                        string.IsNullOrWhiteSpace(valor) || AlumnosCliActions.EsTrabajoPracticoValido(valor)
                            ? ValidationResult.Success()
                            : ValidationResult.Error(AlumnosCliActions.MensajeTrabajoPracticoInvalido(valor))));

            if (string.IsNullOrWhiteSpace(valor)) {
                return permitirTodos ? string.Empty : null;
            }

            return valor.Trim();
        }

        List<InteractiveChoice> opciones = [
            .. permitirTodos ? [new InteractiveChoice(string.Empty, "Todos", $"Revisar los {practicos.Count} trabajos prácticos")] : Array.Empty<InteractiveChoice>(),
            .. practicos.Select(practico => new InteractiveChoice(practico.Carpeta, $"TP{practico.Numero}", practico.Carpeta)),
            new("volver", "Volver", "Volver al menú sin ejecutar")
        ];

        InteractiveChoice seleccionado = AnsiConsole.Prompt(
            new SelectionPrompt<InteractiveChoice>()
                .Title($"[bold cyan]{accion}[/] · Elegí el trabajo práctico\n[grey]Se encontraron {practicos.Count} en {AppPaths.EnunciadosDirectory}[/]")
                .PageSize(12)
                .UseConverter(choice => $"[green]{choice.Label,-22}[/] [grey] {choice.Description}[/]")
                .AddChoices(opciones));

        return seleccionado.Command == "volver" ? null : seleccionado.Command;
    }

    static bool EsAliasAyuda(string valor) =>
        string.Equals(valor, "ayuda", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "help", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "-h", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "--help", StringComparison.OrdinalIgnoreCase);

    sealed record InteractiveChoice(string Command, string Label, string Description);
}
