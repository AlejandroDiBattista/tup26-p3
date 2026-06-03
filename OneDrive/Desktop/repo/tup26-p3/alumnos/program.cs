using Spectre.Console.Cli;

namespace Tup26.AlumnosApp;

class Program {
    static int Main(string[] args) {
        CommandApp app = AlumnosCliApp.Crear();
        Console.Clear();
        if (args.Length == 0) { return AlumnosCliApp.EjecutarModoInteractivo(app); }

        args = AlumnosCliApp.NormalizarArgumentosAyuda(args);
        if (args.Length == 0) { return 0; }

        return app.Run(args);
    }
}
