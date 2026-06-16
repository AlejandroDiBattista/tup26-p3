cusing System;

namespace Calculadora
{
    static class Pruebas
    {
        public static void Ejecutar()
        {
            Test("1 + 2 * 3", 0, 7);
            Test("1 + 2 * x", 10, 21);
            Test("(x - 1) * (x - 8 / 4) + 3", 10, 75);
            Test("-(3 + 2)", 0, -5);
            Test("10 / 2", 0, 5);

            Console.WriteLine("Todas las pruebas pasaron correctamente.");
        }

        private static void Test(string expr, int x, int esperado)
        {
            Nodo nodo = new Compilador(expr).Parsear();
            int obtenido = nodo.Evaluar(x);

            if (obtenido != esperado)
            {
                throw new Exception(
                    $"Fallo: {expr} => {obtenido}, esperado {esperado}");
            }
        }
    }
}