class Pruebas
{
    public static void Ejecutar()
    {
        var c = new Compilador();

        void Test(string expr, int x, int esperado)
        {
            var nodo = c.Parsear(expr);
            var res = nodo.Evaluar(x);

            if (res != esperado)
                throw new Exception($"Fallo: {expr} = {res}, esperado {esperado}");
        }

        Test("1+2*3", 0, 7);
        Test("1+2*x", 10, 21);
        Test("(x-1)*(x-8/4)+3", 10, 75);
        Test("-(3+2)", 0, -5);
        Test("10/2", 0, 5);

        Console.WriteLine("Todas las pruebas pasaron ✔");
    }
}