// Programa.cs
using System;

public class Programa {
    public static void Main(string[] args) {
        Comandos.Procesar(args);
    }

    public static void ModoDirecto(string expresion, int x) {
        try {
            Nodo ast = Compilador.Compilar(expresion);
            int resultado = ast.Evaluar(x);
            Console.WriteLine(resultado);
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            Environment.Exit(1); // Se sale con error según convenciones de consola
        }
    }

    public static void ModoInteractivo() {
        Console.WriteLine("=== Calculadora Interactiva ===");
        Console.WriteLine("Escriba 'fin' o deje vacío en cualquier momento para salir.\n");

        Console.Write("Ingrese la expresión matemática: ");
        string expresion = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(expresion) || expresion.Trim().ToLower() == "fin") return;

        Nodo ast;
        try {
            // El profesor pide que se compile la expresión UNA SOLA VEZ
            ast = Compilador.Compilar(expresion);
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            return;
        }

        while (true) {
            Console.Write("Ingrese un valor para 'x': ");
            string entrada = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(entrada) || entrada.Trim().ToLower() == "fin") {
                break;
            }

            if (int.TryParse(entrada, out int x)) {
                try {
                    int resultado = ast.Evaluar(x);
                    Console.WriteLine($"Resultado: {resultado}");
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message); // Atrapa errores como división por cero
                }
            } else {
                Console.WriteLine("Error: Valor de 'x' inválido. Debe ingresar un número entero.");
            }
        }
    }
}
