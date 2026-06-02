using System;

static class Compilador {
    static string texto = "";
    static int pos = 0;

    public static Nodo Parse(string input) {
        texto = input;
        pos = 0;

        var nodo = ParseExpresion();

        SaltarEspacios();

        if (pos < texto.Length)
            throw new Exception("Entrada invalida");

        return nodo;
    }


    static Nodo ParseExpresion() {
        var izquierda = ParseTermino();

        while (true) {
            SaltarEspacios();

            if (pos >= texto.Length)
                break;

            char op = texto[pos];

            if (op == '+' || op == '-') {
                pos++;
                var derecha = ParseTermino();

                if (op == '+')
                    izquierda = new SumaNodo(izquierda, derecha);
                else
                    izquierda = new RestaNodo(izquierda, derecha);
            } else {
                break;
            }
        }

        return izquierda;
    }

    static Nodo ParseTermino() {
        var izquierda = ParseFactor();

        while (true) {
            SaltarEspacios();

            if (pos >= texto.Length)
                break;

            char op = texto[pos];

            if (op == '*' || op == '/') {
                pos++;
                var derecha = ParseFactor();

                if (op == '*')
                    izquierda = new MultiplicacionNodo(izquierda, derecha);
                else
                    izquierda = new DivisionNodo(izquierda, derecha);
            } else {
                break;
            }
        }

        return izquierda;
    }

    static Nodo ParseFactor() {
        SaltarEspacios();

        if (pos >= texto.Length)
            throw new Exception("Expresion vacia");

        char actual = texto[pos];

        // operador negativo
        if (actual == '-') {
            pos++;
            var expr = ParseFactor();
            return new NegativoNodo(expr);
        }

        // parentesis
        if (actual == '(') {
            pos++;
            var expr = ParseExpresion();

            SaltarEspacios();

            if (pos >= texto.Length || texto[pos] != ')')
                throw new Exception("Falta cerrar parentesis");

            pos++;
            return expr;
        }

        // numero
        if (char.IsDigit(actual)) {
            int inicio = pos;

            while (pos < texto.Length && char.IsDigit(texto[pos]))
                pos++;

            string numero = texto.Substring(inicio, pos - inicio);
            return new NumeroNodo(int.Parse(numero));
        }

        // variable x
        if (actual == 'x' || actual == 'X') {
            pos++;
            return new VariableNodo();
        }

        throw new Exception("Token inesperado");
    }

    static void SaltarEspacios() {
        while (pos < texto.Length && char.IsWhiteSpace(texto[pos]))
            pos++;
    }
}
