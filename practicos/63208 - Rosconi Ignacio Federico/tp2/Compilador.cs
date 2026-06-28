using System;

static class Compilador {
    public static Nodo Compilar(string texto) {
        Parser p = new Parser(texto);
        Nodo nodo = p.Expresion();

        if (p.HayMas())
            throw new Exception("Token inesperado");

        return nodo;
    }

    class Parser {
        string txt;
        int pos;

        public Parser(string t) {
            txt = t;
            pos = 0;
        }

        char Actual => pos < txt.Length ? txt[pos] : '\0';
        public bool HayMas() => pos < txt.Length;
        void Avanzar() => pos++;

        void Espacios() {
            while (char.IsWhiteSpace(Actual)) Avanzar();
        }

        public Nodo Expresion() {
            Nodo nodo = Termino();

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
            Nodo nodo = Factor();

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
                Nodo nodo = Expresion();
                if (Actual != ')') throw new Exception("Paréntesis sin cerrar");
                Avanzar();
                return nodo;
            }

            if (char.IsDigit(Actual)) return Numero();

            if (Actual == 'x' || Actual == 'X') {
                Avanzar();
                return new VariableNodo();
            }

            throw new Exception("Token inesperado");
        }

        Nodo Numero() {
            int inicio = pos;
            while (char.IsDigit(Actual)) Avanzar();
            int valor = int.Parse(txt.Substring(inicio, pos - inicio));
            return new NumeroNodo(valor);
        }
    }
}
