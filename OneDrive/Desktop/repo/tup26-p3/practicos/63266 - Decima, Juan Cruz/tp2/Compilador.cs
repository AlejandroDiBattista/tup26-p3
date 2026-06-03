class Compilador {

    private readonly string _expresion;
    private int _pos;


    public static Nodo Parse(string expresion) {
        var compilador = new Compilador(expresion);
        var nodo = compilador.ParseExpresion();

        compilador.SaltarEspacios();
        if (compilador._pos < compilador._expresion.Length) {
            throw new FormatException(
                $"Token inesperado: '{compilador._expresion[compilador._pos]}'");
        }

        return nodo;
    }

    private Compilador(string expresion) {
        _expresion = expresion;
    }

    private Nodo ParseExpresion() {
        SaltarEspacios();

        if (Fin()) {
            throw new FormatException("Token inesperado: entrada vacía.");
        }

        Nodo izquierda = ParseTermino();

        while (!Fin()) {
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


    private Nodo ParseTermino() {
        Nodo izquierda = ParseFactor();

        while (!Fin()) {
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

    private Nodo ParseFactor() {
        SaltarEspacios();

        if (Fin()) {
            throw new FormatException(
                "Token inesperado: se esperaba un factor pero se llegó al final de la expresión.");
        }

        char c = Actual();

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
            Nodo nodo = ParseExpresion();

            SaltarEspacios();
            if (Fin() || Actual() != ')') {
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
        int inicio = _pos;

        while (!Fin() && char.IsDigit(Actual())) {
            _pos++;
        }

        int numero = int.Parse(_expresion[inicio.._pos]);
        return new NumeroNodo(numero);
    }

    private void SaltarEspacios() {
        while (!Fin() && char.IsWhiteSpace(Actual())) {
            _pos++;
        }
    }

    private bool Fin() {
        return _pos >= _expresion.Length;
    }

    private char Actual() {
        return _expresion[_pos];
    }
}


