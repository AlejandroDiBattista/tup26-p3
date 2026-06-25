using System;

class Compilador {
    private string texto;
    private int pos;

    private Compilador(string texto) {
        this.texto = texto;
        this.pos = 0;
    }

    public static Nodo Compilar(string texto) {
        var compilador = new Compilador(texto);

        compilador.SaltarEspacios();

        Nodo nodo = compilador.Expresion();

        compilador.SaltarEspacios();

        if (compilador.pos < compilador.texto.Length)
            throw new Exception($"Token inesperado en posición {compilador.pos}");

        return nodo;
    }

    private char Actual {
        get {
            if (pos < texto.Length)
                return texto[pos];
            return '\0';
        }
    }

    private void Avanzar() {
        pos++;
    }

    private void SaltarEspacios() {
        while (char.IsWhiteSpace(Actual))
            Avanzar();
    }

    private Nodo Expresion() {
        Nodo nodo = Termino();

        while (true) {
            SaltarEspacios();

            if (Actual == '+') {
                Avanzar();
                nodo = new SumaNodo(nodo, Termino());
            } else if (Actual == '-') {
                Avanzar();
                nodo = new RestaNodo(nodo, Termino());
            } else {
                break;
            }
        }

        return nodo;
    }

    private Nodo Termino() {
        Nodo nodo = Factor();

        while (true) {
            SaltarEspacios();

            if (Actual == '*') {
                Avanzar();
                nodo = new MultiplicacionNodo(nodo, Factor());
            } else if (Actual == '/') {
                Avanzar();
                nodo = new DivisionNodo(nodo, Factor());
            } else {
                break;
            }
        }

        return nodo;
    }

    private Nodo Factor() {
        SaltarEspacios();

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

            SaltarEspacios();

            if (Actual != ')')
                throw new Exception("Paréntesis sin cerrar");

            Avanzar();
            return nodo;
        }

        if (char.IsDigit(Actual)) {
            return Numero();
        }

        if (Actual == 'x' || Actual == 'X') {
            Avanzar();
            return new VariableNodo();
        }

        throw new Exception($"Token inesperado en posición {pos}");
    }

    private Nodo Numero() {
        int inicio = pos;

        while (char.IsDigit(Actual))
            Avanzar();

        string numeroTexto = texto.Substring(inicio, pos - inicio);
        int valor = int.Parse(numeroTexto);

        return new NumeroNodo(valor);
    }
}
