using System;

class Program
{
    static void Main(string[] argumentos)
    {
        if (argumentos.Length > 0)
        {
            if (argumentos.Length > 0 && (argumentos[0] == "--help" || argumentos[0] == "-h"))
            {
                Console.WriteLine("Operaciones disponibles:");
                Console.WriteLine("Suma: +");
                Console.WriteLine("Resta: -");
                Console.WriteLine("Multiplicación: *");
                Console.WriteLine("División entera: /");
                Console.WriteLine("Módulo: %");
                Console.WriteLine("Paréntesis: ()");
                Console.WriteLine();
                Console.WriteLine("Modo interactivo:");
                Console.WriteLine("Ingresar las operaciones manualmente");
                Console.WriteLine("Escriba \"salir\" para terminar");
                return;
            }

            