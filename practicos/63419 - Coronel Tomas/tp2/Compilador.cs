using System;

class Compilador {
    string texto;
    int pos;

    char Actual => pos < texto.Length ? texto[pos] : '\0';
    void Avanzar() => pos++;

    void SaltarEspacio() {
        while (char.IsWhiteSpace(Actual))
            Avanzar();
    }

    public static Nodo Compilar(string texto) {
        var c = new Compilador { texto = texto };
        var nodo = c.Expresion();
        c.SaltarEspacio();

        if (c.pos < c.texto.Length)
            throw new Exception("Token inesperado");

        return nodo;

    }
    Nodo Expresion() {
        Nodo nodo = Termino();

        while (true) {
            SaltarEspacio();

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
        Nodo nodo = Factor();

        while (true) {
            SaltarEspacio();

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
        SaltarEspacio();

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

            SaltarEspacio();
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

        while (char.IsDigit(Actual))
            Avanzar();

        string num = texto.Substring(inicio, pos - inicio);
        return new NumeroNodo(int.Parse(num));
    }

}
