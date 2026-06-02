// Pruebas.cs
using System;

public static class Pruebas {
    public static void Ejecutar() {
        Console.WriteLine("Ejecutando pruebas automáticas...\n");
        int pasadas = 0;
        int fallidas = 0;

        Probar("1 + 2 * 3", 0, 7, ref pasadas, ref fallidas);
        Probar("1 + 2 * x", 10, 21, ref pasadas, ref fallidas);
        Probar("(x - 1) * (x - 8 / 4) + 3", 10, 75, ref pasadas, ref fallidas);
        Probar("-(3 + 2)", 0, -5, ref pasadas, ref fallidas);
        Probar("10 / 2", 0, 5, ref pasadas, ref fallidas);

        // Prueba de error
        ProbarError("(1 + 2", 0, ref pasadas, ref fallidas);

        Console.WriteLine($"\nResultados: {pasadas} pasaron, {fallidas} fallaron.");
        if (fallidas > 0) Environment.Exit(1);
    }

    private static void Probar(string expr, int x, int esperado, ref int pasadas, ref int fallidas) {
        try {
            Nodo ast = Compilador.Compilar(expr);
            int resultado = ast.Evaluar(x);
            if (resultado == esperado) {
                Console.WriteLine($"[EXITO] {expr} (x={x}) == {esperado}");
                pasadas++;
            } else {
                Console.WriteLine($"[FALLO] {expr} (x={x}) -> Esperado: {esperado}, Obtenido: {resultado}");
                fallidas++;
            }
        } catch (Exception ex) {
            Console.WriteLine($"[FALLO] {expr} (x={x}) -> Excepción inesperada: {ex.Message}");
            fallidas++;
        }
    }

    private static void ProbarError(string expr, int x, ref int pasadas, ref int fallidas) {
        try {
            Nodo ast = Compilador.Compilar(expr);
            ast.Evaluar(x);
            Console.WriteLine($"[FALLO] {expr} -> Se esperaba un error pero evaluó correctamente.");
            fallidas++;
        } catch (Exception) {
            Console.WriteLine($"[EXITO] {expr} -> Error de parsing detectado correctamente.");
            pasadas++;
        }
    }
}
