class Pruebas {
    public static void Ejecutar() {
        Console.WriteLine("Ejecutando pruebas automáticas...");

        var numero = 1;

        Probar(ref numero, "Casos mínimos de evaluación del enunciado", () => {
            AfirmarEvaluacion("1 + 2 * 3", 0, 7);
            AfirmarEvaluacion("1 + 2 * x", 10, 21);
            AfirmarEvaluacion("(x - 1) * (x - 8 / 4) + 3", 10, 75);
            AfirmarEvaluacion("-(3 + 2)", 0, -5);
            AfirmarEvaluacion("10 / 2", 0, 5);
        });

        Probar(ref numero, "Variables, mayúsculas y operadores unarios", () => {
            AfirmarEvaluacion("1 + 2 * x", 5, 11);
            AfirmarEvaluacion("(x - 1) * (x - 8 / 4) + 3", 5, 15);
            AfirmarEvaluacion("+X", 7, 7);
            AfirmarEvaluacion("-x", 3, -3);
        });

        Probar(ref numero, "Errores de parsing del enunciado", () => {
            AfirmarExcepcion<FormatException>(() => Compilador.Parse("(1 + 2"), "Se esperaba ')'", "paréntesis sin cerrar");
            AfirmarExcepcion<FormatException>(() => Compilador.Parse(""), "Token inesperado", "entrada vacía");
            AfirmarExcepcion<FormatException>(() => Compilador.Parse("1 + ?"), "Token inesperado", "token inesperado");
        });

        Probar(ref numero, "Errores de evaluación", () => {
            AfirmarExcepcion<DivideByZeroException>(() => Compilador.Parse("10 / (x - 2)").Evaluar(2), null, "división por cero");
        });

        Console.WriteLine($"Todas las pruebas pasaron correctamente. Total: {numero - 1} grupos.");
    }

    private static void Probar(ref int numero, string descripcion, Action accion) {
        Console.WriteLine($"{numero}. {descripcion}");
        accion();
        numero++;
    }

    private static void AfirmarEvaluacion(string expresion, int x, int esperado) {
        var resultado = Compilador.Parse(expresion).Evaluar(x);
        Afirmar(
            resultado == esperado,
            $"La expresión '{expresion}' con x = {x} debería dar {esperado}, pero dio {resultado}."
        );
    }

    private static void AfirmarExcepcion<TException>(Action accion, string? mensajeEsperado, string descripcion)
        where TException : Exception {
        try {
            accion();
        } catch (TException ex) {
            if (mensajeEsperado is not null) {
                Afirmar(
                    ex.Message.Contains(mensajeEsperado, StringComparison.Ordinal),
                    $"La prueba '{descripcion}' esperaba un mensaje que contuviera '{mensajeEsperado}', pero recibió '{ex.Message}'."
                );
            }

            return;
        }

        throw new InvalidOperationException($"La prueba '{descripcion}' esperaba una excepción de tipo {typeof(TException).Name}.");
    }

    private static void Afirmar(bool condicion, string mensaje) {
        if (!condicion) {
            throw new InvalidOperationException(mensaje);
        }
    }
}

