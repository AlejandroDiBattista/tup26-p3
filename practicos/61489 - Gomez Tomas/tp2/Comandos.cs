namespace Calculadora;

static class Comandos {
    public static bool Procesar(string[] args) {
        switch (args) {
            case ["--help"] or ["-h"] or ["--ayuda"]:
                Console.WriteLine("Uso: dotnet run -- [expresion valor] [--test]");
                return true;

            case ["--test"] or ["-t"] or ["--probar"] or ["-p"]:
                Pruebas.Ejecutar();
                return true;

            case [var expresion, var valor]:
                if (int.TryParse(valor, out int x)) {
                    var ast = Compilador.Parse(expresion);
                    Console.WriteLine(ast.Evaluar(x));
                } else {
                    Console.WriteLine("Error: El valor de x debe ser un entero.");
                }
                return true;

            default:
                return false;
        }
    }
}