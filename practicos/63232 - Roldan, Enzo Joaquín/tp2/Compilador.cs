class Compilador {
    private string _texto = "";
    private int    _pos   = 0;

    private char Actual => _pos < _texto.Length ? _texto[_pos] : '\0';

    private void SkipEspacios() {
        while (_pos < _texto.Length && char.IsWhiteSpace(Actual)) _pos++;
    }

    private Nodo Expresion() {
        var nodo = Termino();
        SkipEspacios();
        while (Actual is '+' or '-') {
            var op = Actual; _pos++;
            var der = Termino();
            nodo = op == '+' ? new SumaNodo(nodo, der) : new RestaNodo(nodo, der);
            SkipEspacios();
        }
        return nodo;
    }

    private Nodo Termino() {
        var nodo = Factor();
        SkipEspacios();
        while (Actual is '*' or '/') {
            var op = Actual; _pos++;
            var der = Factor();
            nodo = op == '*' ? new MultiplicacionNodo(nodo, der) : new DivisionNodo(nodo, der);
            SkipEspacios();
        }
        return nodo;
    }

    private Nodo Factor() {
        SkipEspacios();
        if (Actual == '+') { _pos++; return Factor(); }
        if (Actual == '-') { _pos++; return new NegativoNodo(Factor()); }
        if (Actual == '(') {
            _pos++;
            var nodo = Expresion();
            SkipEspacios();
            if (Actual != ')') throw new FormatException("Se esperaba ')'");
            _pos++;
            return nodo;
        }
        if (char.IsDigit(Actual)) {
            var inicio = _pos;
            while (char.IsDigit(Actual)) _pos++;
            return new NumeroNodo(int.Parse(_texto[inicio.._pos]));
        }
        if (Actual is 'x' or 'X') { _pos++; return new VariableNodo(); }

        throw new FormatException($"Token inesperado '{Actual}'");
    }

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new FormatException("Token inesperado: entrada vacía.");

        var compilador = new Compilador { _texto = expresion, _pos = 0 };
        var nodo = compilador.Expresion();
        compilador.SkipEspacios();
        if (compilador._pos < expresion.Length)
            throw new FormatException($"Token inesperado '{expresion[compilador._pos]}'");
        return nodo;
    }
}
