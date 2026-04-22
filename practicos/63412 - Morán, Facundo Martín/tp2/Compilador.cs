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
        if (comp._cursor < comp._input.Length)
            throw new FormatException($"Token inesperado: '{comp._input[comp._cursor]}'");
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
        if (_cursor >= _input.Length)
            throw new FormatException("Token inesperado: se esperaba un factor pero se llegó al final de la expresión.");
        char actual = _input[_cursor];

        if (actual == '-') { _cursor++; return new NegativoNodo(ParseFactor()); }
        if (actual == '+') { _cursor++; return ParseFactor(); }

        if (actual == '(') {
            _cursor++;
            Nodo nodo = ParseExpresion();
            IgnorarEspacios();
            if (_cursor >= _input.Length || _input[_cursor] != ')')
                throw new FormatException("Se esperaba ')'");
            _cursor++;
            return nodo;
        }   

        if (actual == 'x' || actual == 'X') { _cursor++; return new VariableNodo(); }

        if (char.IsDigit(actual)) return ParseNumero();
throw new FormatException($"Token inesperado: '{actual}'");
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
        if (_cursor >= _input.Length)
            throw new FormatException("Token inesperado: entrada vacía.");
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


