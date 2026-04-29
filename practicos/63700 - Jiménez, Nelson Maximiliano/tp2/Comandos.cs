class Comandos
{
    public bool Help;
    public bool Test;
    public string? Expresion;
    public string? Valor;

    public static Comandos Parse(string[] args)
    {
        var cmd = new Comandos();

        foreach (var a in args)
        {
            if (a == "--help" || a == "-h") cmd.Help = true;
            else if (a == "--test" || a == "-t" || a == "--probar") cmd.Test = true;
        }

        var pos = args.Where(a => !a.StartsWith("-")).ToArray();

        if (pos.Length > 0) cmd.Expresion = pos[0];
        if (pos.Length > 1) cmd.Valor = pos[1];

        return cmd;
    }
}
