class Compilador {
    private string _input;
    private int _pos;

    public Nodo Parsear(string input) {
        _input = input.Replace(" ", "");
        _pos = 0;

        if (_input.Length == 0)
            throw new Exception("Expresión vacía");

        var expr = Expresion();

        if (_pos < _input.Length)
            throw new Exception("Token inesperado");

        return expr;
    }

    char Actual() => _pos < _input.Length ? _input[_pos] : '\0';
    void Avanzar() => _pos++;

    Nodo Expresion() {
        var nodo = Termino();

        while (Actual() == '+' || Actual() == '-') {
            char op = Actual();
            Avanzar();
            var der = Termino();

            nodo = op == '+'
                ? new SumaNodo(nodo, der)
                : new RestaNodo(nodo, der);
        }

        return nodo;
    }

    Nodo Termino() {
        var nodo = Factor();

        while (Actual() == '*' || Actual() == '/') {
            char op = Actual();
            Avanzar();
            var der = Factor();

            nodo = op == '*'
                ? new MultiplicacionNodo(nodo, der)
                : new DivisionNodo(nodo, der);
        }

        return nodo;
    }

    Nodo Factor() {
        if (Actual() == '+') {
            Avanzar();
            return Factor();
        }

        if (Actual() == '-') {
            Avanzar();
            return new NegativoNodo(Factor());
        }

        if (Actual() == '(') {
            Avanzar();
            var expr = Expresion();

            if (Actual() != ')')
                throw new Exception("Paréntesis sin cerrar");

            Avanzar();
            return expr;
        }

        if (char.IsDigit(Actual())) {
            return Numero();
        }

        if (Actual() == 'x' || Actual() == 'X') {
            Avanzar();
            return new VariableNodo();
        }

        throw new Exception("Token inesperado");
    }

    Nodo Numero() {
        int inicio = _pos;

        while (char.IsDigit(Actual()))
            Avanzar();

        var texto = _input.Substring(inicio, _pos - inicio);
        return new NumeroNodo(int.Parse(texto));
    }
}
