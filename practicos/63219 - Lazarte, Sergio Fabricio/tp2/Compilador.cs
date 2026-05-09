class Compilador
{
    private string texto = "";
    private int pos;

    public static Nodo Parse(string entrada)
    {
        var c = new Compilador();

        if (string.IsNullOrWhiteSpace(entrada))
            throw new FormatException("Token inesperado");

        c.texto = entrada;
        c.pos = 0;

        var nodo = c.ParseExpresion();

        c.SaltarEspacios();

        if (c.pos < c.texto.Length)
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private Nodo ParseExpresion()
    {
        Nodo nodo = ParseTermino();

        while (true)
        {
            SaltarEspacios();

            if (Match('+'))
                nodo = new SumaNodo(nodo, ParseTermino());
            else if (Match('-'))
                nodo = new RestaNodo(nodo, ParseTermino());
            else
                break;
        }

        return nodo;
    }

    private Nodo ParseTermino()
    {
        Nodo nodo = ParseFactor();

        while (true)
        {
            SaltarEspacios();

            if (Match('*'))
                nodo = new MultiplicacionNodo(nodo, ParseFactor());
            else if (Match('/'))
                nodo = new DivisionNodo(nodo, ParseFactor());
            else
                break;
        }

        return nodo;
    }

    private Nodo ParseFactor()
    {
        SaltarEspacios();

        if (Match('+'))
            return ParseFactor();

        if (Match('-'))
            return new NegativoNodo(ParseFactor());

        if (Match('('))
        {
            var nodo = ParseExpresion();

            if (!Match(')'))
                throw new FormatException("Se esperaba ')'");

            return nodo;
        }

        if (char.IsDigit(Peek()))
        return ParseNumero();

        if (Peek() == 'x' || Peek() == 'X')
        {
            pos++;
            return new VariableNodo();
        }

        throw new FormatException("Token inesperado");
    }

    private Nodo ParseNumero()
    {
        int inicio = pos;

        while (char.IsDigit(Peek()))
            pos++;

        return new NumeroNodo(int.Parse(texto.Substring(inicio, pos - inicio)));
    }

    private char Peek()
    {
        if (pos >= texto.Length) return '\0';
        return texto[pos];
    }

    private bool Match(char c)
    {
        if (Peek() == c)
        {
            pos++;
            return true;
        }
        return false;
    }

    private void SaltarEspacios()
    {
        while (pos < texto.Length && char.IsWhiteSpace(texto[pos]))
            pos++;
    }
}