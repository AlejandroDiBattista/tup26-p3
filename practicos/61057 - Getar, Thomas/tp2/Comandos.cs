using System;

namespace Calculadora
{
    enum Modo
    {
        Interactivo,
        Directo,
        Ayuda,
        Pruebas,
        Error
    }

    class Comandos
    {
        public Modo Modo { get; private set; }
        public string Expresion { get; private set; } = "";
        public int ValorX { get; private set; }

        public Comandos(string[] args)
        {
            Procesar(args);
        }

        private void Procesar(string[] args)
        {
            if (args.Length == 0)
            {
                Modo = Modo.Interactivo;
                return;
            }

            string opcion = args[0].ToLower();

            if (opcion == "--help" || opcion == "-h")
            {
                Modo = Modo.Ayuda;
                return;
            }

            if (opcion == "--test" || opcion == "--probar" ||
                opcion == "-t" || opcion == "-p")
            {
                Modo = Modo.Pruebas;
                return;
            }

            if (args.Length == 2)
            {
                Expresion = args[0];

                if (!int.TryParse(args[1], out int x))
                {
                    Modo = Modo.Error;
                    return;
                }

                ValorX = x;
                Modo = Modo.Directo;
                return;
            }

            Modo = Modo.Error;
        }
    }
}