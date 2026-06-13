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

sealed class PublicarPracticoSettings : TrabajoPracticoSettings {
    [CommandOption("--forzar")]
    public bool Forzar { get; init; }
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

sealed class ContarAsistenciasCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.RelevarAsistencias();
}

sealed class ListarGruposWhatsAppCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.WappGrupos();
}
