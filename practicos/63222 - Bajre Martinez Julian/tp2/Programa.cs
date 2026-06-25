using System;
using System.Collections.Generic;
namespace TP2.Calculadora;

public class Programa {
    public static void Main(string[] args) {
        Console.Title = "TP2 - Calculadora";
        Console.WriteLine("=== Calculadora de Expresiones Aritméticas ===");

        if (args.Length == 0) {
            EjecutarModoInteractivo();
        } else {
            if (!Comandos.Procesar(args))
                Console.WriteLine("Argumentos inválidos. Usá --help para ver las opciones.");
        }
    }

    private static void EjecutarModoInteractivo() {

        Console.WriteLine("Modo interactivo. Ingresá 'fin' o dejá vacío para salir.\n");

        try {
            Console.Write("Expresión: ");
            string? entrada = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(entrada) || entrada == "fin")
                return;

            var compilador = new Compilador();
            Nodo arbol = compilador.Parsear(entrada);

            while (true) {
                Console.Write("Valor de x: ");
                string? valorEntrada = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(valorEntrada) || valorEntrada == "fin")
                    break;

                try {
                    if (!int.TryParse(valorEntrada, out int x))
                        throw new Exception($"Error: '{valorEntrada}' no es un valor entero válido para x.");

                    Console.WriteLine($"Resultado: {arbol.Evaluar(x)}");
                } catch (DivideByZeroException) {
                    Console.WriteLine("Error: división por cero.");
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        } catch (Exception e) {
            Console.WriteLine(e.Message);
        }

    }
}
