using System;

class Compilador {

    private string _texto;
    private int _pos;

    public static Nodo Parse(string expresion) {
        var comp = new Compilador(expresion);
        var nodo = comp.Expresion();

        if (comp._pos < comp._texto.Length)
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private Compilador(string texto) {
        _texto = texto.Replace(" ", "");

        if (string.IsNullOrWhiteSpace(_texto))
            throw new FormatException("Token inesperado");

        _pos = 0;
    }

    private Nodo Expresion() {
        Nodo nodo = Termino();

        while (Actual == '+' || Actual == '-') {
            char op = Actual;
            Avanzar();
            Nodo derecho = Termino();

            nodo = op == '+'
                ? new SumaNodo(nodo, derecho)
                : new RestaNodo(nodo, derecho);
        }

        return nodo;
    }

    private Nodo Termino() {
        Nodo nodo = Factor();

        while (Actual == '*' || Actual == '/') {
            char op = Actual;
            Avanzar();
            Nodo derecho = Factor();

            nodo = op == '*'
                ? new MultiplicacionNodo(nodo, derecho)
                : new DivisionNodo(nodo, derecho);
        }

        return nodo;
    }

    private Nodo Factor() {

        if (Actual == '+') {
            Avanzar();
            return Factor();
        }

        if (Actual == '-') {
            Avanzar();
            return new NegativoNodo(Factor());
        }

        if (Actual == '(') {
            Avanzar();
            Nodo nodo = Expresion();

            if (Actual != ')')
                throw new FormatException("Se esperaba ')'");

            Avanzar();
            return nodo;
        }

        if (char.IsDigit(Actual)) {
            int inicio = _pos;

            while (char.IsDigit(Actual)) {
                Avanzar();
            }

            int valor = int.Parse(_texto.Substring(inicio, _pos - inicio));
            return new NumeroNodo(valor);
        }

        if (Actual == 'x' || Actual == 'X') {
            Avanzar();
            return new VariableNodo();
        }

        throw new FormatException("Token inesperado");
    }

    private char Actual => _pos < _texto.Length ? _texto[_pos] : '\0';

    private void Avanzar() {
        _pos++;
    }
}
