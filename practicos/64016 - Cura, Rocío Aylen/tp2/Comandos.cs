class Comandos
{
    public static bool Procesar(string[] args)
    {
        if (args.Length == 2)
        {
            string expr = args[0];

            if (!int.TryParse(args[1], out int x))
            {
                Console.WriteLine("Valor de x inválido");
                return true;
            }

            var ast = Compilador.Parse(expr);
            Console.WriteLine(ast.Evaluar(x));
            return true;
        }
        return false;
    }
}