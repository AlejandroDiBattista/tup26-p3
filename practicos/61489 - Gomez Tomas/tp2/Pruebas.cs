namespace Calculadora;

static class Pruebas {
    public static void Ejecutar() {
        var tests = new (string exp, int x, int expected)[] {
            ("1 + 2 * 3", 0, 7),
            ("1 + 2 * x", 10, 21),
            ("(x - 1) * (x - 8 / 4) + 3", 10, 75),
            ("-(3 + 2)", 0, -5)
        };

        foreach (var t in tests) {
            try {
                var res = Compilador.Parse(t.exp).Evaluar(t.x);
                Console.WriteLine($"{t.exp} (x={t.x}) => {res} | {(res == t.expected ? "PASÓ" : "FALLÓ")}");
            } catch (Exception e) {
                Console.WriteLine($"{t.exp} => ERROR: {e.Message}");
            }
        }
    }
}