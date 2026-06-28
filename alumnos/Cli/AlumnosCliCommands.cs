using Spectre.Console.Cli;

namespace Tup26.AlumnosApp;

sealed class VacioSettings : CommandSettings;

sealed class RutaSalidaSettings : CommandSettings {
    [CommandArgument(0, "[ruta]")]
    public string? Ruta { get; init; }
}

class TrabajoPracticoSettings : CommandSettings {
    [CommandArgument(0, "<tp>")]
    public string TrabajoPractico { get; init; } = string.Empty;
}

sealed class TrabajoPracticoOpcionalSettings : CommandSettings {
    [CommandArgument(0, "[tp]")]
    public string? TrabajoPractico { get; init; }
}

sealed class LegajoOpcionalSettings : CommandSettings {
    [CommandArgument(0, "[legajo]")]
    public int? Legajo { get; init; }
}

sealed class PublicarPracticoSettings : TrabajoPracticoSettings {
    [CommandOption("--forzar")]
    public bool Forzar { get; init; }
}

sealed class CapturarPantallasSettings : TrabajoPracticoSettings {
    [CommandOption("--headed")]
    public bool Headed { get; init; }

    [CommandOption("--forzar")]
    public bool Forzar { get; init; }

    [CommandOption("--ruta <RUTA>")]
    public string Ruta { get; init; } = "/contactos";
}

sealed class ListarAlumnosCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.Listar();
}

sealed class ListarPracticosFaltantesCommand : Command<TrabajoPracticoSettings> {
    protected override int Execute(CommandContext context, TrabajoPracticoSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.ListarTpNoPresentado(settings.TrabajoPractico);
}

sealed class LimpiarArchivosTemporalesCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.LimpiarProyectosPracticos();
}

sealed class NormalizarCarpetasCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.NormalizarCarpetas();
}

sealed class ExportarMarkdownCommand : Command<RutaSalidaSettings> {
    protected override int Execute(CommandContext context, RutaSalidaSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.GuardarMarkdown(settings.Ruta);
}

sealed class ExportarJsonCommand : Command<RutaSalidaSettings> {
    protected override int Execute(CommandContext context, RutaSalidaSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.GuardarJson(settings.Ruta);
}

sealed class ExportarVCardCommand : Command<RutaSalidaSettings> {
    protected override int Execute(CommandContext context, RutaSalidaSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.GuardarVcf(settings.Ruta);
}

sealed class ExportarEstadoCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.PublicarEstadoInformer();
}

sealed class PublicarPracticoCommand : Command<PublicarPracticoSettings> {
    protected override int Execute(CommandContext context, PublicarPracticoSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.PublicarPractico(settings.TrabajoPractico, settings.Forzar);
}

sealed class PublicarApuntesCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.PublicarApuntes();
}

sealed class RevisarPrsCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.RevisarPullRequests();
}

sealed class BajarPrsCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.BajarPullRequests();
}

sealed class CerrarPrsCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.CerrarPullRequests();
}

sealed class RevisarPresentacionesCommand : Command<TrabajoPracticoOpcionalSettings> {
    protected override int Execute(CommandContext context, TrabajoPracticoOpcionalSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.RevisarPresentados(settings.TrabajoPractico);
}

sealed class VerificarCompilacionCommand : Command<TrabajoPracticoSettings> {
    protected override int Execute(CommandContext context, TrabajoPracticoSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.VerificarCompilacion(settings.TrabajoPractico);
}

sealed class VerificarEjecucionCommand : Command<TrabajoPracticoSettings> {
    protected override int Execute(CommandContext context, TrabajoPracticoSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.VerificarEjecucion(settings.TrabajoPractico);
}

sealed class CapturarPantallasCommand : Command<CapturarPantallasSettings> {
    protected override int Execute(CommandContext context, CapturarPantallasSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.CapturarPantallas(settings.TrabajoPractico, settings.Ruta, settings.Headed, settings.Forzar);
}

sealed class EjecutarTp5Command : Command<LegajoOpcionalSettings> {
    protected override int Execute(CommandContext context, LegajoOpcionalSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.EjecutarTp5(settings.Legajo);
}

sealed class EjecutarTp6Command : Command<LegajoOpcionalSettings> {
    protected override int Execute(CommandContext context, LegajoOpcionalSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.EjecutarTp6(settings.Legajo);
}

sealed class ContarAsistenciasCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.RelevarAsistencias();
}

sealed class ListarGruposWhatsAppCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.WappGrupos();
}
