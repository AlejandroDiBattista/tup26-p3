using System;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            if (Comandos.EsAyuda(args))
            {
                Comandos.MostrarAyuda();
                return;
            }

            if (Comandos.EsTest(args))
            {
                Pruebas.Ejecutar();
                return;
            }

            if (args.Length == 2)
            {
                var nodo = Compilador.Compilar(args[0]);
                int x = int.Parse(args[1]);
                Console.WriteLine(nodo.Evaluar(x));
            }
            else
            {
                Console.Write("Expresión: ");
                string expr = Console.ReadLine();
                var nodo = Compilador.Compilar(expr);

                while (true)
                {
                    Console.Write("x = ");
                    string entrada = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(entrada) || entrada.ToLower() == "fin")
                        break;

                    if (!int.TryParse(entrada, out int x))
                    {
                        Console.WriteLine("Valor inválido");
                        continue;
                    }

                    Console.WriteLine(nodo.Evaluar(x));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
