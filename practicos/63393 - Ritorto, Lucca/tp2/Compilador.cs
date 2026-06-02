using System;

class Compilador {
    private readonly string texto;
    private int posicion;

    private Compilador(string texto) {
        this.texto = texto ?? "";
        this.posicion = 0;
    }

    public static Nodo Parse(string expresion) {
        Compilador compilador = new Compilador(expresion);
        Nodo nodo = compilador.ParsearExpresion();
        compilador.SaltarEspacios();

        if (!compilador.Fin())
            throw new FormatException("Token inesperado: " + compilador.Actual());

        return nodo;
    }

    private Nodo ParsearExpresion() {
        Nodo izquierdo = ParsearTermino();

        while (true) {
            SaltarEspacios();

            if (Coincide('+')) {
                Nodo derecho = ParsearTermino();
                izquierdo = new SumaNodo(izquierdo, derecho);
            } else if (Coincide('-')) {
                Nodo derecho = ParsearTermino();
                izquierdo = new RestaNodo(izquierdo, derecho);
            } else {
                break;
            }
        }

        return izquierdo;
    }

    private Nodo ParsearTermino() {
        Nodo izquierdo = ParsearFactor();

        while (true) {
            SaltarEspacios();

            if (Coincide('*')) {
                Nodo derecho = ParsearFactor();
                izquierdo = new MultiplicacionNodo(izquierdo, derecho);
            } else if (Coincide('/')) {
                Nodo derecho = ParsearFactor();
                izquierdo = new DivisionNodo(izquierdo, derecho);
            } else {
                break;
            }
        }

        return izquierdo;
    }

    private Nodo ParsearFactor() {
        SaltarEspacios();

        if (Fin())
            throw new FormatException("Token inesperado");

        if (Coincide('+'))
            return new PositivoNodo(ParsearFactor());

        if (Coincide('-'))
            return new NegativoNodo(ParsearFactor());

        if (Coincide('(')) {
            Nodo interno = ParsearExpresion();
            SaltarEspacios();

            if (!Coincide(')'))
                throw new FormatException("Se esperaba ')'");

            return interno;
        }

        char c = Actual();

        if (char.IsDigit(c))
            return ParsearNumero();

        if (c == 'x' || c == 'X') {
            posicion++;
            return new VariableNodo();
        }

        throw new FormatException("Token inesperado");
    }

    private Nodo ParsearNumero() {
        int inicio = posicion;

        while (!Fin() && char.IsDigit(Actual()))
            posicion++;

        string numeroTexto = texto.Substring(inicio, posicion - inicio);
        int valor = int.Parse(numeroTexto);
        return new NumeroNodo(valor);
    }

    private void SaltarEspacios() {
        while (!Fin() && char.IsWhiteSpace(Actual()))
            posicion++;
    }

    private bool Coincide(char esperado) {
        SaltarEspacios();

        if (!Fin() && Actual() == esperado) {
            posicion++;
            return true;
        }

        return false;
    }

    private char Actual() {
        return texto[posicion];
    }

    private bool Fin() {
        return posicion >= texto.Length;
    }
}
