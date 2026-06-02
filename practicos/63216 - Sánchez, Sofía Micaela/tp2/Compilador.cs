class Compilador {
    private string texto;
    private int pos;

    private Compilador(string texto) {
        this.texto = texto.Replace(" ", "");
        pos = 0;
    }

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new FormatException("Token inesperado");

        var comp = new Compilador(expresion);
        Nodo nodo = comp.ParseExpresion();

        if (!comp.Fin())
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private bool Fin() {
        return pos >= texto.Length;
    }

    private char Actual() {
        return pos < texto.Length ? texto[pos] : '\0';
    }

    private void Avanzar() {
        pos++;
    }

    private Nodo ParseExpresion() {
        Nodo nodo = ParseTermino();

        while (Actual() == '+' || Actual() == '-') {
            char op = Actual();
            Avanzar();

            Nodo derecho = ParseTermino();

            if (op == '+')
                nodo = new SumaNodo(nodo, derecho);
            else
                nodo = new RestaNodo(nodo, derecho);
        }

        return nodo;
    }

    private Nodo ParseTermino() {
        Nodo nodo = ParseFactor();

        while (Actual() == '*' || Actual() == '/') {
            char op = Actual();
            Avanzar();

            Nodo derecho = ParseFactor();

            if (op == '*')
                nodo = new MultiplicacionNodo(nodo, derecho);
            else
                nodo = new DivisionNodo(nodo, derecho);
        }

        return nodo;
    }

    private Nodo ParseFactor() {
        char c = Actual();

        if (c == '+') {
            Avanzar();
            return ParseFactor();
        }

        if (c == '-') {
            Avanzar();
            return new NegativoNodo(ParseFactor());
        }

        if (c == '(') {
            Avanzar();
            Nodo nodo = ParseExpresion();

            if (Actual() != ')')
                throw new FormatException("Se esperaba ')'");

            Avanzar();
            return nodo;
        }

        if (char.IsDigit(c)) {
            int inicio = pos;

            while (char.IsDigit(Actual()))
                Avanzar();

            int valor = int.Parse(texto.Substring(inicio, pos - inicio));
            return new NumeroNodo(valor);
        }

        if (c == 'x' || c == 'X') {
            Avanzar();
            return new VariableNodo();
        }

        throw new FormatException("Token inesperado");
    }
}
