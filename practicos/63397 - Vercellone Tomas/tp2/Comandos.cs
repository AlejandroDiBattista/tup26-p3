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
    Pueden contener operaciones básicas (+, -, *, /) y la variable 'x'.
    Ejemplo: (x - 1) * (x - 8/4) + 3

Opciones:
    --help,   -h,  --ayuda             Muestra esta ayuda.
    --test,   -t,  --probar,  -p       Ejecuta pruebas automáticas.
""");
                return true;

            case ["--probar"] or ["-p"] or ["--test"] or ["-t"] or ["--prueba"]:
                Pruebas.Ejecutar();
                return true;

            case [var expresion, var valor]:
                if (!int.TryParse(valor, out int x)) {
                    Console.Error.WriteLine($"Error: '{valor}' no es un número entero válido.");
                    return true;
                }
                var funcion = Compilador.Parse(expresion);
                Console.WriteLine(funcion.Evaluar(x));
                return true;

            default:
                return false;
        }
    }
}
