class Compilador
{

    private readonly string _expresion;
    private int _pos;


    public static Nodo Parse(string expresion)
    {
        var compilador = new Compilador(expresion);
        var nodo = compilador.ParseExpresion();

        compilador.SaltarEspacios();
        if (compilador._pos < compilador._expresion.Length)
        {
            throw new FormatException(
                $"Token inesperado: '{compilador._expresion[compilador._pos]}'");
        }

        return nodo;
    }

    private Compilador(string expresion)
    {
        _expresion = expresion;
    }

    private Nodo ParseExpresion()
    {
        SaltarEspacios();

        if (Fin())
        {
            throw new FormatException("Token inesperado: entrada vacía.");
        }

        Nodo izquierda = ParseTermino();

        while (!Fin())
        {
            SaltarEspacios();
            if (Fin()) break;

            char op = Actual();
            if (op != '+' && op != '-') break;

            _pos++;
            Nodo derecha = ParseTermino();

            izquierda = op == '+'
                ? new SumaNodo(izquierda, derecha)
                : new RestaNodo(izquierda, derecha);
        }

        return izquierda;
    }


    private Nodo ParseTermino()

    {
        Nodo izquierda = ParseFactor();

        while (!Fin())
        {
            SaltarEspacios();
            if (Fin()) break;

            char op = Actual();
            if (op != '*' && op != '/') break;

            _pos++;
            Nodo derecha = ParseFactor();

            izquierda = op == '*'
                ? new MultiplicacionNodo(izquierda, derecha)
                : new DivisionNodo(izquierda, derecha);
        }

        return izquierda;
    }

    
}


