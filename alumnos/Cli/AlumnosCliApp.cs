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
            int codigo = app.Run(args);
            AnsiConsole.WriteLine();

            if (codigo != 0) { AnsiConsole.MarkupLine($"[red]El comando terminó con código {codigo}.[/]"); }

            AnsiConsole.MarkupLine("[grey]Presioná una tecla para volver al menú...[/]");
            Console.ReadKey(intercept: true);
            AnsiConsole.Clear();
        }
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
            "listar"         => ["listar"],
            "auditoria"      => SolicitarMenuAuditoria(),
            "exportar"       => SolicitarMenuExportar(),
            "crear-carpetas" => ["crear-carpetas"],
            "publicar"       => ConstruirArgumentosPublicarPractico(),
            "prs"            => SolicitarMenuPrs(),
            "asistencias"    => SolicitarMenuAsistencias(),
            "salir"          => null,
            _                => Array.Empty<string>()
        };
    }

    static string[] SolicitarMenuAuditoria() {
        InteractiveChoice opcion = PedirOpcion(
            "[bold cyan]Principal / Auditoría[/] · Elegí una auditoría", [
                new("sin-github",                  "Sin GitHub",                   "Filtrar alumnos sin usuario GitHub"),
                new("sin-telefono",                "Sin teléfono",                 "Filtrar alumnos sin teléfono"),
                new("sin-foto",                    "Sin foto",                     "Filtrar alumnos sin foto"),
                new("tp-no-presentado",            "TP no presentado",             "Elegir un TP y listar alumnos que adeudan ese práctico"),
                new("sin-practicos",               "Sin prácticos",                "Listar alumnos que no presentaron ningún práctico"),
                new("limpiar-proyectos-practicos", "Limpiar Proyectos Prácticos",  "Eliminar bin, obj, .vs y cachés dentro de prácticos"),
                new("volver",                      "Volver",                       "Regresar al menú principal")
            ]);

        return opcion.Command switch {
            "sin-github"        => ["sin-github"],
            "sin-telefono"      => ["sin-telefono"],
            "sin-foto"          => ["sin-foto"],
            "tp-no-presentado"  => ["tp-no-presentado", PedirTrabajoPractico()],
            "sin-practicos"     => ["sin-practicos"],
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
            "guardar" => ConstruirArgumentosExportacion("guardar"),
            "json"    => ConstruirArgumentosExportacion("json"),
            "vcf"     => ConstruirArgumentosExportacion("vcf"),
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
                new("cerrar-prs",          "Cerrar PRs",           "Cerrar pull requests abiertos"),
                new("revisar-presentados", "Revisar presentados",  "Marcar TPs presentados según líneas locales"),
                new("volver",              "Volver",               "Regresar al menú principal")
            ]);

        return opcion.Command switch {
            "prs" => ["prs"],
            "normalizar-prs"        => ConstruirArgumentosNormalizarPrs(),
            "bajar-prs"             => ConstruirArgumentosBajarPrs(),
            "publicar"              => ConstruirArgumentosPublicarPractico(),
            "cerrar-prs"            => ConstruirArgumentosCerrarPrs(),
            "revisar-presentados"   => ["revisar-presentados", PedirTrabajoPractico()],
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
            "registrar-asistencias"  => ["registrar-asistencias"],
            "contar-asistencias"     => ["contar-asistencias"],
            "wapp-grupos"            => ["wapp-grupos"],
            "wapp-recuperar-tp1-tp2" => ConstruirArgumentosWappRecuperarTp1Tp2(),
            "wapp-foto-parcial"      => ConstruirArgumentosWappFotoParcial(),
            "registrar-respuestas"   => ["registrar-respuestas"],
            _ => Array.Empty<string>()
        };
    }

    static InteractiveChoice PedirOpcion(string titulo, IReadOnlyList<InteractiveChoice> opciones) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<InteractiveChoice>()
                .Title(titulo)
                .PageSize(12)
                .UseConverter(choice => $"[green]{choice.Label, -22}[/] [grey] {choice.Description}[/]")
                .AddChoices(opciones));

    static IReadOnlyList<InteractiveChoice> ObtenerOpcionesPrincipales() => [
            new("asistencias",    "Asistencias y WhatsApp", "Acciones vinculadas a presentes y grupos"),
            new("listar",         "Listar",                 "Mostrar todos los alumnos"),
            new("auditoria",      "Auditoría",              "Revisar datos faltantes o incompletos"),
            new("exportar",       "Exportar",               "Guardar o exportar en distintos formatos"),
            new("crear-carpetas", "Crear carpetas",         "Crear o normalizar carpetas de alumnos"),
            new("publicar",       "Publicar práctico",      "Copiar el enunciado de un TP a cada alumno"),
            new("prs",            "Presentaciones",         "Operaciones sobre pull requests y prácticos"),
            new("salir",          "Salir",                  "Cerrar la aplicación")
        ];

    static string[] ConstruirArgumentosExportacion(string comando) {
        string ruta = AnsiConsole.Prompt(
            new TextPrompt<string>($"Ruta de salida para [green]{comando}[/] ([grey]vacío = predeterminada[/]):")
                .AllowEmpty());

        return string.IsNullOrWhiteSpace(ruta) ? [comando] : [comando, ruta.Trim()];
    }

    static string[] ConstruirArgumentosNormalizarPrs() {
        bool simular = AnsiConsole.Confirm("¿Ejecutar en modo simulación?", true);

        return simular ? ["normalizar-prs", "--simular"] : ["normalizar-prs"];
    }

    static string[] ConstruirArgumentosBajarPrs() {
        string trabajoPractico = PedirTrabajoPractico();
        bool forzar = AnsiConsole.Confirm("¿Sobrescribir archivos ya existentes?", false);

        return forzar
            ? ["bajar-prs", trabajoPractico, "--forzar"]
            : ["bajar-prs", trabajoPractico];
    }

    static string[] ConstruirArgumentosPublicarPractico() {
        string trabajoPractico = PedirTrabajoPractico();
        bool forzar = AnsiConsole.Confirm("¿Sobrescribir archivos ya existentes?", false);

        return forzar
            ? ["publicar", trabajoPractico, "--forzar"]
            : ["publicar", trabajoPractico];
    }

    static string[] ConstruirArgumentosCerrarPrs() {
        bool filtrarPorTp = AnsiConsole.Confirm("¿Querés cerrar sólo los PRs de un TP específico?", false);

        return filtrarPorTp
            ? ["cerrar-prs", PedirTrabajoPractico()]
            : ["cerrar-prs"];
    }

    static string[] ConstruirArgumentosWappRecuperarTp1Tp2() {
        string seleccion = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("¿Qué TP querés recuperar?")
                .AddChoices("TP1", "TP2", "Ambos (TP1 y TP2)"));

        string? tpArg = seleccion switch {
            "TP1" => "1",
            "TP2" => "2",
            _     => null
        };

        bool enviar = AnsiConsole.Confirm("¿Enviar WhatsApp reales ahora?", false);

        List<string> args = ["wapp-recuperar-tp1-tp2"];
        if (tpArg is not null) args.Add(tpArg);
        if (!enviar) args.Add("--simular");
        return [.. args];
    }

    static string[] ConstruirArgumentosWappFotoParcial() {
        bool enviar = AnsiConsole.Confirm("¿Enviar WhatsApp reales ahora?", false);

        return enviar
            ? ["wapp-foto-parcial"]
            : ["wapp-foto-parcial", "--simular"];
    }

    static string PedirTrabajoPractico() {
        IReadOnlyList<EnunciadoPracticoDisponible> practicos = AppPaths.ListarEnunciadosPracticos();
        if (practicos.Count == 0) {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Trabajo práctico ([green]TP1[/] o [green]1[/]):")
                    .PromptStyle("cyan")
                    .Validate(valor =>
                        AlumnosCliActions.EsTrabajoPracticoValido(valor)
                            ? ValidationResult.Success()
                            : ValidationResult.Error(AlumnosCliActions.MensajeTrabajoPracticoInvalido(valor))));
        }

        EnunciadoPracticoDisponible seleccionado = AnsiConsole.Prompt(
            new SelectionPrompt<EnunciadoPracticoDisponible>()
                .Title($"Trabajo práctico · se encontraron [green]{practicos.Count}[/] en [grey]{AppPaths.EnunciadosDirectory}[/]")
                .PageSize(12)
                .UseConverter(practico => $"[green]TP{practico.Numero}[/] [grey]{practico.Carpeta}[/]")
                .AddChoices(practicos));

        return seleccionado.Carpeta;
    }

    static bool EsAliasAyuda(string valor) =>
        string.Equals(valor, "ayuda",  StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "help",   StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "-h",     StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor, "--help", StringComparison.OrdinalIgnoreCase);

    sealed record InteractiveChoice(string Command, string Label, string Description);
}
