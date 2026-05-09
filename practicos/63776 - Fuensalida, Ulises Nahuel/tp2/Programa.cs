using System;

namespace CalculadoraUTN
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Comandos.ModoInteractivo();
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "--help":
                    case "-h":
                        Comandos.MostrarAyuda();
                        break;
                    case "--test":
                    case "-t":
                    case "--probar":
                    case "-p":
                        Pruebas.EjecutarPruebas();
                        break;
                    default:
                        if (args.Length == 2)
                        {
                            Comandos.ModoDirecto(args[0], args[1]);
                        }
                        else
                        {
                            Console.WriteLine("Error: Argumentos insuficientes. Use --help para ayuda.");
                        }
                        break;
                }
            }
        }
    }
}

