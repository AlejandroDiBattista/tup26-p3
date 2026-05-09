class Compilador {
    private string input = "";
    private int pos = 0;

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion)) throw new FormatException ("Token inesperado");

        var p = new Compilador
        {
            input = expresion,
            pos = 0
        };

        var nodo = p.ParseExpresion();

        p.SaltarEspacios();
        if (p.pos < p.input.Length) throw new FormatException("Token inesperado");

        return nodo;
    }

    private Nodo ParseExpresion()
{
    var nodo = ParseTermino();
    while (true)
    {
        SaltarEspacios();
        
        if(Match('+'))
        {
            nodo = new Suma(nodo, ParseTermino());
        }
        else if (Match('-'))
        {
            nodo = new Resta(nodo, ParseTermino());
        }
        else
        {
            break;
        }           
    }
    return nodo;
}

    private Nodo ParseTermino()
{
    var nodo = ParseFactor();
    while (true)
    {
        SaltarEspacios();
        if (Match('*'))
        {
            nodo = new Multiplicacion(nodo, ParseFactor());
        }
        else if (Match('/'))
        {
            nodo = new Division(nodo, ParseFactor());
        }
        else
        {
            break;
        }
    }
    return nodo;
}

    private Nodo ParseFactor()
{
    SaltarEspacios();

    if (Match('+')) return ParseFactor();
    if (Match('-')) return new Negativo(ParseFactor());

    if (Match('('))
    {
        var nodo = ParseExpresion();
        if (!Match(')')) throw new FormatException("Se esperaba ')'");
        return nodo;
    }

    if (char.IsDigit(Peek())) return ParseNumero();

    if (char.ToLower(Peek()) == 'x')
    {
        pos++;
        return new Variable();
    }

    throw new FormatException("Token inesperado");
}

    private char Peek() => pos < input.Length ? input[pos] : '\0';

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
    while (char.IsWhiteSpace(Peek())) pos++;
}

private Nodo ParseNumero()
{
    int inicio = pos;
    while (char.IsDigit(Peek())) pos++;
    var texto = input[inicio..pos];
    return new Numero(int.Parse(texto));
}
}

