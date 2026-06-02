using System;

class Compilador {
    string text;
    int pos;

    Compilador(string input) {
        text = input;
        pos = 0;
    }

    public static Nodo Compilar(string input) {
        var c = new Compilador(input);
        var nodo = c.Expresion();
        if (c.pos < c.text.Length)
            throw new Exception("Token inesperado");
        return nodo;
    }

    char Actual => pos < text.Length ? text[pos] : '\0';

    void Avanzar() => pos++;

    void Espacios() {
        while (char.IsWhiteSpace(Actual)) Avanzar();
    }

    Nodo Expresion() {
        var nodo = Termino();

        while (true) {
            Espacios();
            if (Actual == '+') {
                Avanzar();
                nodo = new SumaNodo(nodo, Termino());
            } else if (Actual == '-') {
                Avanzar();
                nodo = new RestaNodo(nodo, Termino());
            } else break;
        }

        return nodo;
    }

    Nodo Termino() {
        var nodo = Factor();

        while (true) {
            Espacios();
            if (Actual == '*') {
                Avanzar();
                nodo = new MultiplicacionNodo(nodo, Factor());
            } else if (Actual == '/') {
                Avanzar();
                nodo = new DivisionNodo(nodo, Factor());
            } else break;
        }

        return nodo;
    }

    Nodo Factor() {
        Espacios();

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
            var nodo = Expresion();

            if (Actual != ')')
                throw new Exception("Paréntesis sin cerrar");

            Avanzar();
            return nodo;
        }

        if (char.IsDigit(Actual))
            return Numero();

        if (Actual == 'x' || Actual == 'X') {
            Avanzar();
            return new VariableNodo();
        }

        throw new Exception("Token inesperado");
    }

    Nodo Numero() {
        int inicio = pos;

        while (char.IsDigit(Actual)) Avanzar();

        int valor = int.Parse(text.Substring(inicio, pos - inicio));
        return new NumeroNodo(valor);
    }
}
