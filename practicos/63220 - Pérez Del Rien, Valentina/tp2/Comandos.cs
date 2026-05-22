using System;

class Comandos
{
    public bool EsHelp { get; private set; }
    public bool EsTest { get; private set; }
    public string? Expresion { get; private set; }
    public int? ValorX { get; private set; }

    public Comandos(string[] args)
    {
        if (args.Length == 0)
            return;

        if (args[0] == "--help" || args[0] == "-h")
        {
            EsHelp = true;
            return;
        }

        if (args[0] == "--test" || args[0] == "-t")
        {
            EsTest = true;
            return;
        }

        if (args.Length == 2)
        {
            if (string.IsNullOrWhiteSpace(args[0]))
                throw new Exception("Entrada vacía");

            Expresion = args[0];

            if (!int.TryParse(args[1], out int x))
                throw new Exception("Valor de x inválido");

            ValorX = x;
            return;
        }

        throw new Exception("Entrada vacía");
    }
}