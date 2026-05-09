using Spectre.Console.Cli;

namespace Tup26.AlumnosApp;

sealed class VacioSettings : CommandSettings;

sealed class RutaSalidaSettings : CommandSettings {
    [CommandArgument(0, "[ruta]")]
    public string? Ruta { get; init; }
}

sealed class NormalizarPrsSettings : CommandSettings {
    [CommandOption("--simular")]
    public bool Simular { get; init; }
}

sealed class SimularSettings : CommandSettings {
    [CommandOption("--simular")]
    public bool Simular { get; init; }
}

class TrabajoPracticoSettings : CommandSettings {
    [CommandArgument(0, "<tp>")]
    public string TrabajoPractico { get; init; } = string.Empty;
}

sealed class BajarPrsSettings : TrabajoPracticoSettings {
    [CommandOption("--forzar")]
    public bool Forzar { get; init; }
}

sealed class CerrarPrsSettings : CommandSettings {
    [CommandArgument(0, "[tp]")]
    public string? TrabajoPractico { get; init; }
}

sealed class ListarCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.Listar();
}

sealed class SinGithubCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.ListarSinGithub();
}

sealed class SinTelefonoCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.ListarSinTelefono();
}

sealed class SinFotoCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.ListarSinFoto();
}

sealed class Tp1NoPresentadoCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.ListarTp1NoPresentado();
}

sealed class Tp2NoPresentadoCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.ListarTp2NoPresentado();
}

sealed class LimpiarProyectosPracticosCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.LimpiarProyectosPracticos();
}

sealed class GuardarCommand : Command<RutaSalidaSettings> {
    protected override int Execute(CommandContext context, RutaSalidaSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.GuardarMarkdown(settings.Ruta);
}

sealed class JsonCommand : Command<RutaSalidaSettings> {
    protected override int Execute(CommandContext context, RutaSalidaSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.GuardarJson(settings.Ruta);
}

sealed class VcfCommand : Command<RutaSalidaSettings> {
    protected override int Execute(CommandContext context, RutaSalidaSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.GuardarVcf(settings.Ruta);
}

sealed class InformerEstadoCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.PublicarEstadoInformer();
}

sealed class CrearCarpetasCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.CrearCarpetas();
}

sealed class PrsCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.RevisarPullRequests();
}

sealed class NormalizarPrsCommand : Command<NormalizarPrsSettings> {
    protected override int Execute(CommandContext context, NormalizarPrsSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.NormalizarPullRequests(settings.Simular);
}

sealed class BajarPrsCommand : Command<BajarPrsSettings> {
    protected override int Execute(CommandContext context, BajarPrsSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.BajarPullRequests(settings.TrabajoPractico, settings.Forzar);
}

sealed class CerrarPrsCommand : Command<CerrarPrsSettings> {
    protected override int Execute(CommandContext context, CerrarPrsSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.CerrarPullRequests(settings.TrabajoPractico);
}

sealed class RevisarPresentadosCommand : Command<TrabajoPracticoSettings> {
    protected override int Execute(CommandContext context, TrabajoPracticoSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.RevisarPresentados(settings.TrabajoPractico);
}

sealed class RegistrarAsistenciasCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.RegistrarAsistencias();
}

sealed class RelevarAsistenciasCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.RelevarAsistencias();
}

sealed class WappGruposCommand : Command<VacioSettings> {
    protected override int Execute(CommandContext context, VacioSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.WappGrupos();
}

sealed class WappRecuperarTp1Tp2Command : Command<SimularSettings> {
    protected override int Execute(CommandContext context, SimularSettings settings, CancellationToken cancellationToken) =>
        AlumnosCliActions.WappRecuperarTp1Tp2(settings.Simular);
}
