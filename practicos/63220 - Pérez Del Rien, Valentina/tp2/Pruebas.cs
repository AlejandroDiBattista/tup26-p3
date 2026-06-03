using System;

class Pruebas {
    public static void Ejecutar() {
        Console.WriteLine("Ejecutando pruebas automáticas...\n");

        int numero = 1;

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

        Probar(ref numero, "Paréntesis y precedencia", () => {
            AfirmarEvaluacion("(1 + 2) * 3", 0, 9);
            AfirmarEvaluacion("1 + (2 * 3)", 0, 7);
            AfirmarEvaluacion("((2 + 3) * 4)", 0, 20);
        });

        Probar(ref numero, "Errores de parsing", () => {
            AfirmarExcepcion(() => Parsear("(1 + 2"), "Paréntesis sin cerrar", "paréntesis sin cerrar");
            AfirmarExcepcion(() => Parsear(""), "Entrada vacía", "entrada vacía");
            AfirmarExcepcion(() => Parsear("1 + ?"), "Token inesperado", "token inesperado");
        });

        Probar(ref numero, "Errores de evaluación", () => {
            AfirmarExcepcion(() => Evaluar("10 / (x - 2)", 2), "División por cero", "división por cero");
        });

        Console.WriteLine($"\nTodas las pruebas pasaron correctamente. Total: {numero - 1} grupos.");
    }

    private static void Probar(ref int numero, string descripcion, Action accion) {
        Console.WriteLine($"{numero}. {descripcion}");

        try {
            accion();
            Console.WriteLine("   OK\n");
        } catch (Exception ex) {
            Console.WriteLine($"   FAIL: {ex.Message}\n");
            throw;
        }

        numero++;
    }

    private static void AfirmarEvaluacion(string expresion, int x, int esperado) {
        int resultado = Evaluar(expresion, x);

        Afirmar(
            resultado == esperado,
            $"La expresión '{expresion}' con x = {x} debería dar {esperado}, pero dio {resultado}."
        );
    }

    private static void AfirmarExcepcion(Action accion, string mensajeEsperado, string descripcion) {
        try {
            accion();
        } catch (Exception ex) {
            Afirmar(
                ex.Message.Contains(mensajeEsperado, StringComparison.OrdinalIgnoreCase),
                $"La prueba '{descripcion}' esperaba un mensaje que contuviera '{mensajeEsperado}', pero recibió '{ex.Message}'."
            );
            return;
        }

        throw new InvalidOperationException(
            $"La prueba '{descripcion}' esperaba una excepción con el mensaje '{mensajeEsperado}'."
        );
    }

    private static int Evaluar(string expresion, int x) {
        var comp = new Compilador();
        var ast = comp.Parsear(expresion);
        return ast.Evaluar(x);
    }

    private static Nodo Parsear(string expresion) {
        var comp = new Compilador();
        return comp.Parsear(expresion);
    }

    private static void Afirmar(bool condicion, string mensaje) {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }
}
