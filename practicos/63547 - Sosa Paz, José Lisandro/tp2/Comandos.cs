using System;

static class Comandos {
    public static bool Procesar(string[] args) {
        switch (args) {
            case ["--help"] or ["-h"] or ["--ayuda"]:
                Console.WriteLine("""

""");
                return true;

            case ["--probar"] or ["-p"] or ["--test"] or ["-t"]:
                Pruebas.Ejecutar();
                return true;

            case [var expresion, var valor]:
                try {
                    var x = int.Parse(valor);
                    var funcion = Compilador.Parse(expresion);
                    Console.WriteLine(funcion.Evaluar(x));
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
                return true;

            default:
                return false;
        }
    }
}
