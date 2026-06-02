using System;

class Compilador {
    private string texto = "";
    private int pos;

    public Nodo Parsear(string input) {
        if (string.IsNullOrWhiteSpace(input))
            throw new Exception("Entrada vacía");

        texto = input.Replace(" ", "");
        pos = 0;

        Nodo nodo = ParseExpresion();

        if (pos < texto.Length)
            throw new Exception("Token inesperado");

        return nodo;
    }

    private Nodo ParseExpresion() {
        Nodo nodo = ParseTermino();

        while (pos < texto.Length && (texto[pos] == '+' || texto[pos] == '-')) {
            char op = texto[pos];
            pos++;

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

        while (pos < texto.Length && (texto[pos] == '*' || texto[pos] == '/')) {
            char op = texto[pos];
            pos++;

            Nodo derecho = ParseFactor();

            if (op == '*')
                nodo = new MultiplicacionNodo(nodo, derecho);
            else
                nodo = new DivisionNodo(nodo, derecho);
        }

        return nodo;
    }

    private Nodo ParseFactor() {
        if (pos >= texto.Length)
            throw new Exception("Expresión incompleta");

        char c = texto[pos];

        if (c == '+') {
            pos++;
            return ParseFactor();
        }

        if (c == '-') {
            pos++;
            return new NegativoNodo(ParseFactor());
        }

        if (c == '(') {
            pos++;
            Nodo nodo = ParseExpresion();

            if (pos >= texto.Length || texto[pos] != ')')
                throw new Exception("Paréntesis sin cerrar");

            pos++;
            return nodo;
        }

        if (char.IsDigit(c)) {
            int inicio = pos;

            while (pos < texto.Length && char.IsDigit(texto[pos]))
                pos++;

            string numeroTexto = texto.Substring(inicio, pos - inicio);
            int valor = int.Parse(numeroTexto);

            return new NumeroNodo(valor);
        }

        if (c == 'x' || c == 'X') {
            pos++;
            return new VariableNodo();
        }

        throw new Exception("Token inesperado");
    }
}
