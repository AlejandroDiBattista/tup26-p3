using System;

public class Compilador {
    private readonly string input;
    private int pos;

    private Compilador(string expresion) {
        input = expresion;
        pos = 0;
    }

    public static Nodo Parsear(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new Exception("Error: Entrada vacía.");

        var compilador = new Compilador(expresion);
        Nodo ast = compilador.ParsearExpresion();

        compilador.SaltarEspacios();
        if (compilador.pos < compilador.input.Length)
            throw new Exception($"Error: Token inesperado '{compilador.input[compilador.pos]}' en la posición {compilador.pos}.");

        return ast;
    }

    private void SaltarEspacios() {
        while (pos < input.Length && char.IsWhiteSpace(input[pos])) pos++;
    }

    private char? Peek() {
        SaltarEspacios();
        if (pos >= input.Length) return null;
        return input[pos];
    }

    private char Leer() {
        SaltarEspacios();
        return input[pos++];
    }

    private Nodo ParsearExpresion() {
        Nodo nodo = ParsearTermino();
        while (true) {
            char? op = Peek();
            if (op == '+' || op == '-') {
                Leer(); // Consume el operador
                Nodo derecho = ParsearTermino();
                if (op == '+') nodo = new SumaNodo(nodo, derecho);
                else nodo = new RestaNodo(nodo, derecho);
            } else break;
        }
        return nodo;
    }

    private Nodo ParsearTermino() {
        Nodo nodo = ParsearFactor();
        while (true) {
            char? op = Peek();
            if (op == '*' || op == '/') {
                Leer();
                Nodo derecho = ParsearFactor();
                if (op == '*') nodo = new MultiplicacionNodo(nodo, derecho);
                else nodo = new DivisionNodo(nodo, derecho);
            } else break;
        }
        return nodo;
    }

    private Nodo ParsearFactor() {
        char? token = Peek();
        if (token == null) throw new Exception("Error: Expresión incompleta, token inesperado.");

        if (token == '+') {
            Leer();
            return new PositivoNodo(ParsearFactor());
        }
        if (token == '-') {
            Leer();
            return new NegativoNodo(ParsearFactor());
        }

        if (token == '(') {
            Leer();
            Nodo nodo = ParsearExpresion();
            char? cierre = Peek();
            if (cierre != ')') throw new Exception("Error: Paréntesis sin cerrar.");
            Leer(); // Consume el ')'
            return nodo;
        }

        if (token == 'x' || token == 'X') {
            Leer();
            return new VariableNodo();
        }

        if (char.IsDigit(token.Value)) {
            int valor = 0;
            while (pos < input.Length && char.IsDigit(input[pos])) {
                valor = valor * 10 + (input[pos] - '0');
                pos++;
            }
            return new NumeroNodo(valor);
        }

        throw new Exception($"Error: Token inesperado '{token}' en la posición {pos}.");
    }
}
