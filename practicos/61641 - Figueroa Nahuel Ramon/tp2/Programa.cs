using System;

namespace CalculadoraAST
{
    class Programa
    {
        static void Main(string[] args)
        {
            // 1. Manejo de banderas globales
            if (args.Length > 0)
            {
                string arg0 = args[0].ToLower();
                if (arg0 == "--help" || arg0 == "-h")
                {
                    Comandos.MostrarAyuda();
                    return;
                }
                if (arg0 == "--test" || arg0 == "-t" || arg0 == "--probar" || arg0 == "-p")
                {
                    Pruebas.Ejecutar();
                    return;
                }
            }

            // 2. Modo Directo (2 argumentos posicionales)
            if (args.Length == 2)
            {
                string expresion = args[0];
                if (!int.TryParse(args[1], out int valorX))
                {
                    Console.WriteLine("Error: El valor asignado a 'x' debe ser un número entero válido.");
                    return;
                }

                try
                {
                    Compilador comp = new Compilador(expresion);
                    Nodo ast = comp.Parsear();
                    Console.WriteLine(ast.Evaluar(valorX));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return;
            }

            // 3. Modo Interactivo (Sin argumentos)
            if (args.Length == 0)
            {
                Console.Write("Ingrese expresión: ");
                string expresion = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(expresion) || expresion.Trim().ToLower() == "fin")
                    return;

                try
                {
                    Compilador comp = new Compilador(expresion);
                    Nodo ast = comp.Parsear(); // Compila una sola vez

                    while (true)
                    {
                        Console.Write("Ingrese valor de x: ");
                        string inputX = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(inputX) || inputX.Trim().ToLower() == "fin")
                            break;

                        if (int.TryParse(inputX, out int valorX))
                        {
                            try
                            {
                                Console.WriteLine(ast.Evaluar(valorX));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error: Valor de x inválido.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return;
            }

            // Si se ingresó una cantidad incorrecta de argumentos
            Console.WriteLine("Argumentos inválidos. Use --help para ver las opciones.");
        }
    }
}