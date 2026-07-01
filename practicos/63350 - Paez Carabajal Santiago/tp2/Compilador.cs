using System;

class Compilador {
    private string _entrada = "";
    private int _pos;

    // Esta es la "puerta" que ahora SÍ tiene contenido
    public static Nodo Parse(string expresion) {
        var c = new Compilador();
        return c.ParsearInterno(expresion);
    }

    private Nodo ParsearInterno(string expresion) {
        _entrada = expresion.Replace(" ", "");
        _pos = 0;
        if (string.IsNullOrWhiteSpace(_entrada))
            throw new FormatException("Token inesperado: entrada vacía");

        var nodo = ParsearExpresion();

        if (_pos < _entrada.Length)
            throw new FormatException($"Token inesperado: {_entrada[_pos]}");

        return nodo;
    }

    private char Actual => _pos < _entrada.Length ? _entrada[_pos] : '\0';

    private Nodo ParsearExpresion() {
        var nodo = ParsearTermino();
        while (Actual == '+' || Actual == '-') {
            char op = Actual; _pos++;
            var der = ParsearTermino();
            nodo = (op == '+') ? new SumaNodo(nodo, der) : new RestaNodo(nodo, der);
        }
        return nodo;
    }

    private Nodo ParsearTermino() {
        var nodo = ParsearFactor();
        while (Actual == '*' || Actual == '/') {
            char op = Actual; _pos++;
            var der = ParsearFactor();
            nodo = (op == '*') ? new MultiplicacionNodo(nodo, der) : new DivisionNodo(nodo, der);
        }
        return nodo;
    }

    private Nodo ParsearFactor() {
        if (Actual == '+') { _pos++; return new PositivoNodo(ParsearFactor()); }
        if (Actual == '-') { _pos++; return new NegativoNodo(ParsearFactor()); }
        if (Actual == '(') {
            _pos++;
            var nodo = ParsearExpresion();
            if (Actual != ')') throw new FormatException("Se esperaba ')' (paréntesis sin cerrar)");
            _pos++;
            return nodo;
        }
        if (char.IsDigit(Actual)) {
            string num = "";
            while (char.IsDigit(Actual)) { num += Actual; _pos++; }
            return new NumeroNodo(int.Parse(num));
        }
        if (Actual == 'x' || Actual == 'X') { _pos++; return new VariableNodo(); }

        throw new FormatException($"Token inesperado: {Actual}");
    }
}

// Esto es para que no te tire error el IsWhiteSpace en Programa.cs
public static class StringExtensions {
    public static bool IsWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);
}
