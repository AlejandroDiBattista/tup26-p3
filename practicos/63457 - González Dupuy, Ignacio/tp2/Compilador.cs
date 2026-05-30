class Compilador {
    private static string texto = "";
    private static int pos = 0;

    public static Nodo Parse(string expresion) {
        texto = expresion.Replace(" ", "");
        pos = 0;

        Nodo nodo = Expresion();

        if (pos < texto.Length)
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private static Nodo Expresion() {
        Nodo nodo = Termino();

        while (pos < texto.Length && (texto[pos] == '+' || texto[pos] == '-')) {
            char op = texto[pos];
            pos++;

            Nodo derecho = Termino();

            if (op == '+')
                nodo = new SumaNodo(nodo, derecho);
            else
                nodo = new RestaNodo(nodo, derecho);
        }

        return nodo;
    }

    private static Nodo Termino() {
        Nodo nodo = Factor();

        while (pos < texto.Length && (texto[pos] == '*' || texto[pos] == '/')) {
            char op = texto[pos];
            pos++;

            Nodo derecho = Factor();

            if (op == '*')
                nodo = new MultiplicacionNodo(nodo, derecho);
            else
                nodo = new DivisionNodo(nodo, derecho);
        }

        return nodo;
    }
    private static Nodo Factor() {
        if (pos >= texto.Length)
            throw new FormatException("Token inesperado");

        if (texto[pos] == '+') {
            pos++;
            return Factor();
        }

        if (texto[pos] == '-') {
            pos++;
            return new NegativoNodo(Factor());
        }

        if (texto[pos] == '(') {
            pos++;
            Nodo nodo = Expresion();

            if (pos >= texto.Length || texto[pos] != ')')
                throw new FormatException("Se esperaba ')'");

            pos++;
            return nodo;
        }

        if (char.ToLower(texto[pos]) == 'x') {
            pos++;
            return new VariableNodo();
        }

        if (char.IsDigit(texto[pos])) {
            string numero = "";

            while (pos < texto.Length && char.IsDigit(texto[pos])) {
                numero += texto[pos];
                pos++;
            }

            return new NumeroNodo(int.Parse(numero));
        }

        throw new FormatException("Token inesperado");
    }
}
