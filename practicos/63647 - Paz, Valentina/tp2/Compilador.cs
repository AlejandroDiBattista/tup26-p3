public class Compilador
{
    private string _texto = "";
    private int _pos;

    public static Nodo Parse(string texto)
    {
        Compilador compilador = new Compilador();
        return compilador.Parsear(texto);
    }

    public Nodo Parsear(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new FormatException("Entrada vacía.");

        _texto = texto;
        _pos = 0;

        Nodo resultado = ParsearExpresion();

        SaltarEspacios();

        if (_pos < _texto.Length)
            throw new FormatException($"Token inesperado: '{_texto[_pos]}'");

        return resultado;
    }

    private Nodo ParsearExpresion()
    {
        Nodo nodo = ParsearTermino();

        SaltarEspacios();

        while (_pos < _texto.Length && (_texto[_pos] == '+' || _texto[_pos] == '-'))
        {
            char operador = _texto[_pos];
            _pos++;
            SaltarEspacios();

            Nodo derecho = ParsearTermino();

            if (operador == '+')
                nodo = new SumaNodo(nodo, derecho);
            else
                nodo = new RestaNodo(nodo, derecho);

            SaltarEspacios();
        }

        return nodo;
    }

    private Nodo ParsearTermino()
    {
        Nodo nodo = ParsearFactor();

        SaltarEspacios();

        while (_pos < _texto.Length && (_texto[_pos] == '*' || _texto[_pos] == '/'))
        {
            char operador = _texto[_pos];
            _pos++;
            SaltarEspacios();

            Nodo derecho = ParsearFactor();

            if (operador == '*')
                nodo = new MultiplicacionNodo(nodo, derecho);
            else
                nodo = new DivisionNodo(nodo, derecho);

            SaltarEspacios();
        }

        return nodo;
    }

    private Nodo ParsearFactor()
    {
        SaltarEspacios();

        if (Match('+'))
            return ParsearFactor();

        if (Match('-'))
            return new NegativoNodo(ParsearFactor());

        if (Match('('))
        {
            Nodo nodo = ParsearExpresion();
            SaltarEspacios();

            if (!Match(')'))
                throw new FormatException("Se esperaba ')'");

            return nodo;
        }

        char actual = Peek();

        if (actual == 'x' || actual == 'X')
        {
            _pos++;
            return new VariableNodo();
        }

        if (char.IsDigit(actual))
            return ParsearNumero();

        if (actual == '\0')
            throw new FormatException("Token inesperado: fin de entrada.");

        throw new FormatException($"Token inesperado: '{actual}'");
    }

    private Nodo ParsearNumero()
    {
        int inicio = _pos;

        while (char.IsDigit(Peek()))
            _pos++;

        string textoNumero = _texto.Substring(inicio, _pos - inicio);
        int numero = int.Parse(textoNumero);

        return new NumeroNodo(numero);
    }

    private void SaltarEspacios()
    {
        while (_pos < _texto.Length && char.IsWhiteSpace(_texto[_pos]))
            _pos++;
    }

    private bool Match(char c)
    {
        if (Peek() == c)
        {
            _pos++;
            return true;
        }

        return false;
    }

    private char Peek()
    {
        if (_pos >= _texto.Length)
            return '\0';

        return _texto[_pos];
    }
}