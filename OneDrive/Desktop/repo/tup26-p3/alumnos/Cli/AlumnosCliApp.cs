using Spectre.Console;
using Spectre.Console.Cli;

namespace Tup26.AlumnosApp;

static class AlumnosCliApp {
    public static CommandApp Crear() {
        CommandApp app = new();
        app.Configure(config => {
            config.SetApplicationName("dotnet run --");

            config.AddCommand<ListarCommand>("listar")
                .WithDescription("Muestra todos los alumnos.");
            config.AddCommand<SinGithubCommand>("sin-github")
                .WithDescription("Lista alumnos sin cuenta de GitHub.");
            config.AddCommand<SinTelefonoCommand>("sin-telefono")
                .WithDescription("Lista alumnos sin teléfono.");
            config.AddCommand<SinFotoCommand>("sin-foto")
                .WithDescription("Lista alumnos sin foto.");
            config.AddCommand<Tp1NoPresentadoCommand>("tp1-no-presentado")
                .WithDescription("Lista alumnos que no presentaron el trabajo práctico 1.");
            config.AddCommand<Tp2NoPresentadoCommand>("tp2-no-presentado")
                .WithDescription("Lista alumnos que no presentaron el trabajo práctico 2.");
            config.AddCommand<TpNoPresentadoCommand>("tp-no-presentado")
                .WithDescription("Lista alumnos que no presentaron un trabajo práctico, ignorando quienes no presentaron ninguno.");
            config.AddCommand<SinPracticosCommand>("sin-practicos")
                .WithDescription("Lista alumnos que no presentaron ningún trabajo práctico.");
            config.AddCommand<LimpiarProyectosPracticosCommand>("limpiar-proyectos-practicos")
                .WithDescription("Elimina bin, obj, .vs y cachés de compilación dentro de prácticos.");
            config.AddCommand<GuardarCommand>("guardar")
                .WithDescription("Exporta la lista a Markdown.");
            config.AddCommand<JsonCommand>("json")
                .WithDescription("Exporta la lista a JSON.");
            config.AddCommand<VcfCommand>("vcf")
                .WithDescription("Exporta la lista a vCard.");
            config.AddCommand<InformerEstadoCommand>("informar-estado")
                .WithDescription("Publica el estado resumido en el README.md del repositorio.");
            config.AddCommand<CrearCarpetasCommand>("crear-carpetas")
                .WithDescription("Crea o normaliza las carpetas de prácticos.");
            config.AddCommand<PublicarCommand>("publicar")
                .WithDescription("Publica el enunciado de un trabajo práctico en la carpeta de cada alumno.");
            config.AddCommand<PublicarRehacerCommand>("publicar-rehacer")
                .WithDescription("Borra y republica el enunciado de un práctico solo en alumnos con estado Revisar.");
            config.AddCommand<PrsCommand>("prs")
                .WithDescription("Revisa pull requests de los alumnos.");
            config.AddCommand<NormalizarPrsCommand>("normalizar-prs")
                .WithDescription("Normaliza títulos de pull requests.");
            config.AddCommand<BajarPrsCommand>("bajar-prs")
                .WithDescription("Descarga archivos del práctico desde PRs.");
            config.AddCommand<CerrarPrsCommand>("cerrar-prs")
                .WithDescription("Cierra PRs abiertos; opcionalmente filtra por TP.");
            config.AddCommand<RevisarPresentadosCommand>("revisar-presentados")
                .WithDescription("Marca TPs presentados a partir del código local.");
            config.AddCommand<RegistrarAsistenciasCommand>("registrar-asistencias")
                .WithDescription("Convierte presentes del día en asistencias acumuladas.");
            config.AddCommand<RelevarAsistenciasCommand>("contar-asistencias")
                .WithDescription("Busca asistencias de hoy a partir de WhatsApp.");
            config.AddCommand<WappGruposCommand>("wapp-grupos")
                .WithDescription("Lista grupos y participantes de WhatsApp.");
            config.AddCommand<WappRecuperarTp1Tp2Command>("wapp-recuperar-tp1-tp2")
                .WithDescription("Envía un WhatsApp a alumnos que no presentaron TP1 ni TP2.");
            config.AddCommand<WappFotoParcialCommand>("wapp-foto-parcial")
                .WithDescription("Envía un WhatsApp a alumnos sin foto pidiéndoles una selfie para el parcial.");
            config.AddCommand<RegistrarRespuestasCommand>("registrar-respuestas")
                .WithDescription("Lee respuestas de WhatsApp y registra el código de cada alumno.");
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
            "listar" => "Listar alumnos",
            "sin-github" => "Listar alumnos sin GitHub",
            "sin-telefono" => "Listar alumnos sin teléfono",
            "sin-foto" => "Listar alumnos sin foto",
            "tp-no-presentado" => $"Listar alumnos que no presentaron TP{detalle}",
            "sin-practicos" => "Listar alumnos sin prácticos presentados",
            "limpiar-proyectos-practicos" => "Limpiar proyectos prácticos",
            "guardar" => "Guardar alumnos en Markdown",
            "json" => "Exportar alumnos a JSON",
            "vcf" => "Exportar alumnos a vCard",
            "informar-estado" => "Informar estado",
            "crear-carpetas" => "Crear carpetas",
            "publicar" => $"Publicar práctico{detalle}",
            "publicar-rehacer" => $"Publicar Rehacer{detalle}",
            "prs" => "Revisar pull requests",
            "normalizar-prs" => "Normalizar PRs",
            "bajar-prs" => $"Bajar PRs{detalle}",
            "cerrar-prs" => $"Cerrar PRs{detalle}",
            "revisar-presentados" => $"Revisar presentados{detalle}",
            "registrar-asistencias" => "Registrar asistencias",
            "contar-asistencias" => "Contar asistencias desde WhatsApp",
            "wapp-grupos" => "Listar grupos y participantes de WhatsApp",
            "wapp-recuperar-tp1-tp2" => "Recuperar TP1/TP2 por WhatsApp",
            "wapp-foto-parcial" => "Pedir foto para el parcial por WhatsApp",
            "registrar-respuestas" => "Registrar respuestas de WhatsApp",
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
            "listar" => ["listar"],
            "auditoria" => SolicitarMenuAuditoria(),
            "exportar" => SolicitarMenuExportar(),
            "crear-carpetas" => ["crear-carpetas"],
            "publicar" => ConstruirArgumentosPublicarPractico(),
            "publicar-rehacer" => ConstruirArgumentosPublicarRehacer(),
            "prs" => SolicitarMenuPrs(),
            "asistencias" => SolicitarMenuAsistencias(),
            "salir" => null,
            _ => Array.Empty<string>()
        };
    }

    static string[] SolicitarMenuAuditoria() {
        InteractiveChoice opcion = PedirOpcion(
            "[bold cyan]Principal / Auditoría[/] · Elegí una auditoría", [
                new("sin-github",                  "Sin GitHub",        "Filtrar alumnos sin usuario GitHub"),
                new("sin-telefono",                "Sin teléfono",      "Filtrar alumnos sin teléfono"),
                new("sin-foto",                    "Sin foto",          "Filtrar alumnos sin foto"),
                new("tp-no-presentado",            "TP no presentado",  "Elegir un TP y listar alumnos que adeudan ese práctico"),
                new("sin-practicos",               "Sin prácticos",     "Listar alumnos que no presentaron ningún práctico"),
                new("limpiar-proyectos-practicos", "Limpiar Prácticos", "Eliminar bin, obj, .vs y cachés dentro de prácticos"),
                new("volver",                      "Volver",            "Regresar al menú principal")
            ]);

        return opcion.Command switch {
            "sin-github" => ["sin-github"],
            "sin-telefono" => ["sin-telefono"],
            "sin-foto" => ["sin-foto"],
            "tp-no-presentado" => ConstruirArgumentosTpNoPresentado(),
            "sin-practicos" => ["sin-practicos"],
            "limpiar-proyectos-practicos" => ["limpiar-proyectos-practicos"],
            _ => Array.Empty<string>()
        };
    }

    static string[] SolicitarMenuExportar() {
        InteractiveChoice opcion = PedirOpcion(
            "[bold cyan]Principal / Exportar[/] · Elegí un formato", [
                new("guardar",         "Markdown",        "Guardar el listado en formato Markdown"),
                new("json",            "JSON",            "Exportar el listado en JSON"),
                new("vcf",             "vCard",           "Exportar contactos en formato vCard"),
                new("informar-estado", "Informar estado", "Publicar en ESTADO.md el estado de los prácticos presentados"),
                new("volver",          "Volver",          "Regresar al menú principal")
            ]);

        return opcion.Command switch {
            "guardar" => ConstruirArgumentosExportacion("Guardar Markdown", "guardar"),
            "json" => ConstruirArgumentosExportacion("Exportar JSON", "json"),
            "vcf" => ConstruirArgumentosExportacion("Exportar vCard", "vcf"),
            "informar-estado" => ["informar-estado"],
            _ => Array.Empty<string>()
        };
    }

    static string[] SolicitarMenuPrs() {
        InteractiveChoice opcion = PedirOpcion(
            "[bold cyan]Principal / PRs[/] · Elegí una acción", [
                new("prs",                 "Revisar PRs",          "Mostrar estado de pull requests"),
                new("normalizar-prs",      "Normalizar PRs",       "Ajustar títulos de pull requests"),
                new("bajar-prs",           "Bajar PRs",            "Descargar archivos de un trabajo práctico"),
                new("publicar",            "Publicar práctico",    "Copiar el enunciado de un TP a cada alumno"),
                new("publicar-rehacer",    "Publicar Rehacer",     "Borrar y republicar un TP solo a alumnos en Revisar"),
                new("cerrar-prs",          "Cerrar PRs",           "Cerrar pull requests abiertos"),
                new("revisar-presentados", "Revisar presentados",  "Marcar TPs presentados según líneas locales"),
                new("volver",              "Volver",               "Regresar al menú principal")
            ]);

        return opcion.Command switch {
            "prs" => ["prs"],
            "normalizar-prs" => ConstruirArgumentosNormalizarPrs(),
            "bajar-prs" => ConstruirArgumentosBajarPrs(),
            "publicar" => ConstruirArgumentosPublicarPractico(),
            "publicar-rehacer" => ConstruirArgumentosPublicarRehacer(),
            "cerrar-prs" => ConstruirArgumentosCerrarPrs(),
            "revisar-presentados" => ConstruirArgumentosRevisarPresentados(),
            _ => Array.Empty<string>()
        };
    }

    static string[] SolicitarMenuAsistencias() {
        InteractiveChoice opcion = PedirOpcion(
            "[bold cyan]Principal / Asistencias y WhatsApp[/] · Elegí una acción", [
                new("registrar-respuestas",   "Registrar respuestas",  "Leer respuestas de WhatsApp y registrar códigos"),
                new("contar-asistencias",     "Contar asistencias",    "Detectar presentes desde WhatsApp"),
                new("registrar-asistencias",  "Registrar asistencias", "Consolidar presentes del día"),
                new("wapp-grupos",            "WhatsApp grupos",       "Listar grupos y participantes"),
                new("wapp-recuperar-tp1-tp2", "Recuperar TP1/TP2",     "Enviar aviso a alumnos que no presentaron TP1 ni TP2"),
                new("wapp-foto-parcial",      "Foto para el parcial",  "Pedir selfie a alumnos sin foto de perfil"),
                new("volver",                 "Volver",                "Regresar al menú principal")
            ]);

        return opcion.Command switch {
            "registrar-asistencias" => ["registrar-asistencias"],
            "contar-asistencias" => ["contar-asistencias"],
            "wapp-grupos" => ["wapp-grupos"],
            "wapp-recuperar-tp1-tp2" => ConstruirArgumentosWappRecuperarTp1Tp2(),
            "wapp-foto-parcial" => ConstruirArgumentosWappFotoParcial(),
            "registrar-respuestas" => ["registrar-respuestas"],
            _ => Array.Empty<string>()
        };
    }

    static InteractiveChoice PedirOpcion(string titulo, IReadOnlyList<InteractiveChoice> opciones) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<InteractiveChoice>()
                .Title(titulo)
                .PageSize(12)
                .UseConverter(choice => $"[green]{choice.Label,-22}[/] [grey] {choice.Description}[/]")
                .AddChoices(opciones));

    static IReadOnlyList<InteractiveChoice> ObtenerOpcionesPrincipales() => [
            new("asistencias",    "Asistencias y WhatsApp", "Acciones vinculadas a presentes y grupos"),
            new("listar",         "Listar",                 "Mostrar todos los alumnos"),
            new("auditoria",      "Auditoría",              "Revisar datos faltantes o incompletos"),
            new("exportar",       "Exportar",               "Guardar o exportar en distintos formatos"),
            new("crear-carpetas", "Crear carpetas",         "Crear o normalizar carpetas de alumnos"),
            new("publicar",       "Publicar práctico",      "Copiar el enunciado de un TP a cada alumno"),
            new("publicar-rehacer","Publicar Rehacer",       "Borrar y republicar un TP solo a alumnos en Revisar"),
            new("prs",            "Presentaciones",         "Operaciones sobre pull requests y prácticos"),
            new("salir",          "Salir",                  "Cerrar la aplicación")
        ];

    static string[] ConstruirArgumentosExportacion(string accion, string comando) {
        InteractiveChoice modoRuta = PedirOpcion(
            $"[bold cyan]{accion}[/] · Elegí la ruta de salida", [
                new("predeterminada", "Predeterminada", "Usar la ruta de salida configurada"),
                new("personalizada",  "Personalizada",  "Ingresar una ruta manualmente"),
                new("cancelar",       "Cancelar",       "Volver al menú sin ejecutar")
            ]);

        if (modoRuta.Command == "cancelar") { return Array.Empty<string>(); }
        if (modoRuta.Command == "predeterminada") { return [comando]; }

        string ruta = AnsiConsole.Prompt(
            new TextPrompt<string>($"[bold cyan]{accion}[/] · Ruta de salida ([grey]vacío = cancelar[/]):")
                .AllowEmpty());

        return string.IsNullOrWhiteSpace(ruta)
            ? Array.Empty<string>()
            : [comando, ruta.Trim()];
    }

    static string[] ConstruirArgumentosNormalizarPrs() {
        string modo = PedirModoEjecucion("Normalizar PRs");

        return modo switch {
            "simular" => ["normalizar-prs", "--simular"],
            "ejecutar" => ["normalizar-prs"],
            _ => Array.Empty<string>()
        };
    }

    static string[] ConstruirArgumentosTpNoPresentado() {
        string? trabajoPractico = PedirTrabajoPractico("TP no presentado");

        return trabajoPractico is null
            ? Array.Empty<string>()
            : ["tp-no-presentado", trabajoPractico];
    }

    static string[] ConstruirArgumentosBajarPrs() {
        InteractiveChoice alcance = PedirOpcion(
            "[bold cyan]Bajar PRs[/] · Elegí el alcance", [
                new("todos",    "Todos",    "Bajar todos los TPs detectados en cada PR"),
                new("por-tp",   "Por TP",   "Bajar solo los PRs que tienen archivos de un TP"),
                new("cancelar", "Cancelar", "Volver al menú sin ejecutar")
            ]);

        if (alcance.Command == "cancelar") { return Array.Empty<string>(); }

        string? trabajoPractico = null;
        if (alcance.Command == "por-tp") {
            trabajoPractico = PedirTrabajoPractico("Bajar PRs");
            if (trabajoPractico is null) { return Array.Empty<string>(); }
        }

        string sobrescritura = PedirModoSobrescritura("Bajar PRs");

        return sobrescritura switch {
            "conservar" when trabajoPractico is null => ["bajar-prs"],
            "conservar" => ["bajar-prs", trabajoPractico],
            "sobrescribir" when trabajoPractico is null => ["bajar-prs", "--forzar"],
            "sobrescribir" => ["bajar-prs", trabajoPractico, "--forzar"],
            _ => Array.Empty<string>()
        };
    }

    static string[] ConstruirArgumentosPublicarPractico() {
        string? trabajoPractico = PedirTrabajoPractico("Publicar práctico");
        if (trabajoPractico is null) { return Array.Empty<string>(); }

        string sobrescritura = PedirModoSobrescritura("Publicar práctico");

        return sobrescritura switch {
            "conservar" => ["publicar", trabajoPractico],
            "sobrescribir" => ["publicar", trabajoPractico, "--forzar"],
            _ => Array.Empty<string>()
        };
    }

    static string[] ConstruirArgumentosPublicarRehacer() {
        string? trabajoPractico = PedirTrabajoPractico("Publicar Rehacer");

        if (trabajoPractico is null) { return Array.Empty<string>(); }

        return ["publicar-rehacer", trabajoPractico];
    }

    static string[] ConstruirArgumentosCerrarPrs() {
        InteractiveChoice alcance = PedirOpcion(
            "[bold cyan]Cerrar PRs[/] · Elegí el alcance", [
                new("todos",    "Todos",     "Cerrar todos los PRs abiertos"),
                new("por-tp",   "Por TP",    "Cerrar sólo los PRs de un trabajo práctico"),
                new("cancelar", "Cancelar",  "Volver al menú sin ejecutar")
            ]);

        return alcance.Command switch {
            "todos" => ["cerrar-prs"],
            "por-tp" => ConstruirArgumentosCerrarPrsPorTp(),
            _ => Array.Empty<string>()
        };
    }

    static string[] ConstruirArgumentosCerrarPrsPorTp() {
        string? trabajoPractico = PedirTrabajoPractico("Cerrar PRs");

        return trabajoPractico is null
            ? Array.Empty<string>()
            : ["cerrar-prs", trabajoPractico];
    }

    static string[] ConstruirArgumentosRevisarPresentados() {
        string? trabajoPractico = PedirTrabajoPractico("Revisar presentados");

        return trabajoPractico is null
            ? Array.Empty<string>()
            : ["revisar-presentados", trabajoPractico];
    }

    static string[] ConstruirArgumentosWappRecuperarTp1Tp2() {
        InteractiveChoice seleccion = PedirOpcion(
            "[bold cyan]Recuperar TP1/TP2 por WhatsApp[/] · Elegí qué TP recuperar", [
                new("1",        "TP1",      "Enviar aviso por TP1"),
                new("2",        "TP2",      "Enviar aviso por TP2"),
                new("ambos",    "Ambos",    "Enviar aviso por TP1 y TP2"),
                new("cancelar", "Cancelar", "Volver al menú sin ejecutar")
            ]);

        if (seleccion.Command == "cancelar") { return Array.Empty<string>(); }

        string? tpArg = seleccion.Command switch {
            "1" => "1",
            "2" => "2",
            _ => null
        };

        string modo = PedirModoEjecucion("Recuperar TP1/TP2 por WhatsApp", "Enviar mensajes reales");
        if (modo == "cancelar") { return Array.Empty<string>(); }

        List<string> args = ["wapp-recuperar-tp1-tp2"];
        if (tpArg is not null) args.Add(tpArg);
        if (modo == "simular") args.Add("--simular");
        return [.. args];
    }

    static string[] ConstruirArgumentosWappFotoParcial() {
        string modo = PedirModoEjecucion("Foto para el parcial por WhatsApp", "Enviar mensajes reales");

        return modo switch {
            "simular" => ["wapp-foto-parcial", "--simular"],
            "ejecutar" => ["wapp-foto-parcial"],
            _ => Array.Empty<string>()
        };
    }

    static string PedirModoEjecucion(string accion, string etiquetaEjecucion = "Ejecutar cambios reales") {
        InteractiveChoice opcion = PedirOpcion(
            $"[bold cyan]{accion}[/] · Elegí el modo de ejecución", [
                new("simular",  "Simulación", etiquetaEjecucion == "Enviar mensajes reales" ? "Previsualizar sin enviar mensajes" : "Previsualizar sin aplicar cambios"),
                new("ejecutar", "Real",       etiquetaEjecucion),
                new("cancelar", "Cancelar",   "Volver al menú sin ejecutar")
            ]);

        return opcion.Command;
    }

    static string PedirModoSobrescritura(string accion) {
        InteractiveChoice opcion = PedirOpcion(
            $"[bold cyan]{accion}[/] · Elegí cómo manejar archivos existentes", [
                new("conservar",     "Conservar",     "No reemplazar archivos ya existentes"),
                new("sobrescribir",  "Sobrescribir",  "Reemplazar archivos ya existentes"),
                new("cancelar",      "Cancelar",      "Volver al menú sin ejecutar")
            ]);

        return opcion.Command;
    }

    static string? PedirTrabajoPractico(string accion) {
        IReadOnlyList<EnunciadoPracticoDisponible> practicos = AppPaths.ListarEnunciadosPracticos();
        if (practicos.Count == 0) {
            string valor = AnsiConsole.Prompt(
                new TextPrompt<string>($"[bold cyan]{accion}[/] · Trabajo práctico ([green]TP1[/] o [green]1[/], [grey]vacío = cancelar[/]):")
                    .PromptStyle("cyan")
                    .AllowEmpty()
                    .Validate(valor =>
                        string.IsNullOrWhiteSpace(valor) || AlumnosCliActions.EsTrabajoPracticoValido(valor)
                            ? ValidationResult.Success()
                            : ValidationResult.Error(AlumnosCliActions.MensajeTrabajoPracticoInvalido(valor))));

            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        List<InteractiveChoice> opciones = [
            .. practicos.Select(practico => new InteractiveChoice(practico.Carpeta, $"TP{practico.Numero}", practico.Carpeta)),
            new("cancelar", "Cancelar", "Volver al menú sin ejecutar")
        ];

        InteractiveChoice seleccionado = AnsiConsole.Prompt(
            new SelectionPrompt<InteractiveChoice>()
                .Title($"[bold cyan]{accion}[/] · Elegí el trabajo práctico\n[grey]Se encontraron {practicos.Count} en {AppPaths.EnunciadosDirectory}[/]")
                .PageSize(12)
                .UseConverter(choice => $"[green]{choice.Label,-22}[/] [grey] {choice.Description}[/]")
                .AddChoices(opciones));

        return seleccionado.Command == "cancelar" ? null : seleccionado.Command;
    }

    static bool EsAliasAyuda(string valor) =>
        string.Equals(valor, "ayuda", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "help", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "-h", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "--help", StringComparison.OrdinalIgnoreCase);

    sealed record InteractiveChoice(string Command, string Label, string Description);
}
