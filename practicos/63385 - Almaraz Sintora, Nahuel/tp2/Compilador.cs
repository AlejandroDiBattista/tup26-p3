class Compilador {
    private string _expresion = "";
    private int _pos = 0;
    public static Nodo Parse(string expresion) {
        var compilador = new Compilador(expresion);
        var nodo = compilador.ParseExpresion();
        compilador.SaltarEspacios();
        if (compilador._pos < compilador._expresion.Length) {
            throw new FormatException($"Token inesperado: '{compilador._expresion[compilador._pos]}'");
        }
        return nodo;
    }
    private Compilador(string expresion) {
        _expresion = expresion;
    }
    private Nodo ParseExpresion() {
        SaltarEspacios();

        if (_pos >= _expresion.Length) {
            throw new FormatException("Token inesperado: entrada vacía.");
        }

        var izquierda = ParseTermino();

        while (true) {
            SaltarEspacios();
            if (_pos >= _expresion.Length) break;

            var op = _expresion[_pos];
            if (op != '+' && op != '-') break;

            _pos++;
            var derecha = ParseTermino();

            izquierda = op == '+'
                ? new SumaNodo(izquierda, derecha)
                : new RestaNodo(izquierda, derecha);
        }

        return izquierda;
    }
     private Nodo ParseTermino() {
        var izquierda = ParseFactor();

        while (true) {
            SaltarEspacios();
            if (_pos >= _expresion.Length) break;

            var op = _expresion[_pos];
            if (op != '*' && op != '/') break;

            _pos++;
            var derecha = ParseFactor();

            izquierda = op == '*'
                ? new MultiplicacionNodo(izquierda, derecha)
                : new DivisionNodo(izquierda, derecha);
        }

        return izquierda;
    }
     private Nodo ParseFactor() {
        SaltarEspacios();

        if (_pos >= _expresion.Length) {
            throw new FormatException("Token inesperado: se esperaba un factor pero se llegó al final de la expresión.");
        }

        var c = _expresion[_pos];

        if (c == '+') {
            _pos++;
            return ParseFactor();
        }

        if (c == '-') {
            _pos++;
            return new NegativoNodo(ParseFactor());
        }

        if (c == '(') {
            _pos++; 
            var nodo = ParseExpresion();
            SaltarEspacios();

            if (_pos >= _expresion.Length || _expresion[_pos] != ')') {
                throw new FormatException("Se esperaba ')'");
            }

            _pos++; 
            return nodo;
        }

        if (c == 'x' || c == 'X') {
            _pos++;
            return new VariableNodo();
        }

        if (char.IsDigit(c)) {
            return ParseNumero();
        }

        throw new FormatException($"Token inesperado: '{c}'");
    }
    private Nodo ParseNumero() {
        var inicio = _pos;
        while (_pos < _expresion.Length && char.IsDigit(_expresion[_pos])) {
            _pos++;
        }
        var numero = int.Parse(_expresion[inicio.._pos]);
        return new NumeroNodo(numero);
    }
    private void SaltarEspacios() {
        while (_pos < _expresion.Length && char.IsWhiteSpace(_expresion[_pos])) {
            _pos++;
        }
    }
}
