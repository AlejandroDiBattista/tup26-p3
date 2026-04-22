using System;
using System.Collections.Generic;
namespace TP2.Calculadora;

public class Programa
{
    public static void Main(string[] args)
    {
        Console.Title = "TP2 - Calculadora";
        Console.WriteLine("=== Calculadora de Expresiones Aritméticas ===");

        if (args.Length == 0)
        {
            Console.WriteLine("Modo interactivo activo. Ingresá 'fin' para salir.");
        }
        else
        {
            Console.WriteLine($"Se recibieron {args.Length} argumento(s). Procesando...");
        }
    }
}