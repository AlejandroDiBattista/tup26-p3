static class Comandos {
    public static bool Procesar(string[] args) {
        switch (args) {
            case ["--help"] or ["-h"] or ["--ayuda"]:
                Console.WriteLine("""

Uso: dotnet run -- [opciones] [<expresión> <valor>]

    Este programa permite analizar y evaluar expresiones matemáticas
    que pueden incluir la variable 'x'.

    Si se proporciona una expresión junto con un valor, el programa
    reemplaza 'x' por ese valor y muestra el resultado.

    Si se ejecuta sin argumentos, inicia un modo interactivo para
    ingresar una expresión y evaluarla con distintos valores de 'x'.

Expresiones válidas:
- Pueden contener expresiones matemáticas básicas y la variable 'x'.
- Ejemplo: (x - 1) * (x - 8/4) + 3

Opciones:
    --help, -h, --ayuda                  Muestra esta ayuda.
    --test, -t, --probar, -p            Ejecuta pruebas automáticas.

""");
                return true;

            case ["--probar"] or ["-p"] or ["--test"] or ["-t"]:
                Pruebas.Ejecutar();
                return true;

            case [var expresion, var valor]:
                if (!int.TryParse(valor, out var x)) {
                    Console.WriteLine("Valor de x inválido.");
                    return true;
                }

                try {
                    var funcion = Compilador.Parse(expresion);
                    Console.WriteLine(funcion.Evaluar(x));
                } catch (Exception ex) when (
                    ex is FormatException ||
                    ex is DivideByZeroException
                ) {
                    Console.WriteLine(ex.Message);
                }

                return true;

            default:
                return false;
        }
    }
}