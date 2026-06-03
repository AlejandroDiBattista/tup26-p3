#!/usr/bin/env -S dotnet run
#:package Spectre.Console.Cli@0.55.0
#:property PublishAot=false

using System.ComponentModel;
using Spectre.Console.Cli;

CommandApp<SortxCommand> app = new();
app.Configure(config => { config.SetApplicationName("sortx"); });

return app.Run(args);

// Crea una clase para definir el comando.

class SortxCommand : Command<SortxCommand.Settings> {

    // Crea una clase para cargar la configuración del comando a partir de los argumentos y opciones.    
    public class Settings : CommandSettings {
        [CommandArgument(0, "[input]")]
        [Description("Archivo de entrada. Si se omite, lee desde stdin.")]
        public string? InputFile { get; init; }

        [CommandArgument(1, "[output]")]
        [Description("Archivo de salida. Si se omite, escribe en stdout.")]
        public string? OutputFile { get; init; }

        [CommandOption("-b|--by <campo:orden>")]
        [Description("Campo de ordenamiento. Repetible; orden: asc o desc.")]
        public string[] SortSpecs { get; init; } = [];

        [CommandOption("-d|--delimiter <delimitador>")]
        [Description("Delimitador de columnas. Default: , ; usar \\t para tabulación.")]
        [DefaultValue(",")]
        public string Delimiter { get; init; } = ",";
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation) {
        Console.WriteLine($"""
        Parametros capturados por Spectre.Console.Cli:
          input     : {settings.InputFile ?? "(stdin)"}
          output    : {settings.OutputFile ?? "(stdout)"}
          by        : {(settings.SortSpecs.Length == 0 ? "(ninguno)" : string.Join(", ", settings.SortSpecs))}
          delimiter : '{settings.Delimiter}'
        """);
        return 0;
    }
}
