using System.ComponentModel;
using System.Dynamic;
using System.Reflection.Metadata;

class Compilador {
    private string input;
    private int pos;

    public static Nodo Parse(string texto) {
        if (string.IsNullOrWhiteSpace(texto))
            throw new FormatException("Token inesperado");

        var comp = new Compilador();
        comp.input = texto.Replace(" ", "");
        comp.pos = 0;

        var resultado = comp.Expresion();

        if (comp.pos != comp.input.Length)
            throw new FormatException("Token inesperado");

        return resultado;
    }

    private char Actual() {
        if (pos >= input.Length) return '\0';
        return input[pos];
    }

    private char Consumir() {
        return input[pos++];
    }

    private Nodo Expresion() {
        Nodo nodo = Termino();

        while (Actual() == '+' || Actual() == '-') {
            char op = Consumir();
            Nodo der = Termino();
            nodo = new Binaria(nodo, op, der);
        }

        return nodo;
    }

    private Nodo Termino() {
        Nodo nodo = Factor();

        while (Actual() == '*' || Actual() == '/') {
            char op = Consumir();
            Nodo der = Factor();
            nodo = new Binaria(nodo, op, der);
        }

        return nodo;
    }

    private Nodo Factor() {
        char c = Actual();


        if (c == '+' || c == '-') {
            char op = Consumir();
            Nodo expr = Factor();

            if (op == '+') return expr;
            return new Unaria('-', expr);
        }


        if (c == 'x' || c == 'X') {
            Consumir();
            return new Variable();
        }


        if (char.IsDigit(c)) {
            return Numero();
        }

        if (c == '(') {
            Consumir();
            Nodo expr = Expresion();

            if (Actual() != ')')
                throw new FormatException("Se esperaba ')'");

            Consumir();
            return expr;
        }

        throw new FormatException("Token inesperado");
    }

    private Nodo Numero() {
        int inicio = pos;

        while (char.IsDigit(Actual())) {
            Consumir();
        }

        int valor = int.Parse(input.Substring(inicio, pos - inicio));
        return new Numero(valor);
    }
}
