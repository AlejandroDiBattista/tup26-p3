using System;
class Compilador {
    private string input = "";
    private int pos = 0;
    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new FormatException("Token inesperado");
        var comp = new Compilador();
        comp.input = expresion;
        comp.pos = 0;
        var nodo = comp.ParseExpresion();
        return nodo;
    }
    private Nodo ParseExpresion() {
        Nodo nodo = ParseTermino();
        while (true) {
            SkipEspacios();
            if (Match('+')) {
                nodo = new SumaNodo(nodo, ParseTermino());
            }
            else if (Match('-')) {
                nodo = new RestaNodo(nodo, ParseTermino());
            }
            else {
                break;
            }
        }
        return nodo;
    }
    private Nodo ParseTermino() {
        Nodo nodo = ParseFactor();
        while (true) {
            SkipEspacios();
            if (Match('*')) {
                nodo = new MultiplicacionNodo(nodo, ParseFactor());
            }
            else if (Match('/')) {
                nodo = new DivisionNodo(nodo, ParseFactor());
            }
            else {
                break;
            }
        }

        return nodo;
    }
     private Nodo ParseFactor() {
        SkipEspacios();
        if (Match('+')) {
            return ParseFactor();
        }
        if (Match('-')) {
            return new NegativoNodo(ParseFactor());
        }
        if (Match('(')) {
            var nodo = ParseExpresion();
            SkipEspacios();
            if (!Match(')'))
                throw new FormatException("Se esperaba ')'");
            return nodo;
        }
        if (Peek() == 'x' || Peek() == 'X') {
            pos++;
            return new VariableNodo();
        }
        if (char.IsDigit(Peek())) {
            return new NumeroNodo(ParseNumero());
        }
        throw new FormatException("Token inesperado");
    }
    private int ParseNumero() {
        SkipEspacios();
        int start = pos;
        while (pos < input.Length && char.IsDigit(input[pos])) {
            pos++;
        }
        string num = input.Substring(start, pos - start);
        return int.Parse(num);
    }
    private void SkipEspacios() {
        while (pos < input.Length && char.IsWhiteSpace(input[pos])) {
            pos++;
        }
    }
    private char Peek() {
        if (pos >= input.Length) return '\0';
        return input[pos];
    }
    private bool Match(char c) {
        SkipEspacios();
        if (Peek() == c) {
            pos++;
            return true;
        }
        return false;
    }
}

