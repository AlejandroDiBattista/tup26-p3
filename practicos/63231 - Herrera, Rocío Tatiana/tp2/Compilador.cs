class Compilador {
    public static Nodo Parse(string expresion) {
        throw new NotImplementedException("Implementar el parser para convertir la expresión en un AST.");
    }
}

public class Parser {
    private readonly string _cadena;
    private int _pos;


    private Parser(string expresion) {
        _cadena = expresion.Replace(" ", "");
        _pos = 0;
    }


    public static Expresion Analizar(string expresion) {
        var p = new Parser(expresion);
        var resultado = p.LeerExpresion();

        if (p._pos != p._cadena.Length)
            throw new FormatException("Caracter inesperado");

        return resultado;
    }

    private Expresion LeerExpresion() {
        Expresion nodo = LeerTermino();

        while (_pos < _cadena.Length &&
              (_cadena[_pos] == '+' || _cadena[_pos] == '-')) {
            char op = _cadena[_pos++];
            Expresion der = LeerTermino();

            nodo = op == '+'
                ? new Suma(nodo, der)
                : new Resta(nodo, der);
        }

        return nodo;
    }

    private Expresion LeerTermino() {
        Expresion nodo = LeerFactor();

        while (_pos < _cadena.Length &&
              (_cadena[_pos] == '*' || _cadena[_pos] == '/')) {
            char op = _cadena[_pos++];
            Expresion der = LeerFactor();

            nodo = op == '*'
                ? new Producto(nodo, der)
                : new Cociente(nodo, der);
        }

        return nodo;
    }

    private Expresion LeerFactor() {
        if (_pos >= _cadena.Length)
            throw new FormatException("Token inesperado");

        char actual = _cadena[_pos];

        if (actual == '+') { _pos++; return LeerFactor(); }
        if (actual == '-') { _pos++; return new UnarioNegativo(LeerFactor()); }

        if (actual == '(') {
            _pos++;
            var nodo = LeerExpresion();
            if (_pos >= _cadena.Length || _cadena[_pos] != ')')
                throw new FormatException("Se esperaba ')'");
            _pos++;
            return nodo;
        }

        if (char.ToLower(actual) == 'x') {
            _pos++;
            return new Variable();
        }

        if (char.IsDigit(actual)) {
            int inicio = _pos;
            while (_pos < _cadena.Length && char.IsDigit(_cadena[_pos])) _pos++;
            string numero = _cadena.Substring(inicio, _pos - inicio);
            return new Constante(int.Parse(numero));
        }

        throw new FormatException("Token inesperado en posición " + _pos);
    }
}
