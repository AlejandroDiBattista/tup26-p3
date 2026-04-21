namespace TP2.Calculadora;

public class Compilador
{
    private readonly string _entrada;
    private int _pos;

    private char Actual => _pos < _entrada.Length ? _entrada[_pos] : '\0';

    public Compilador(string entrada)
    {
        // Limpia espacios para evitar error
        _entrada = entrada.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
        _pos = 0;
    }

    public static Nodo Parse(string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) 
            throw new FormatException("Token inesperado");
            
        var instancia = new Compilador(entrada);
        var arbol = instancia.ParsearExpresion();

        if (instancia.Actual != '\0')
            throw new FormatException("Token inesperado");
        return arbol;
    }

    private Nodo ParsearExpresion()
    {
        var nodoIzq = ParsearTermino();
        while (Actual == '+' || Actual == '-')
        {
            char op = Actual;
            _pos++;
            var nodoDer = ParsearTermino();
            nodoIzq = op == '+' ? new SumaNodo(nodoIzq, nodoDer) : new RestaNodo(nodoIzq, nodoDer);
        }
        return nodoIzq;
    }

    private Nodo ParsearTermino()
    {
        var nodoIzq = ParsearFactor();
        while (Actual == '*' || Actual == '/')
        {
            char op = Actual;
            _pos++;
            var nodoDer = ParsearFactor();
            nodoIzq = op == '*' ? new MultiplicacionNodo(nodoIzq, nodoDer) : new DivisionNodo(nodoIzq, nodoDer);
        }
        return nodoIzq;
    }

    private Nodo ParsearFactor()
    {
        if (Actual == '-')
        { 
            _pos++; return new NegativoNodo(ParsearFactor());
        }

        if (Actual == '+')
        { 
            _pos++;
            return ParsearFactor();
        }

        if (Actual == '(')
        {
            _pos++;
            var nodo = ParsearExpresion();
            if (Actual != ')')
            throw new FormatException("Se esperaba ')'");
            _pos++;
            return nodo;
        }

        if (char.IsDigit(Actual))
        {
            string numStr = "";
            while (char.IsDigit(Actual)) { numStr += Actual; _pos++; }
            return new NumeroNodo(int.Parse(numStr));
        }

        if (Actual == 'x' || Actual == 'X')
        {
            _pos++;
            return new VariableNodo();
        }

        throw new FormatException("Token inesperado");
    }
}