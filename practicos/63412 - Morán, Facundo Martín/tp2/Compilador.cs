class Compilador {
    private readonly string _input;
    private int _cursor;
    private Compilador(string input) {
        _input = input;
        _cursor = 0;
    }
    private void IgnorarEspacios() {
        while (_cursor < _input.Length && char.IsWhiteSpace(_input[_cursor])) {
            _cursor++;
        }
    }
    public static Nodo Parse(string expresion) {
        var comp = new Compilador(expresion);
        Nodo arbol = comp.ParseExpresion();
        comp.IgnorarEspacios();
        return arbol;
    }
    private Nodo ParseNumero() {
        int inicio = _cursor;
        while (_cursor < _input.Length && char.IsDigit(_input[_cursor]))
            _cursor++;
        return new NumeroNodo(int.Parse(_input[inicio.._cursor]));
    }
    private Nodo ParseFactor() {
        IgnorarEspacios();
        char actual = _input[_cursor];

        if (actual == '-') { _cursor++; return new NegativoNodo(ParseFactor()); }
        if (actual == '+') { _cursor++; return ParseFactor(); }

        if (actual == '(') {
            _cursor++;
            Nodo nodo = ParseExpresion();
            IgnorarEspacios();
            _cursor++; // consume ')'
            return nodo;
        }

        if (actual == 'x' || actual == 'X') { _cursor++; return new VariableNodo(); }

        return ParseNumero();
    }
    private Nodo ParseTermino() {
        Nodo resultado = ParseFactor();

        IgnorarEspacios();
        while (_cursor < _input.Length && (_input[_cursor] == '*' || _input[_cursor] == '/')) {
            char op = _input[_cursor++];
            Nodo derecha = ParseFactor();
            resultado = op == '*'
                ? new MultiplicacionNodo(resultado, derecha)
                : new DivisionNodo(resultado, derecha);
            IgnorarEspacios();
        }

        return resultado;
    }
    private Nodo ParseExpresion() {
        IgnorarEspacios();
        Nodo resultado = ParseTermino();

        IgnorarEspacios();
        while (_cursor < _input.Length && (_input[_cursor] == '+' || _input[_cursor] == '-')) {
            char op = _input[_cursor++];
            Nodo derecha = ParseTermino();
            resultado = op == '+'
                ? new SumaNodo(resultado, derecha)
                : new RestaNodo(resultado, derecha);
            IgnorarEspacios();
        }

        return resultado;
    }
}


