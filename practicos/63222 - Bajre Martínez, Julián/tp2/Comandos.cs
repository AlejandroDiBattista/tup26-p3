using System;
using System.Collections.Generic;
namespace TP2.Calculadora;

static class Comandos {
    public static bool Procesar(string[] args) 
    {
        switch (args)
        {
            case ["--help"] or ["-h"] or ["--ayuda"]:
                MostrarAyuda();
                return true;

            case ["--test"] or ["-t"] or ["--probar"] or ["--prueba"] or ["-p"]:
                Pruebas.Ejecutar();
                return true;

            case [var expresion, var valor]:
                EjecutarModoDirecto(expresion, valor);
                return true;

            default:
                return false;
        }
    }
    private static void EjecutarModoDirecto(string expresion, string valor)
    {
        int x = int.Parse(valor);
        var compilador = new Compilador();
        Nodo arbol = compilador.Parsear(expresion);
        Console.WriteLine(arbol.Evaluar(x));
    }

    private static void MostrarAyuda()
    {
        Console.WriteLine("""

Uso: dotnet run -- [opciones] [<expresión> <valor>]

    Analiza y evalúa expresiones matemáticas con la variable 'x'.
    Si se pasa una expresión y un valor, reemplaza 'x' y muestra el resultado.
    Sin argumentos, inicia el modo interactivo.

Expresiones válidas:
    Operadores: + - * /
    Variable:   x o X
    Ejemplo:    (x - 1) * (x - 8/4) + 3

Opciones:
    --help,  -h, --ayuda                 Muestra esta ayuda.
    --test,  -t, --probar, --prueba, -p  Ejecuta pruebas automáticas.

""");
        Environment.Exit(0);
    }
}

