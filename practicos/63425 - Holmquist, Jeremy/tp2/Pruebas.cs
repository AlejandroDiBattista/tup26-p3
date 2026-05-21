using System;
using System.Collections.Generic;

public class Pruebas
{
    public static void EjecutarPruebas()
    {
        Console.WriteLine("=== Ejecutando Pruebas Automáticas ===\n");

        int pasadas = 0;
        int totales = 0;

        var casos = ObtenerCasosPrueba();

        foreach (var caso in casos)
        {
            totales++;
            bool paso = EjecutarCaso(caso);
            if (paso) pasadas++;
            Console.WriteLine();
        }

        Console.WriteLine("\n╔════════════════════════════╗");
        Console.WriteLine($"║ Pasadas: {pasadas}/{totales} de pruebas            ║");
        Console.WriteLine("╚════════════════════════════╝");

        if (pasadas == totales)
        {
            Console.WriteLine("✓ Todas las pruebas pasaron");
            Environment.Exit(0);
        }
        else
        {
            Console.WriteLine($"✗ {totales - pasadas} prueba(s) fallaron");
            Environment.Exit(1);
        }
    }

    private static bool EjecutarCaso(CasoPrueba caso)
    {
        try
        {
            var compilador = new Compilador(caso.Expresion);
            Nodo ast = compilador.Parsear();
            int resultado = ast.Evaluar(caso.ValorX);

            if (resultado == caso.ResultadoEsperado)
            {
                Console.WriteLine($"✓ {caso.Descripcion}");
                Console.WriteLine($"  Expresión: '{caso.Expresion}' con x={caso.ValorX}");
                Console.WriteLine($"  Resultado: {resultado}");
                return true;
            }
            else
            {
                Console.WriteLine($"✗ {caso.Descripcion}");
                Console.WriteLine($"  Expresión: '{caso.Expresion}' con x={caso.ValorX}");
                Console.WriteLine($"  Resultado: {resultado} (esperado: {caso.ResultadoEsperado})");
                return false;
            }
        }
        catch (Exception ex)
        {
            if (caso.DebefallarCon != null)
            {
                bool esErrorEsperado = ex.Message.Contains(caso.DebefallarCon);
                if (esErrorEsperado)
                {
                    Console.WriteLine($"✓ {caso.Descripcion} (Error esperado)");
                    Console.WriteLine($"  Expresión: '{caso.Expresion}'");
                    Console.WriteLine($"  Error: {ex.Message}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ {caso.Descripcion} (Error diferente)");
                    Console.WriteLine($"  Esperado: {caso.DebefallarCon}");
                    Console.WriteLine($"  Obtuvimos: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"✗ {caso.Descripcion} (Excepción inesperada)");
                Console.WriteLine($"  Expresión: '{caso.Expresion}'");
                Console.WriteLine($"  Error: {ex.Message}");
                return false;
            }
        }
    }

    private static List<CasoPrueba> ObtenerCasosPrueba()
    {
        return new List<CasoPrueba>
        {
            new CasoPrueba
            {
                Descripcion = "Suma simple",
                Expresion = "1+2",
                ValorX = 0,
                ResultadoEsperado = 3
            },
            new CasoPrueba
            {
                Descripcion = "Resta simple",
                Expresion = "10-3",
                ValorX = 0,
                ResultadoEsperado = 7
            },
            new CasoPrueba
            {
                Descripcion = "Multiplicación simple",
                Expresion = "3*4",
                ValorX = 0,
                ResultadoEsperado = 12
            },
            new CasoPrueba
            {
                Descripcion = "División entera",
                Expresion = "10/2",
                ValorX = 0,
                ResultadoEsperado = 5
            },

            new CasoPrueba
            {
                Descripcion = "Precedencia: 1+2*3",
                Expresion = "1+2*3",
                ValorX = 0,
                ResultadoEsperado = 7
            },
            new CasoPrueba
            {
                Descripcion = "Precedencia: 2*3+1",
                Expresion = "2*3+1",
                ValorX = 0,
                ResultadoEsperado = 7
            },
            new CasoPrueba
            {
                Descripcion = "Precedencia: 10-2*3",
                Expresion = "10-2*3",
                ValorX = 0,
                ResultadoEsperado = 4
            },

            new CasoPrueba
            {
                Descripcion = "Paréntesis: (1+2)*3",
                Expresion = "(1+2)*3",
                ValorX = 0,
                ResultadoEsperado = 9
            },
            new CasoPrueba
            {
                Descripcion = "Paréntesis anidados",
                Expresion = "((2+3)*4)",
                ValorX = 0,
                ResultadoEsperado = 20
            },

            new CasoPrueba
            {
                Descripcion = "Negación simple",
                Expresion = "-5",
                ValorX = 0,
                ResultadoEsperado = -5
            },
            new CasoPrueba
            {
                Descripcion = "Negación de suma",
                Expresion = "-(3+2)",
                ValorX = 0,
                ResultadoEsperado = -5
            },
            new CasoPrueba
            {
                Descripcion = "Positivo simple",
                Expresion = "+5",
                ValorX = 0,
                ResultadoEsperado = 5
            },
            new CasoPrueba
            {
                Descripcion = "Negación doble",
                Expresion = "--5",
                ValorX = 0,
                ResultadoEsperado = 5
            },

            new CasoPrueba
            {
                Descripcion = "Variable x sola",
                Expresion = "x",
                ValorX = 10,
                ResultadoEsperado = 10
            },
            new CasoPrueba
            {
                Descripcion = "Suma con x: 1+2*x",
                Expresion = "1+2*x",
                ValorX = 10,
                ResultadoEsperado = 21
            },
            new CasoPrueba
            {
                Descripcion = "Resta con x: x-1",
                Expresion = "x-1",
                ValorX = 5,
                ResultadoEsperado = 4
            },
            new CasoPrueba
            {
                Descripcion = "Multiplicación con x: x*2",
                Expresion = "x*2",
                ValorX = 5,
                ResultadoEsperado = 10
            },
            new CasoPrueba
            {
                Descripcion = "División con x: x/2",
                Expresion = "x/2",
                ValorX = 10,
                ResultadoEsperado = 5
            },
            new CasoPrueba
            {
                Descripcion = "x después de operador: 2*x",
                Expresion = "2*x",
                ValorX = 5,
                ResultadoEsperado = 10
            },
            new CasoPrueba
            {
                Descripcion = "x con negación unaria: -x",
                Expresion = "-x",
                ValorX = 5,
                ResultadoEsperado = -5
            },

            new CasoPrueba
            {
                Descripcion = "Expresión compleja 1: (x-1)*2",
                Expresion = "(x-1)*2",
                ValorX = 10,
                ResultadoEsperado = 18
            },
            new CasoPrueba
            {
                Descripcion = "Expresión compleja 2",
                Expresion = "(x-1)*(x-8/4)+3",
                ValorX = 5,
                ResultadoEsperado = 15
            },
            new CasoPrueba
            {
                Descripcion = "Expresión compleja 3",
                Expresion = "(x-1)*(x-8/4)+3",
                ValorX = 10,
                ResultadoEsperado = 75
            },

            new CasoPrueba
            {
                Descripcion = "Paréntesis sin cerrar",
                Expresion = "(1+2",
                ValorX = 0,
                DebefallarCon = "Paréntesis sin cerrar"
            },
            new CasoPrueba
            {
                Descripcion = "División por cero",
                Expresion = "1/0",
                ValorX = 0,
                DebefallarCon = "División por cero"
            }
        };
    }

    private class CasoPrueba
    {
        public string Descripcion { get; set; }
        public string Expresion { get; set; }
        public int ValorX { get; set; }
        public int ResultadoEsperado { get; set; }
        public string DebefallarCon { get; set; }
    }
}