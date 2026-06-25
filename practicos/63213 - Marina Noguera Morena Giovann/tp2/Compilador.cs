namespace TP2.Calculadora;

public class Compilador {
    private readonly string _entrada;
    private int _pos;
    private char Actual => _pos < _entrada.Length ? _entrada[_pos] : '\0';

    public Compilador(string entrada) {
        _entrada = entrada.Replace(" ", "");
        _pos = 0;
    }

    public static Nodo Parse(string entrada) => new Compilador(entrada).ParsearExpresion();

    private Nodo ParsearExpresion() {
        var nodoIzq = ParsearTermino();
        while (Actual == '+' || Actual == '-') {
            char op = Actual;
            _pos++;
            var nodoDer = ParsearTermino();
            nodoIzq = op == '+' ? new SumaNodo(nodoIzq, nodoDer) : new RestaNodo(nodoIzq, nodoDer);
        }
        return nodoIzq;
    }

    private Nodo ParsearTermino() {
        var nodoIzq = ParsearFactor();
        while (Actual == '*' || Actual == '/') {
            char op = Actual;
            _pos++;
            var nodoDer = ParsearFactor();
            nodoIzq = op == '*' ? new MultiplicacionNodo(nodoIzq, nodoDer) : new DivisionNodo(nodoIzq, nodoDer);
        }
        return nodoIzq;
    }

    private Nodo ParsearFactor() {
        if (Actual == '-') { _pos++; return new NegativoNodo(ParsearFactor()); }
        if (Actual == '+') { _pos++; return ParsearFactor(); }

        if (Actual == '(') {
            _pos++;
            var nodo = ParsearExpresion();
            if (Actual != ')') throw new FormatException("Se esperaba ')'");
            _pos++;
            return nodo;
        }

        if (char.IsDigit(Actual)) {
            string n = "";
            while (char.IsDigit(Actual)) n += _entrada[_pos++];
            return new NumeroNodo(int.Parse(n));
        }

        if (Actual == 'x' || Actual == 'X') {
            _pos++;
            return new VariableNodo();
        }

        throw new FormatException($"Token inesperado: {Actual}");
    }
}
